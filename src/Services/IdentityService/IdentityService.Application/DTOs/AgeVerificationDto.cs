namespace IdentityService.Application.DTOs;

/// <summary>
/// Minimal age verification data (privacy-first)
/// Contains ONLY verification result and pseudonymous identifiers
/// </summary>
public class AgeVerificationDto
{
    /// <summary>
    /// Provider that performed verification (mitid, sbid, nbid)
    /// </summary>
    public required string ProviderId { get; init; }
    
    /// <summary>
    /// Pseudonymous subject identifier from Signicat's subject.id
    /// NOT a national ID, CPR, or other personal identifier
    /// </summary>
    public required string SubjectId { get; init; }
    
    /// <summary>
    /// Whether the subject is adult (18+)
    /// </summary>
    public required bool IsAdult { get; init; }
    
    /// <summary>
    /// When this verification occurred
    /// </summary>
    public required DateTime VerifiedAt { get; init; }
    
    /// <summary>
    /// Level of Assurance from the provider
    /// </summary>
    public required string AssuranceLevel { get; init; }
    
    /// <summary>
    /// Optional: when this verification expires
    /// </summary>
    public DateTime? ExpiresAt { get; init; }
}
