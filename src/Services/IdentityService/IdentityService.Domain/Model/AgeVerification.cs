namespace IdentityService.Domain.Model;

/// <summary>
/// Privacy-first age verification record
/// Stores ONLY verification status and pseudonymous identifiers
/// NO personal data (CPR, NationalId, DateOfBirth, Name, Address)
/// </summary>
public class AgeVerification
{
    public Guid Id { get; set; }
    
    /// <summary>
    /// Optional: Internal account ID if linked to a main account
    /// Nullable because IdentityService may be valid for standalone verification
    /// </summary>
    public Guid? AccountId { get; set; }
    
    /// <summary>
    /// Provider that performed verification (mitid, sbid, nbid)
    /// </summary>
    public string ProviderId { get; set; } = null!;
    
    /// <summary>
    /// Pseudonymous subject identifier from Signicat's subject.id
    /// Max length: 256 chars, URL-safe characters only
    /// </summary>
    public string SubjectId { get; set; } = null!;
    
    /// <summary>
    /// Whether the subject is adult (18+)
    /// </summary>
    public bool IsAdult { get; set; }
    
    /// <summary>
    /// When this verification occurred
    /// </summary>
    public DateTime VerifiedAt { get; set; }
    
    /// <summary>
    /// Level of Assurance from the provider
    /// </summary>
    public string AssuranceLevel { get; set; } = "substantial";
    
    /// <summary>
    /// Optional: when this verification expires
    /// FUTURE-PROOFING: Allows policy changes (e.g., "re-verify every 6 months")
    /// </summary>
    public DateTime? ExpiresAt { get; set; }
    
    /// <summary>
    /// Optional: policy version applied during verification
    /// FUTURE-PROOFING: Allows invalidating old verifications when rules change
    /// </summary>
    public string? PolicyVersion { get; set; }
    
    /// <summary>
    /// When this record was created
    /// </summary>
    public DateTime CreatedAt { get; set; }
    
    /// <summary>
    /// When this record was last updated
    /// </summary>
    public DateTime UpdatedAt { get; set; }
}
