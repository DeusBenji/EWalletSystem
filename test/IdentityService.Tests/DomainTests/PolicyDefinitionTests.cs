using IdentityService.Domain.Entities;
using Xunit;

namespace IdentityService.Tests.DomainTests;

/// <summary>
/// Tests for PolicyDefinition domain entity.
/// Focuses on business logic, not database operations.
/// </summary>
public class PolicyDefinitionTests
{
    [Fact]
    public void ComputePolicyHash_ShouldBeDeterministic()
    {
        // Arrange
        var policy = new PolicyDefinition
        {
            PolicyId = "age_over_18",
            Version = "1.2.0",
            CircuitId = "age_verification_v1",
            VerificationKeyId = "vkey-age-v1-2024",
            VerificationKeyFingerprint = "sha256:abc123",
            CompatibleVersions = "^1.0.0",
            DefaultExpiry = "PT72H",
            RequiredPublicSignalsSchema = "{}"
        };

        // Act
        var hash1 = policy.ComputePolicyHash();
        var hash2 = policy.ComputePolicyHash();

        // Assert
        Assert.Equal(hash1, hash2);
        Assert.StartsWith("sha256:", hash1);
    }

    [Fact]
    public void ComputePolicyHash_DifferentVersions_ShouldProduceDifferentHashes()
    {
        // Arrange
        var policy1 = new PolicyDefinition
        {
            PolicyId = "age_over_18",
            Version = "1.0.0",
            CircuitId = "age_verification_v1",
            VerificationKeyId = "vkey-age-v1-2024",
            VerificationKeyFingerprint = "sha256:abc123",
            CompatibleVersions = "^1.0.0",
            DefaultExpiry = "PT72H",
            RequiredPublicSignalsSchema = "{}"
        };

        var policy2 = new PolicyDefinition
        {
            PolicyId = "age_over_18",
            Version = "2.0.0",
            CircuitId = "age_verification_v2",
            VerificationKeyId = "vkey-age-v2-2024",
            VerificationKeyFingerprint = "sha256:def456",
            CompatibleVersions = "^2.0.0",
            DefaultExpiry = "PT72H",
            RequiredPublicSignalsSchema = "{}"
        };

        // Act
        var hash1 = policy1.ComputePolicyHash();
        var hash2 = policy2.ComputePolicyHash();

        // Assert
        Assert.NotEqual(hash1, hash2);
    }

    [Fact]
    public void PolicyStatus_ShouldDefaultToActive()
    {
        // Arrange & Act
        var policy = new PolicyDefinition
        {
            PolicyId = "test_policy",
            Version = "1.0.0",
            CircuitId = "test_circuit",
            VerificationKeyId = "test_vkey",
            VerificationKeyFingerprint = "sha256:test",
            CompatibleVersions = "^1.0.0",
            DefaultExpiry = "PT24H",
            RequiredPublicSignalsSchema = "{}"
        };

        // Assert
        Assert.Equal(PolicyStatus.Active, policy.Status);
    }

    [Theory]
    [InlineData(PolicyStatus.Active)]
    [InlineData(PolicyStatus.Deprecated)]
    [InlineData(PolicyStatus.Blocked)]
    public void PolicyStatus_CanBeSetToAllValidStates(PolicyStatus status)
    {
        // Arrange
        var policy = new PolicyDefinition
        {
            PolicyId = "test_policy",
            Version = "1.0.0",
            CircuitId = "test_circuit",
            VerificationKeyId = "test_vkey",
            VerificationKeyFingerprint = "sha256:test",
            CompatibleVersions = "^1.0.0",
            DefaultExpiry = "PT24H",
            RequiredPublicSignalsSchema = "{}"
        };

        // Act
        policy.Status = status;

        // Assert
        Assert.Equal(status, policy.Status);
    }
}
