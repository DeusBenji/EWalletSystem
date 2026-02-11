using ValidationService.Protocol;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace ValidationService.Tests.ProtocolTests;

/// <summary>
/// Tests for ProofEnvelopeValidator.
/// Verifies protocol compliance, origin binding, and anti-downgrade enforcement.
/// </summary>
public class ProofEnvelopeValidatorTests
{
    private readonly ProofEnvelopeValidator _validator;

    public ProofEnvelopeValidatorTests()
    {
        _validator = new ProofEnvelopeValidator(
            NullLogger<ProofEnvelopeValidator>.Instance);
    }

    [Fact]
    public async Task ValidateEnvelope_CompleteValidEnvelope_ShouldPass()
    {
        // Arrange
        var envelope = CreateValidEnvelope();

        // Act
        var result = await _validator.ValidateEnvelopeAsync(
            envelope,
            expectedOrigin: "https://example.com");

        // Assert
        Assert.True(result.Valid);
        Assert.Equal(EnvelopeValidationCode.Valid, result.Code);
    }

    [Fact]
    public async Task ValidateEnvelope_MissingProtocolVersion_ShouldFail()
    {
        // Arrange
        var envelope = CreateValidEnvelope() with { ProtocolVersion = "" };

        // Act
        var result = await _validator.ValidateEnvelopeAsync(
            envelope,
            expectedOrigin: "https://example.com");

        // Assert
        Assert.False(result.Valid);
        Assert.Equal(EnvelopeValidationCode.MissingField, result.Code);
    }

    [Fact]
    public async Task ValidateEnvelope_OriginMismatch_ShouldFail()
    {
        // Arrange
        var envelope = CreateValidEnvelope() with { Origin = "https://malicious.com" };

        // Act
        var result = await _validator.ValidateEnvelopeAsync(
            envelope,
            expectedOrigin: "https://example.com");

        // Assert
        Assert.False(result.Valid);
        Assert.Equal(EnvelopeValidationCode.OriginMismatch, result.Code);
    }

