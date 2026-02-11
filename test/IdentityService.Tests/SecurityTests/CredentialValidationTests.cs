using IdentityService.Application.Interfaces;
using IdentityService.Domain.Entities;
using IdentityService.Infrastructure.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace IdentityService.Tests.SecurityTests;

/// <summary>
/// Tests for credential validation with focus on security invariants.
/// Verifies clock skew tolerance, retired key rejection, and nbf claim validation.
/// </summary>
public class CredentialValidationTests
{
    private readonly Mock<IKeyManagementService> _mockKeyManagement;
    private readonly Mock<IPolicyRegistry> _mockPolicyRegistry;
    private readonly CredentialValidationService _validationService;

    public CredentialValidationTests()
    {
        _mockKeyManagement = new Mock<IKeyManagementService>();
        _mockPolicyRegistry = new Mock<IPolicyRegistry>();
        _validationService = new CredentialValidationService(
            _mockKeyManagement.Object,
            _mockPolicyRegistry.Object,
            NullLogger<CredentialValidationService>.Instance);
    }

    [Fact]
    public async Task ValidateCredential_RetiredKey_ShouldReject()
    {
        // Arrange - SECURITY INVARIANT: Retired keys immediately invalid
        var retiredKey = CreateKey(KeyStatus.Retired);
        _mockKeyManagement
            .Setup(x => x.GetKeyByIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(retiredKey);

        var credentialJwt = CreateValidJwt(keyId: retiredKey.KeyId);

        // Act
        var result = await _validationService.ValidateCredentialAsync(credentialJwt);

        // Assert
        Assert.False(result.Valid);
        Assert.Equal(ValidationReasonCode.RetiredKeyUsed, result.ReasonCode);
    }

    [Fact]
    public async Task ValidateCredential_CurrentKey_ShouldAccept()
    {
        // Arrange
        var currentKey = CreateKey(KeyStatus.Current);
        _mockKeyManagement
            .Setup(x => x.GetKeyByIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(currentKey);

        var credentialJwt = CreateValidJwt(keyId: currentKey.KeyId);

        // Act
        var result = await _validationService.ValidateCredentialAsync(credentialJwt);

        // Assert
        Assert.True(result.Valid);
        Assert.Equal(ValidationReasonCode.Valid, result.ReasonCode);
    }

    [Fact]
    public async Task ValidateCredential_DeprecatedKeyWithinGrace_ShouldAccept()
    {
        // Arrange
        var deprecatedKey = CreateKey(KeyStatus.Deprecated);
        deprecatedKey.DeprecatedAt = DateTime.UtcNow.AddDays(-3); // 3 days ago, grace period 7 days
        
        _mockKeyManagement
            .Setup(x => x.GetKeyByIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(deprecatedKey);

        var credentialJwt = CreateValidJwt(keyId: deprecatedKey.KeyId);

        // Act
        var result = await _validationService.ValidateCredentialAsync(credentialJwt);

        // Assert
        Assert.True(result.Valid);
        Assert.Equal(ValidationReasonCode.Valid, result.ReasonCode);
    }

    [Fact]
    public async Task ValidateCredential_UnknownKey_ShouldReject()
    {
        // Arrange
        _mockKeyManagement
            .Setup(x => x.GetKeyByIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IssuerSigningKey?)null);

        var credentialJwt = CreateValidJwt(keyId: "unknown-key-123");

        // Act
        var result = await _validationService.ValidateCredentialAsync(credentialJwt);

        // Assert
        Assert.False(result.Valid);
        Assert.Equal(ValidationReasonCode.UnknownKey, result.ReasonCode);
    }

    [Fact]
    public async Task ValidateCredential_ClockSkew3Minutes_ShouldAccept()
    {
        // Arrange - Within ±5 min tolerance
        var key = CreateKey(KeyStatus.Current);
        _mockKeyManagement
            .Setup(x => x.GetKeyByIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(key);

        var issuedAt = DateTime.UtcNow.AddMinutes(-3); // 3 minutes ago
        var credentialJwt = CreateValidJwt(keyId: key.KeyId, issuedAt: issuedAt);

        // Act
        var result = await _validationService.ValidateCredentialAsync(credentialJwt);

        // Assert
        Assert.True(result.Valid);
    }

    [Fact]
    public async Task ValidateCredential_ClockSkew10Minutes_ShouldReject()
    {
        // Arrange - Exceeds ±5 min tolerance
        var key = CreateKey(KeyStatus.Current);
        _mockKeyManagement
            .Setup(x => x.GetKeyByIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(key);

        var issuedAt = DateTime.UtcNow.AddMinutes(-10); // 10 minutes ago
        var credentialJwt = CreateValidJwt(keyId: key.KeyId, issuedAt: issuedAt);

        // Act
        var result = await _validationService.ValidateCredentialAsync(credentialJwt);

        // Assert
        Assert.False(result.Valid);
        Assert.Equal(ValidationReasonCode.ClockSkewExceeded, result.ReasonCode);
    }

    [Fact]
    public async Task ValidateCredential_FutureDateClockSkew_ShouldReject()
    {
        // Arrange - Credential issued in future (attacker clock manipulation)
        var key = CreateKey(KeyStatus.Current);
        _mockKeyManagement
            .Setup(x => x.GetKeyByIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(key);

        var issuedAt = DateTime.UtcNow.AddMinutes(10); // 10 minutes in future
        var credentialJwt = CreateValidJwt(keyId: key.KeyId, issuedAt: issuedAt);

        // Act
        var result = await _validationService.ValidateCredentialAsync(credentialJwt);

        // Assert
        Assert.False(result.Valid);
        Assert.Equal(ValidationReasonCode.ClockSkewExceeded, result.ReasonCode);
    }

    [Fact]
    public async Task ValidateCredential_NotBeforeInFuture_ShouldReject()
    {
        // Arrange
        var key = CreateKey(KeyStatus.Current);
        _mockKeyManagement
            .Setup(x => x.GetKeyByIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(key);

        var issuedAt = DateTime.UtcNow;
        var notBefore = DateTime.UtcNow.AddMinutes(10); // Not valid for 10 more minutes
        var credentialJwt = CreateValidJwt(keyId: key.KeyId, issuedAt: issuedAt, notBefore: notBefore);

        // Act
        var result = await _validationService.ValidateCredentialAsync(credentialJwt);

        // Assert
        Assert.False(result.Valid);
        Assert.Equal(ValidationReasonCode.NotYetValid, result.ReasonCode);
    }

    [Fact]
    public async Task ValidateCredential_NotBeforeInPast_ShouldAccept()
    {
        // Arrange
        var key = CreateKey(KeyStatus.Current);
        _mockKeyManagement
            .Setup(x => x.GetKeyByIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(key);

        var issuedAt = DateTime.UtcNow;
        var notBefore = DateTime.UtcNow.AddMinutes(-5); // Valid 5 minutes ago
        var credentialJwt = CreateValidJwt(keyId: key.KeyId, issuedAt: issuedAt, notBefore: notBefore);

        // Act
        var result = await _validationService.ValidateCredentialAsync(credentialJwt);

        // Assert
        Assert.True(result.Valid);
    }

    [Fact]
    public async Task ValidateCredential_Expired_ShouldReject()
    {
        // Arrange
        var key = CreateKey(KeyStatus.Current);
        _mockKeyManagement
            .Setup(x => x.GetKeyByIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(key);

        var issuedAt = DateTime.UtcNow.AddDays(-4);
        var expiresAt = DateTime.UtcNow.AddDays(-1); // Expired yesterday
        var credentialJwt = CreateValidJwt(
            keyId: key.KeyId,
            issuedAt: issuedAt,
            expiresAt: expiresAt);

        // Act
        var result = await _validationService.ValidateCredentialAsync(credentialJwt);

        // Assert
        Assert.False(result.Valid);
        Assert.Equal(ValidationReasonCode.Expired, result.ReasonCode);
    }

    [Fact]
    public async Task ValidateCredential_MalformedJwt_ShouldReject()
    {
        // Arrange
        var malformedJwt = "not.a.valid.jwt.format";

        // Act
        var result = await _validationService.ValidateCredentialAsync(malformedJwt);

        // Assert
        Assert.False(result.Valid);
        Assert.Equal(ValidationReasonCode.MalformedJwt, result.ReasonCode);
    }

    private IssuerSigningKey CreateKey(KeyStatus status)
    {
        return new IssuerSigningKey
        {
            KeyId = $"key-test-{Guid.NewGuid()}",
            Algorithm = "ES256",
            PublicKeyJwk = "{}",
            EncryptedPrivateKey = "encrypted",
            Status = status,
            CreatedAt = DateTime.UtcNow
        };
    }

    private string CreateValidJwt(
        string keyId,
        DateTime? issuedAt = null,
        DateTime? expiresAt = null,
        DateTime? notBefore = null)
    {
        issuedAt ??= DateTime.UtcNow;
        expiresAt ??= DateTime.UtcNow.AddDays(3);

        var header = new
        {
            alg = "ES256",
            typ = "JWT",
            kid = keyId
        };

        var payload = new
        {
            jti = Guid.NewGuid().ToString(),
            sub = "user-123",
            iat = new DateTimeOffset(issuedAt.Value).ToUnixTimeSeconds(),
            exp = new DateTimeOffset(expiresAt.Value).ToUnixTimeSeconds(),
            nbf = notBefore.HasValue ? new DateTimeOffset(notBefore.Value).ToUnixTimeSeconds() : (long?)null,
            policyId = "age_over_18",
            policyHash = "sha256:abc123",
            claims = new { birthYear = 1990 }
        };

        var headerBase64 = Base64UrlEncode(System.Text.Json.JsonSerializer.Serialize(header));
        var payloadBase64 = Base64UrlEncode(System.Text.Json.JsonSerializer.Serialize(payload));
        var signature = Base64UrlEncode("test-signature");

        return $"{headerBase64}.{payloadBase64}.{signature}";
    }

    private string Base64UrlEncode(string input)
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes(input);
        return Convert.ToBase64String(bytes)
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');
    }
}
