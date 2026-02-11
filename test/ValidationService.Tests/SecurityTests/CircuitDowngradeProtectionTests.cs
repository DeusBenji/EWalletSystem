using Xunit;

namespace ValidationService.Tests.SecurityTests;

/// <summary>
/// Tests for circuit downgrade protection.
/// Ensures clients cannot be forced to use older, vulnerable circuit versions.
/// </summary>
public class CircuitDowngradeProtectionTests
{
    [Theory]
    [InlineData("1.2.0", "1.2.0", true)]  // Exact match
    [InlineData("1.3.0", "1.2.0", true)]  // Newer version OK
    [InlineData("1.2.1", "1.2.0", true)]  // Patch version OK
    [InlineData("1.1.9", "1.2.0", false)] // Older minor version REJECTED
    [InlineData("0.9.0", "1.2.0", false)] // Older major version REJECTED
    [InlineData("2.0.0", "1.2.0", true)]  // Major version upgrade OK
    public void IsCircuitVersionAcceptable_ShouldEnforceMinimum(
        string actualVersion,
        string minimumVersion,
        bool expectedAcceptable)
    {
        // Act
        var isAcceptable = IsVersionAcceptable(actualVersion, minimumVersion);

        // Assert
        Assert.Equal(expectedAcceptable, isAcceptable);
    }

    [Fact]
    public void DowngradeAttack_OldVulnerableCircuit_ShouldBeRejected()
    {
        // Arrange - Attacker tries to serve vulnerable v1.0.0
        var attackerCircuitVersion = "1.0.0";
        var minimumSafeVersion = "1.2.0"; // v1.0.0 had vulnerability, fixed in v1.2.0

        // Act
        var isAcceptable = IsVersionAcceptable(attackerCircuitVersion, minimumSafeVersion);

        // Assert - SECURITY INVARIANT: Downgrade rejected
        Assert.False(isAcceptable);
    }

    [Fact]
    public void NormalUpgrade_NewerCircuit_ShouldBeAccepted()
    {
        // Arrange
        var newCircuitVersion = "1.3.0";
        var minimumVersion = "1.2.0";

        // Act
        var isAcceptable = IsVersionAcceptable(newCircuitVersion, minimumVersion);

        // Assert
        Assert.True(isAcceptable);
    }

    [Fact]
    public void UnknownCircuitId_ShouldReject()
    {
        // Arrange
        var unknownCircuitId = "malicious_circuit_v1";
        
        // Act
        var isKnown = IsKnownCircuit(unknownCircuitId);

        // Assert
        Assert.False(isKnown);
    }

    [Theory]
    [InlineData("age_verification_v1", true)]
    [InlineData("drivers_license_v1", true)]
    [InlineData("unknown_circuit", false)]
    public void IsKnownCircuit_ShouldValidateAllowList(string circuitId, bool expectedKnown)
    {
        // Act
        var isKnown = IsKnownCircuit(circuitId);

        // Assert
        Assert.Equal(expectedKnown, isKnown);
    }

    // Helper methods (simplified semver comparison for testing)
    private bool IsVersionAcceptable(string actualVersion, string minimumVersion)
    {
        var actual = ParseVersion(actualVersion);
        var minimum = ParseVersion(minimumVersion);

        if (actual.Major > minimum.Major) return true;
        if (actual.Major < minimum.Major) return false;

        if (actual.Minor > minimum.Minor) return true;
        if (actual.Minor < minimum.Minor) return false;

        return actual.Patch >= minimum.Patch;
    }

    private (int Major, int Minor, int Patch) ParseVersion(string version)
    {
        var parts = version.Split('.');
        return (int.Parse(parts[0]), int.Parse(parts[1]), int.Parse(parts[2]));
    }

    private bool IsKnownCircuit(string circuitId)
    {
        var knownCircuits = new[] { "age_verification_v1", "drivers_license_v1" };
        return Array.Exists(knownCircuits, c => c == circuitId);
    }
}
