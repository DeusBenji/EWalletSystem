using ValidationService.Security;
using Xunit;

namespace ValidationService.Tests.SecurityTests;

/// <summary>
/// Tests for circuit manifest verification.
/// Ensures only signed circuits are accepted.
/// </summary>
[Trait("Category", "SecurityInvariant")]
public class CircuitManifestVerifierTests
{
    private readonly CircuitManifestVerifier _verifier;

    public CircuitManifestVerifierTests()
    {
        _verifier = new CircuitManifestVerifier(
            Microsoft.Extensions.Logging.Abstractions.NullLogger<CircuitManifestVerifier>.Instance);
    }

    [Fact]
    public async Task VerifyManifest_MissingSignature_ShouldReject()
    {
        // Arrange - SECURITY INVARIANT: Unsigned circuits rejected
        var manifest = CreateManifest(signature: null);

        // Act
        var result = await _verifier.VerifyManifestAsync(manifest);

        // Assert
        Assert.False(result.Valid);
        Assert.Equal(ManifestVerificationCode.MissingSignature, result.Code);
    }

    [Fact]
    public async Task VerifyManifest_EmptySignature_ShouldReject()
    {
        // Arrange
        var manifest = CreateManifest(signature: "");

        // Act
        var result = await _verifier.VerifyManifestAsync(manifest);

        // Assert
        Assert.False(result.Valid);
        Assert.Equal(ManifestVerificationCode.MissingSignature, result.Code);
    }

    [Fact]
    public async Task VerifyManifest_InvalidBase64Signature_ShouldReject()
    {
        // Arrange
        var manifest = CreateManifest(signature: "not-valid-base64!!!");

        // Act
        var result = await _verifier.VerifyManifestAsync(manifest);

        // Assert
        Assert.False(result.Valid);
        Assert.Equal(ManifestVerificationCode.InvalidSignature, result.Code);
    }

    [Fact]
    public async Task VerifyManifest_ValidSignature_ShouldAccept()
    {
        // Arrange
        // Note: This is a placeholder test - in production, use real signature
        var manifest = CreateManifest(signature: Convert.ToBase64String(new byte[] { 1, 2, 3 }));

        // Act
        var result = await _verifier.VerifyManifestAsync(manifest);

        // Assert
        // TODO: Update when real signature verification is implemented
        Assert.True(result.Valid); // Placeholder always accepts valid base64
    }

    [Fact]
    public void CircuitSigningPublicKey_HasFingerprint()
    {
        // Assert - Ensure public key fingerprint is set
        Assert.NotNull(CircuitSigningPublicKey.Fingerprint);
        Assert.NotEmpty(CircuitSigningPublicKey.Fingerprint);
    }

    [Fact]
    public void CircuitSigningPublicKey_HasPublicKeyPem()
    {
        // Assert - Ensure public key PEM is set
        Assert.NotNull(CircuitSigningPublicKey.PublicKeyPem);
        Assert.Contains("BEGIN PUBLIC KEY", CircuitSigningPublicKey.PublicKeyPem);
        Assert.Contains("END PUBLIC KEY", CircuitSigningPublicKey.PublicKeyPem);
    }

    [Fact]
    public void CircuitSigningPublicKey_IsValidKeyFingerprint_CurrentKey_ReturnsTrue()
    {
        // Act
        var isValid = CircuitSigningPublicKey.IsValidKeyFingerprint(
            CircuitSigningPublicKey.Fingerprint);

        // Assert
        Assert.True(isValid);
    }

    [Fact]
    public void CircuitSigningPublicKey_IsValidKeyFingerprint_UnknownKey_ReturnsFalse()
    {
        // Act
        var isValid = CircuitSigningPublicKey.IsValidKeyFingerprint("unknown-fingerprint");

        // Assert
        Assert.False(isValid);
    }

    private CircuitManifest CreateManifest(string? signature)
    {
        return new CircuitManifest
        {
            CircuitId = "age_verification_v1",
            Version = "1.2.0",
            BuildTimestamp = DateTime.UtcNow,
            Artifacts = new CircuitArtifacts
            {
                Prover = new ArtifactInfo
                {
                    Filename = "prover.wasm",
                    Sha256 = "abc123",
                    Size = 12345
                },
                VerificationKey = new ArtifactInfo
                {
                    Filename = "verification_key.json",
                    Sha256 = "def456",
                    Size = 6789
                }
            },
            Builder = new BuilderInfo
            {
                CircomVersion = "2.1.6",
                SnarkjsVersion = "0.7.0",
                DockerImage = "node:20-alpine"
            },
            Signature = signature
        };
    }
}
