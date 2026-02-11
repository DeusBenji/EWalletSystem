using ValidationService.Protocol;
using Xunit;

namespace ValidationService.Tests.ProtocolTests;

/// <summary>
/// Tests for anti-downgrade enforcement.
/// Ensures malicious websites cannot force users to use vulnerable circuit versions.
/// </summary>
public class AntiDowngradeTests
{
    [Fact]
    public void MinimumPolicyVersions_UnknownPolicy_ReturnsFalse()
    {
        // Act
        var isAcceptable = MinimumPolicyVersions.IsVersionAcceptable("unknown_policy", "1.0.0");

        // Assert
        Assert.False(isAcceptable);
    }

    [Theory]
    [InlineData("age_over_18", "1.2.0", true)]   // Exact minimum
    [InlineData("age_over_18", "1.3.0", true)]   // Above minimum
    [InlineData("age_over_18", "2.0.0", true)]   // Major version upgrade
    [InlineData("age_over_18", "1.1.9", false)]  // Below minimum (BLOCKED)
    [InlineData("age_over_18", "1.0.0", false)]  // Vulnerable version (BLOCKED)
    [InlineData("age_over_18", "0.9.0", false)]  // Old major version (BLOCKED)
    public void MinimumPolicyVersions_VersionComparison(
        string policyId,
        string version,
        bool expectedAcceptable)
    {
        // Act
        var isAcceptable = MinimumPolicyVersions.IsVersionAcceptable(policyId, version);

        // Assert
        Assert.Equal(expectedAcceptable, isAcceptable);
    }

    [Fact]
    public void DowngradeAttack_VulnerableVersion_ShouldBeRejected()
    {
        // Arrange - Attacker tries to force vulnerable v1.0.0
        var attackVersion = "1.0.0";
        var policyId = "age_over_18";

        // Act
        var isAcceptable = MinimumPolicyVersions.IsVersionAcceptable(policyId, attackVersion);

        // Assert - SECURITY INVARIANT: Downgrade rejected
        Assert.False(isAcceptable);
    }

    [Fact]
    public void NormalUpgrade_NewerVersion_ShouldBeAccepted()
    {
        // Arrange
        var upgradeVersion = "1.3.0";
        var policyId = "age_over_18";

        // Act
        var isAcceptable = MinimumPolicyVersions.IsVersionAcceptable(policyId, upgradeVersion);

        // Assert
        Assert.True(isAcceptable);
    }

    [Fact]
    public void GetMinimumVersion_KnownPolicy_ReturnsMinimum()
    {
        // Act
        var minimum = MinimumPolicyVersions.GetMinimumVersion("age_over_18");

        // Assert
        Assert.NotNull(minimum);
        Assert.Equal("1.2.0", minimum);
    }

    [Fact]
    public void GetMinimumVersion_UnknownPolicy_ReturnsNull()
    {
        // Act
        var minimum = MinimumPolicyVersions.GetMinimumVersion("unknown_policy");

        // Assert
        Assert.Null(minimum);
    }

    [Theory]
    [InlineData("drivers_license", "1.0.0", true)]   // Minimum met
    [InlineData("drivers_license", "1.1.0", true)]   // Above minimum
    [InlineData("drivers_license", "0.9.0", false)]  // Below minimum
    public void MinimumPolicyVersions_DriversLicense(
        string policyId,
        string version,
        bool expectedAcceptable)
    {
        // Act
        var isAcceptable = MinimumPolicyVersions.IsVersionAcceptable(policyId, version);

        // Assert
        Assert.Equal(expectedAcceptable, isAcceptable);
    }
}
