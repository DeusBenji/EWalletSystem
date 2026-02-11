using IdentityService.Domain.ValueObjects;
using Xunit;

namespace IdentityService.Tests.DomainTests;

/// <summary>
/// Tests for PolicyCredential value object.
/// Verifies JWT encoding and credential hash computation.
/// </summary>
public class PolicyCredentialTests
{
    [Fact]
    public void ComputeCredentialHash_ShouldBeDeterministic()
    {
        // Arrange
        var credential = CreateCredential();

        // Act
        var hash1 = credential.ComputeCredentialHash();
        var hash2 = credential.ComputeCredentialHash();

        // Assert
        Assert.Equal(hash1, hash2);
        Assert.StartsWith("sha256:", hash1);
    }

    [Fact]
    public void ComputeCredentialHash_DifferentCredentials_ProduceDifferentHashes()
    {
        // Arrange
        var credential1 = CreateCredential(credentialId: "cred-1");
        var credential2 = CreateCredential(credentialId: "cred-2");

        // Act
        var hash1 = credential1.ComputeCredentialHash();
        var hash2 = credential2.ComputeCredentialHash();

        // Assert
        Assert.NotEqual(hash1, hash2);
    }

    [Fact]
    public void ToJwt_ShouldContainAllRequiredFields()
    {
        // Arrange
        var credential = CreateCredential();
        var signature = "test-signature";

        // Act
        var jwt = credential.ToJwt(signature);

        // Assert
        var parts = jwt.Split('.');
        Assert.Equal(3, parts.Length); // header.payload.signature
        Assert.Equal(signature, parts[2]);
    }

    [Fact]
    public void ToJwt_ShouldIncludeKeyId()
    {
        // Arrange
        var credential = CreateCredential(keyId: "key-2026-02");
        var signature = "sig";

        // Act
        var jwt = credential.ToJwt(signature);

        // Assert
        // Decode header
        var headerBase64 = jwt.Split('.')[0];
        var headerJson = DecodeBase64Url(headerBase64);
        Assert.Contains("\"kid\":\"key-2026-02\"", headerJson);
    }

    [Fact]
    public void ToJwt_WithNotBefore_ShouldIncludeNbfClaim()
    {
        // Arrange
        var nbf = DateTime.UtcNow.AddMinutes(-5);
        var credential = new PolicyCredential
        {
            CredentialId = "cred-123",
            PolicyId = "age_over_18",
            SubjectId = "user-456",
            Claims = new Dictionary<string, object> { { "birthYear", 1990 } },
            IssuedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(3),
            NotBefore = nbf,
            IssuerSigningKeyId = "key-123",
            PolicyHash = "sha256:abc"
        };

        // Act
        var jwt = credential.ToJwt("sig");

        // Assert
        var payloadBase64 = jwt.Split('.')[1];
        var payloadJson = DecodeBase64Url(payloadBase64);
        Assert.Contains("\"nbf\":", payloadJson);
    }

    [Fact]
    public void ToJwt_WithoutNotBefore_ShouldOmitNbfClaim()
    {
        // Arrange
        var credential = CreateCredential();

        // Act
        var jwt = credential.ToJwt("sig");

        // Assert
        var payloadBase64 = jwt.Split('.')[1];
        var payloadJson = DecodeBase64Url(payloadBase64);
        Assert.DoesNotContain("\"nbf\":", payloadJson);
    }

    private PolicyCredential CreateCredential(
        string credentialId = "cred-123",
        string keyId = "key-123")
    {
        return new PolicyCredential
        {
            CredentialId = credentialId,
            PolicyId = "age_over_18",
            SubjectId = "user-456",
            Claims = new Dictionary<string, object>
            {
                { "birthYear", 1990 },
                { "isAdult", true }
            },
            IssuedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(3),
            IssuerSigningKeyId = keyId,
            PolicyHash = "sha256:abc123"
        };
    }

    private string DecodeBase64Url(string base64Url)
    {
        // Convert base64url to base64
        var base64 = base64Url.Replace('-', '+').Replace('_', '/');
        var padding = (4 - base64.Length % 4) % 4;
        base64 += new string('=', padding);

        var bytes = Convert.FromBase64String(base64);
        return System.Text.Encoding.UTF8.GetString(bytes);
    }
}