    [Fact]
    public async Task ValidateEnvelope_DowngradeAttempt_ShouldBeBlocked()
    {
        // Arrange - Attacker tries to force vulnerable v1.0.0
        var envelope = CreateValidEnvelope() with
        {
            PolicyId = "age_over_18",
            PolicyVersion = "1.0.0"  // Below minimum (1.2.0)
        };

        // Act
        var result = await _validator.ValidateEnvelopeAsync(
            envelope,
            expectedOrigin: "https://example.com");

        // Assert - SECURITY INVARIANT: Downgrade blocked
        Assert.False(result.Valid);
        Assert.Equal(EnvelopeValidationCode.DowngradeRejected, result.Code);
        Assert.Contains("minimum", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ValidateEnvelope_AcceptableVersion_ShouldPass()
    {
        // Arrange
        var envelope = CreateValidEnvelope() with
        {
            PolicyId = "age_over_18",
            PolicyVersion = "1.2.0"  // Meets minimum
        };

        // Act
        var result = await _validator.ValidateEnvelopeAsync(
            envelope,
            expectedOrigin: "https://example.com");

        // Assert
        Assert.True(result.Valid);
    }

    [Fact]
    public async Task ValidateEnvelope_ClockSkewExceeded_ShouldFail()
    {
        // Arrange - Timestamp 10 minutes in the past (exceeds 5 min limit)
        var tenMinutesAgo = DateTimeOffset.UtcNow.AddMinutes(-10).ToUnixTimeSeconds();
        var envelope = CreateValidEnvelope() with { IssuedAt = tenMinutesAgo };

        // Act
        var result = await _validator.ValidateEnvelopeAsync(
            envelope,
            expectedOrigin: "https://example.com");

        // Assert
        Assert.False(result.Valid);
        Assert.Equal(EnvelopeValidationCode.ClockSkew, result.Code);
    }

    [Fact]
    public async Task ValidateEnvelope_ClockSkewWithinTolerance_ShouldPass()
    {
        // Arrange - Timestamp 3 minutes ago (within 5 min tolerance)
        var threeMinutesAgo = DateTimeOffset.UtcNow.AddMinutes(-3).ToUnixTimeSeconds();
        var envelope = CreateValidEnvelope() with { IssuedAt = threeMinutesAgo };

        // Act
        var result = await _validator.ValidateEnvelopeAsync(
            envelope,
            expectedOrigin: "https://example.com");

        // Assert
        Assert.True(result.Valid);
    }

    [Fact]
    public async Task ValidateEnvelope_ShortNonce_ShouldFail()
    {
        // Arrange - Nonce too short (< 32 bytes = 64 hex chars)
        var envelope = CreateValidEnvelope() with { Nonce = "short_nonce" };

        // Act
        var result = await _validator.ValidateEnvelopeAsync(
            envelope,
            expectedOrigin: "https://example.com");

        // Assert
        Assert.False(result.Valid);
        Assert.Equal(EnvelopeValidationCode.MissingField, result.Code);
        Assert.Contains("too short", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ValidateEnvelope_MissingSignature_ShouldFail()
    {
        // Arrange
        var envelope = CreateValidEnvelope() with { Signature = null };

        // Act
        var result = await _validator.ValidateEnvelopeAsync(
            envelope,
            expectedOrigin: "https://example.com");

        // Assert
        Assert.False(result.Valid);
        Assert.Equal(EnvelopeValidationCode.MissingField, result.Code);
    }

    [Fact]
    public async Task ValidateEnvelope_UnsupportedProtocolVersion_ShouldFail()
    {
        // Arrange - Protocol v2.0.0 not supported (only v1.x)
        var envelope = CreateValidEnvelope() with { ProtocolVersion = "2.0.0" };

        // Act
        var result = await _validator.ValidateEnvelopeAsync(
            envelope,
            expectedOrigin: "https://example.com");

        // Assert
        Assert.False(result.Valid);
        Assert.Equal(EnvelopeValidationCode.UnsupportedProtocolVersion, result.Code);
    }

    [Fact]
    public async Task ValidateEnvelope_FewPublicSignals_ShouldFail()
    {
        // Arrange - Less than minimum 7 signals
        var envelope = CreateValidEnvelope() with
        {
            PublicSignals = new List<string> { "signal1", "signal2" }
        };

        // Act
        var result = await _validator.ValidateEnvelopeAsync(
            envelope,
            expectedOrigin: "https://example.com");

        // Assert
        Assert.False(result.Valid);
        Assert.Equal(EnvelopeValidationCode.InvalidPublicSignals, result.Code);
    }

    private ProofEnvelope CreateValidEnvelope()
    {
        return new ProofEnvelope
        {
            ProtocolVersion = "1.0.0",
            PolicyId = "age_over_18",
            PolicyVersion = "1.2.0",
            Origin = "https://example.com",
            Nonce = "a3f8b2c1e5d7f9a1b3c5d7e9f1a3b5c7d9e1f3a5b7c9d1e3f5a7b9c1d3e5f7a9", // 64 chars
            IssuedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            Proof = new ZkProof
            {
                PiA = new List<string> { "1", "2", "3" },
                PiB = new List<List<string>>
                {
                    new() { "1", "2" },
                    new() { "3", "4" },
                    new() { "5", "6" }
                },
                PiC = new List<string> { "7", "8", "9" }
            },
            PublicSignals = new List<string>
            {
                "12345678901234567890",  // challengeHash
                "98765432109876543210",  // credentialHash
                "11111111111111111111",  // policyHash
                "22222222222222222222",  // originHash
                "1707659400",            // issuedAt
                "1707918600",            // expiresAt
                "1"                      // policyResult
            },
            CredentialHash = "sha256:abc123def456",
            PolicyHash = "sha256:def456abc123",
            Signature = "MEUCIQDx..."
        };
    }
}
