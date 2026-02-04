namespace Api.Contracts;

/// <summary>
/// Request contract for issuing a policy-based credential
/// </summary>
public record IssuePolicyCredentialRequestContract
{
    /// <summary>
    /// Account ID requesting the credential
    /// </summary>
    public required Guid AccountId { get; init; }
    
    /// <summary>
    /// Policy identifier (e.g., "age_over_18", "lawyer", "student")
    /// </summary>
    public required string PolicyId { get; init; }
    
    /// <summary>
    /// Subject commitment (Poseidon(walletSecret))
    /// </summary>
    public required string SubjectCommitment { get; init; }
}
