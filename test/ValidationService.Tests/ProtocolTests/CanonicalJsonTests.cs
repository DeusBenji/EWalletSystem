using ValidationService.Protocol;
using Xunit;

namespace ValidationService.Tests.ProtocolTests;

/// <summary>
/// Tests for canonical JSON encoding.
/// Ensures deterministic encoding for signature verification.
/// </summary>
public class CanonicalJsonTests
{
    [Fact]
    public void Encode_ValidEnvelope_RemovesSignature()
    {
        // Arrange
        var envelope = CreateEnvelope(signature: "should-be-removed");

        // Act
        var canonical = CanonicalJsonEncoder.Encode(envelope);

        // Assert - Signature not in canonical JSON
        Assert.DoesNotContain("should-be-removed", canonical);
        Assert.DoesNotContain("signature", canonical);
    }

    [Fact]
    public void Encode_ValidEnvelope_IncludesAllOtherFields()
    {
        // Arrange
        var envelope = CreateEnvelope();

        // Act
        var canonical = CanonicalJsonEncoder.Encode(envelope);

        // Assert - All required fields present
        Assert.Contains("protocolVersion", canonical);
        Assert.Contains("policyId", canonical);
        Assert.Contains("origin", canonical);
        Assert.Contains("nonce", canonical);
        Assert.Contains("proof", canonical);
        Assert.Contains("publicSignals", canonical);
    }

    [Fact]
    public void Encode_ValidEnvelope_NoWhitespace()
    {
        // Arrange
        var envelope = CreateEnvelope();

        // Act
        var canonical = CanonicalJsonEncoder.Encode(envelope);

        // Assert - Compact JSON (no whitespace)
        Assert.DoesNotContain("\n", canonical);
        Assert.DoesNotContain("  ", canonical);
    }

    [Fact]
    public void Parse_ValidJson_ReturnsEnvelope()
    {
        // Arrange
        var json = @"{
            ""protocolVersion"": ""1.0.0"",
            ""policyId"": ""age_over_18"",
            ""policyVersion"": ""1.2.0"",
            ""origin"": ""https://example.com"",
            ""nonce"": ""abc123"",
            ""issuedAt"": 1707659400,
            ""proof"": {
                ""piA"": [""1"", ""2"", ""3""],
                ""piB"": [[""1"", ""2""], [""3"", ""4""], [""5"", ""6""]],
                ""piC"": [""7"", ""8"", ""9""]
            },
            ""publicSignals"": [""1"", ""2"", ""3"", ""4"", ""5"", ""6"", ""7""],
            ""credentialHash"": ""sha256:abc123"",
            ""policyHash"": ""sha256:def456"",
            ""signature"": ""signature-value""
        }";

        // Act
        var envelope = CanonicalJsonEncoder.Parse(json);

        // Assert
        Assert.NotNull(envelope);
        Assert.Equal("1.0.0", envelope.ProtocolVersion);
        Assert.Equal("age_over_18", envelope.PolicyId);
        Assert.Equal("signature-value", envelope.Signature);
    }

    [Fact]
    public void Parse_InvalidJson_ReturnsNull()
    {
        // Arrange
        var invalidJson = "{ this is not valid json }";

        // Act
        var envelope = CanonicalJsonEncoder.Parse(invalidJson);

        // Assert
        Assert.Null(envelope);
    }

    [Fact]
    public void Parse_MissingRequiredField_ReturnsNull()
    {
        // Arrange - Missing policyId
        var json = @"{
            ""protocolVersion"": ""1.0.0"",
            ""origin"": ""https://example.com""
        }";

        // Act
        var envelope = CanonicalJsonEncoder.Parse(json);

        // Assert
        Assert.Null(envelope);
    }

    private ProofEnvelope CreateEnvelope(string? signature = null)
    {
        return new ProofEnvelope
        {
            ProtocolVersion = "1.0.0",
            PolicyId = "age_over_18",
            PolicyVersion = "1.2.0",
            Origin = "https://example.com",
            Nonce = "a3f8b2c1e5d7f9a1b3c5d7e9f1a3b5c7",
            IssuedAt = 1707659400,
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
            PublicSignals = new List<string> { "1", "2", "3", "4", "5", "6", "7" },
            CredentialHash = "sha256:abc123",
            PolicyHash = "sha256:def456",
            Signature = signature
        };
    }
}
