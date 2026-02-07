using IdentityService.Application.DTOs;
using IdentityService.Domain.Exceptions;
namespace IdentityService.Application.Interfaces;

/// <summary>
/// Maps provider session response to age verification DTO
/// PRIVACY-FIRST: Extracts only age verification status, discards all personal data
/// </summary>
public interface IClaimsMapper
{
    /// <summary>
    /// Provider this mapper handles (mitid, sbid, nbid)
    /// </summary>
    string ProviderId { get; }
    
    /// <summary>
    /// Map Signicat session response to minimal age verification DTO
    /// Uses dateOfBirth for age calculation, then DISCARDS it
    /// Returns ONLY verification result and pseudonymous identifiers
    /// </summary>
    /// <param name="sessionResponse">Session response from Signicat</param>
    /// <returns>Minimal age verification DTO (no personal data)</returns>
    /// <exception cref="AgeVerificationException">
    /// Thrown with specific error code if:
    /// - dateOfBirth attribute missing (MISSING_ATTRIBUTE)
    /// - dateOfBirth format invalid (INVALID_DATE_FORMAT)
    /// - subject.id missing (MISSING_SUBJECT_ID)
    /// - subject.id invalid format (INVALID_SUBJECT_ID)
    /// </exception>
    AgeVerificationDto MapToAgeVerification(SessionDataDto sessionResponse);
}
