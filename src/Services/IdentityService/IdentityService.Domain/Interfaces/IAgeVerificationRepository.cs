using IdentityService.Domain.Model;

namespace IdentityService.Domain.Interfaces;

/// <summary>
/// Repository for managing age verification records
/// </summary>
public interface IAgeVerificationRepository
{
    /// <summary>
    /// Upsert age verification based on (ProviderId, SubjectId)
    /// If exists: update IsAdult, VerifiedAt, Loa, and metadata
    /// If not exists: insert new record
    /// </summary>
    Task<AgeVerification> UpsertVerificationAsync(AgeVerification verification);
    
    /// <summary>
    /// Get verification by pseudonymous generic subject ID
    /// </summary>
    Task<AgeVerification?> GetByProviderSubjectIdAsync(string providerId, string subjectId);
    
    /// <summary>
    /// Get verification by internal account ID (if linked)
    /// </summary>
    Task<AgeVerification?> GetByAccountIdAsync(Guid accountId);
}
