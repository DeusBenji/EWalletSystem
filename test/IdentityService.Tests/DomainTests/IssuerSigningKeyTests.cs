using IdentityService.Domain.Entities;
using Xunit;

namespace IdentityService.Tests.DomainTests;

/// <summary>
/// Tests for IssuerSigningKey entity.
/// Verifies key lifecycle and verification logic.
/// </summary>
public class IssuerSigningKeyTests
{
    [Fact]
    public void CanSign_CurrentKey_ReturnsTrue()
    {
        // Arrange
        var key = CreateKey(KeyStatus.Current);

        // Act & Assert
        Assert.True(key.CanSign());
    }

    [Fact]
    public void CanSign_DeprecatedKey_ReturnsFalse()
    {
        // Arrange
        var key = CreateKey(KeyStatus.Deprecated);

        // Act & Assert
        Assert.False(key.CanSign());
    }

    [Fact]
    public void CanSign_RetiredKey_ReturnsFalse()
    {
        // Arrange
        var key = CreateKey(KeyStatus.Retired);

        // Act & Assert
        Assert.False(key.CanSign());
    }

    [Fact]
    public void CanVerify_CurrentKey_ReturnsTrue()
    {
        // Arrange
        var key = CreateKey(KeyStatus.Current);

        // Act & Assert
        Assert.True(key.CanVerify());
    }

    [Fact]
    public void CanVerify_DeprecatedKeyWithinGracePeriod_ReturnsTrue()
    {
        // Arrange
        var key = CreateKey(KeyStatus.Deprecated);
        key.DeprecatedAt = DateTime.UtcNow.AddDays(-3); // 3 days ago, grace period 7 days

        // Act & Assert
        Assert.True(key.CanVerify());
    }

    [Fact]
    public void CanVerify_DeprecatedKeyOutsideGracePeriod_ReturnsFalse()
    {
        // Arrange
        var key = CreateKey(KeyStatus.Deprecated);
        key.DeprecatedAt = DateTime.UtcNow.AddDays(-8); // 8 days ago, grace period 7 days

        // Act & Assert
        Assert.False(key.CanVerify());
    }

    [Fact]
    public void CanVerify_RetiredKey_ReturnsFalse()
    {
        // Arrange
        var key = CreateKey(KeyStatus.Retired);

        // Act & Assert
        Assert.False(key.CanVerify());
    }

    [Fact]
    public void ShouldBeRetired_DeprecatedKeyPastGracePeriod_ReturnsTrue()
    {
        // Arrange
        var key = CreateKey(KeyStatus.Deprecated);
        key.DeprecatedAt = DateTime.UtcNow.AddDays(-8); // 8 days ago, grace period 7 days

        // Act & Assert
        Assert.True(key.ShouldBeRetired());
    }

    [Fact]
    public void ShouldBeRetired_DeprecatedKeyWithinGracePeriod_ReturnsFalse()
    {
        // Arrange
        var key = CreateKey(KeyStatus.Deprecated);
        key.DeprecatedAt = DateTime.UtcNow.AddDays(-3); // 3 days ago, grace period 7 days

        // Act & Assert
        Assert.False(key.ShouldBeRetired());
    }

    [Fact]
    public void ShouldBeRetired_CurrentKey_ReturnsFalse()
    {
        // Arrange
        var key = CreateKey(KeyStatus.Current);

        // Act & Assert
        Assert.False(key.ShouldBeRetired());
    }

    [Fact]
    public void GracePeriod_DefaultsTo7Days()
    {
        // Arrange & Act
        var key = CreateKey(KeyStatus.Current);

        // Assert
        Assert.Equal(TimeSpan.FromDays(7), key.GracePeriod);
    }

    private IssuerSigningKey CreateKey(KeyStatus status)
    {
        return new IssuerSigningKey
        {
            KeyId = "test-key-123",
            Algorithm = "ES256",
            PublicKeyJwk = "{}",
            EncryptedPrivateKey = "encrypted",
            Status = status,
            CreatedAt = DateTime.UtcNow
        };
    }
}
