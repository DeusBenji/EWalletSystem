namespace Application.DTOs;

/// <summary>
/// Request DTO for issuing a policy-based credential
/// </summary>
public record IssuePolicyCredentialDto
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
    /// Binds the credential to the wallet without revealing the secret
    /// </summary>
    public required string SubjectCommitment { get; init; }
}
