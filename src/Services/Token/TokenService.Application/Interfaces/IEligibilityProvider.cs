namespace TokenService.Application.Interfaces;

/// <summary>
/// Plugin interface for policy eligibility providers.
/// Each provider determines if an account is eligible for a specific policy.
/// </summary>
public interface IEligibilityProvider
{
    /// <summary>
    /// Policy identifier this provider handles (e.g., "age_over_18", "lawyer", "student")
    /// </summary>
    string PolicyId { get; }
    
    /// <summary>
    /// Check if an account is eligible for this policy.
    /// </summary>
    /// <param name="accountId">Account ID to check</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>True if eligible, false otherwise</returns>
    Task<bool> IsEligibleAsync(Guid accountId, CancellationToken ct = default);
    
    /// <summary>
    /// Get metadata about the policy (optional, for UI display)
    /// </summary>
    /// <returns>Policy metadata (description, requirements, etc.)</returns>
    PolicyMetadata GetMetadata();
}

/// <summary>
/// Metadata about a policy
/// </summary>
public record PolicyMetadata
{
    public required string PolicyId { get; init; }
    public required string DisplayName { get; init; }
    public required string Description { get; init; }
    public string? RequirementsDescription { get; init; }
}
