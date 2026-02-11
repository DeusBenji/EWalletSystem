using IdentityService.Domain.Entities;

namespace IdentityService.Application.Interfaces;

/// <summary>
/// Repository interface for managing policy definitions in the system.
/// Policy definitions are versioned cryptographic contracts that specify what can be proven.
/// </summary>
public interface IPolicyRegistry
{
    /// <summary>
    /// Retrieves a policy definition by its ID and version
    /// </summary>
    /// <param name="policyId">Policy identifier (e.g., "age_over_18")</param>
    /// <param name="version">Semantic version (e.g., "1.2.0"). If null, returns the latest active version.</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Policy definition if found, null otherwise</returns>
    Task<PolicyDefinition?> GetPolicyAsync(
        string policyId,
        string? version = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all active policies
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of active policy definitions</returns>
    Task<IReadOnlyList<PolicyDefinition>> GetActivePoliciesAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all versions of a specific policy
    /// </summary>
    /// <param name="policyId">Policy identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of all policy versions, ordered by version descending</returns>
    Task<IReadOnlyList<PolicyDefinition>> GetPolicyVersionsAsync(
        string policyId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a policy version is compatible with a given version range
    /// </summary>
    /// <param name="policyId">Policy identifier</param>
    /// <param name="version">Policy version to check</param>
    /// <param name="compatibleRange">Semver range (e.g., "^1.0.0", "1.x")</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if version is compatible, false otherwise</returns>
    Task<bool> IsCompatibleVersionAsync(
        string policyId,
        string version,
        string compatibleRange,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new policy definition
    /// </summary>
    /// <param name="policy">Policy definition to create</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created policy definition</returns>
    Task<PolicyDefinition> CreatePolicyAsync(
        PolicyDefinition policy,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the status of a policy (active → deprecated → blocked)
    /// IMPORTANT: Status transitions are logged for audit purposes
    /// </summary>
    /// <param name="policyId">Policy identifier</param>
    /// <param name="version">Policy version</param>
    /// <param name="newStatus">New status</param>
    /// <param name="reason">Reason for status change (for audit log)</param>
    /// <param name="actor">Who is making the change (for audit log)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated policy definition</returns>
    Task<PolicyDefinition> UpdatePolicyStatusAsync(
        string policyId,
        string version,
        PolicyStatus newStatus,
        string reason,
        string actor,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Signs a policy definition with the issuer's signing key
    /// This allows websites to verify the authenticity of policy metadata
    /// </summary>
    /// <param name="policy">Policy to sign</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Policy with signature populated</returns>
    Task<PolicyDefinition> SignPolicyAsync(
        PolicyDefinition policy,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Verifies the signature of a policy definition
    /// </summary>
    /// <param name="policy">Policy to verify</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if signature is valid, false otherwise</returns>
    Task<bool> VerifyPolicySignatureAsync(
        PolicyDefinition policy,
        CancellationToken cancellationToken = default);
}
