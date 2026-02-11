using IdentityService.Domain.Entities;
using Xunit;

namespace IdentityService.Tests.DomainTests;

/// <summary>
/// Tests for PolicyAttestation domain entity.
/// Verifies expiry logic and validation methods.
/// </summary>
public class PolicyAttestationTests
{
    [Fact]
    public void IsValid_UnexpiredAndVerified_ReturnsTrue()
    {
        // Arrange
        var attestation = new PolicyAttestation
        {
            PolicyId = "age_over_18",
            SubjectId = "user-123",
            ProviderId = "mitid",
            Verified = true,
            VerifiedAt = DateTime.UtcNow.AddHours(-1),
            ExpiresAt = DateTime.UtcNow.AddHours(23) // 23 hours remaining
        };

        // Act & Assert
        Assert.True(attestation.IsValid());
    }

    [Fact]
    public void IsValid_Expired_ReturnsFalse()
    {
        // Arrange
        var attestation = new PolicyAttestation
        {
            PolicyId = "age_over_18",
            SubjectId = "user-123",
            ProviderId = "mitid",
            Verified = true,
            VerifiedAt = DateTime.UtcNow.AddHours(-25),
            ExpiresAt = DateTime.UtcNow.AddHours(-1) // Expired 1 hour ago
        };

        // Act & Assert
        Assert.False(attestation.IsValid());
    }

    [Fact]
    public void IsValid_NotVerified_ReturnsFalse()
    {
        // Arrange
        var attestation = new PolicyAttestation
        {
            PolicyId = "age_over_18",
            SubjectId = "user-123",
            ProviderId = "mitid",
            Verified = false, // User failed verification
            VerifiedAt = DateTime.UtcNow.AddHours(-1),
            ExpiresAt = DateTime.UtcNow.AddHours(23)
        };

        // Act & Assert
        Assert.False(attestation.IsValid());
    }

    [Fact]
    public void IsExpired_BeforeExpiry_ReturnsFalse()
    {
        // Arrange
        var attestation = new PolicyAttestation
        {
            PolicyId = "age_over_18",
            SubjectId = "user-123",
            ProviderId = "mitid",
            Verified = true,
            VerifiedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(1)
        };

        // Act & Assert
        Assert.False(attestation.IsExpired());
    }

    [Fact]
    public void IsExpired_AfterExpiry_ReturnsTrue()
    {
        // Arrange
        var attestation = new PolicyAttestation
        {
            PolicyId = "age_over_18",
            SubjectId = "user-123",
            ProviderId = "mitid",
            Verified = true,
            VerifiedAt = DateTime.UtcNow.AddDays(-3),
            ExpiresAt = DateTime.UtcNow.AddSeconds(-1) // Just expired
        };

        // Act & Assert
        Assert.True(attestation.IsExpired());
    }

    [Fact]
    public void TimeUntilExpiry_ShouldCalculateCorrectly()
    {
        // Arrange
        var expiresAt = DateTime.UtcNow.AddHours(12);
        var attestation = new PolicyAttestation
        {
            PolicyId = "age_over_18",
            SubjectId = "user-123",
            ProviderId = "mitid",
            Verified = true,
            VerifiedAt = DateTime.UtcNow,
            ExpiresAt = expiresAt
        };

        // Act
        var timeRemaining = attestation.TimeUntilExpiry();

        // Assert
        Assert.InRange(timeRemaining.TotalHours, 11.9, 12.1); // Allow small time delta
    }

    [Fact]
    public void AssuranceLevel_ShouldDefaultToSubstantial()
    {
        // Arrange & Act
        var attestation = new PolicyAttestation
        {
            PolicyId = "age_over_18",
            SubjectId = "user-123",
            ProviderId = "mitid",
            Verified = true,
            VerifiedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(3)
        };

        // Assert
        Assert.Equal("substantial", attestation.AssuranceLevel);
    }
}
