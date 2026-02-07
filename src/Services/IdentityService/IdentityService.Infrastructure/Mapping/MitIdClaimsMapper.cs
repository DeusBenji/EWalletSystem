using System.Globalization;
using IdentityService.Application.DTOs;
using IdentityService.Domain.Exceptions;
using IdentityService.Application.Interfaces;
using Microsoft.Extensions.Logging;
using IdentityService.Domain.Interfaces;

namespace IdentityService.Infrastructure.Mapping;

/// <summary>
/// Claims mapper for MitID.
/// Extracts ONLY age verification status from session data.
/// Does NOT map or store CPR numbers, names, or addresses.
/// </summary>
public class MitIdClaimsMapper : IClaimsMapper
{
    private readonly ISafeLogger<MitIdClaimsMapper> _safeLogger;
    private readonly TimeProvider _timeProvider;

    public MitIdClaimsMapper(ISafeLogger<MitIdClaimsMapper> safeLogger, TimeProvider timeProvider)
    {
        _safeLogger = safeLogger;
        _timeProvider = timeProvider;
    }

    public string ProviderId => "mitid";

    public AgeVerificationDto MapToAgeVerification(SessionDataDto sessionResponse)
    {
        // CONTROLLED FAILURE: Return user-friendly error if dateOfBirth missing
        if (sessionResponse.Subject?.DateOfBirth == null)
        {
            // Safe logging: only providerId and sessionId (masked), NO claims
            _safeLogger.LogError(
                "Age verification failed: missing dateOfBirth attribute. ProviderId={ProviderId}, SessionId={SessionId}",
                "mitid",
                MaskSessionId(sessionResponse.Id));
            
            throw new AgeVerificationException(
                AgeVerificationErrorCode.MISSING_ATTRIBUTE,
                "Age verification requires dateOfBirth attribute from provider");
        }
        
        // STRICT: Use InvariantCulture and exact format (ISO 8601 YYYY-MM-DD from Signicat)
        if (!DateOnly.TryParseExact(
            sessionResponse.Subject.DateOfBirth,
            "yyyy-MM-dd",
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out var dob))
        {
            _safeLogger.LogError(
                "Invalid dateOfBirth format. ProviderId={ProviderId}, SessionId={SessionId}",
                "mitid",
                MaskSessionId(sessionResponse.Id));
            
            throw new AgeVerificationException(
                AgeVerificationErrorCode.INVALID_DATE_FORMAT,
                "Invalid date format from provider");
        }
        
        var isAdult = CalculateAge(dob) >= 18;
        // DOB is NOT stored - only used for calculation, then discarded
        
        // STRICT: Assert SubjectId is from subject.id ONLY
        if (string.IsNullOrWhiteSpace(sessionResponse.Subject.Id))
        {
            throw new AgeVerificationException(
                AgeVerificationErrorCode.MISSING_SUBJECT_ID,
                "Subject.Id is required from Signicat");
        }
        
        // Validate SubjectId is safe (ASCII/URL-safe, max 256 chars)
        if (sessionResponse.Subject.Id.Length > 256 || 
            !IsUrlSafe(sessionResponse.Subject.Id))
        {
            throw new AgeVerificationException(
                AgeVerificationErrorCode.INVALID_SUBJECT_ID,
                "Subject.Id contains invalid characters or is too long");
        }
        
        return new AgeVerificationDto
        {
            ProviderId = "mitid",
            SubjectId = sessionResponse.Subject.Id, // ONLY from subject.id
            IsAdult = isAdult,
            VerifiedAt = _timeProvider.GetUtcNow().UtcDateTime,
            AssuranceLevel = sessionResponse.Loa ?? "unknown",
            ExpiresAt = sessionResponse.ExpiresAt?.UtcDateTime
        };
    }

    private int CalculateAge(DateOnly birthDate)
    {
        var today = DateOnly.FromDateTime(_timeProvider.GetUtcNow().UtcDateTime);
        var age = today.Year - birthDate.Year;
        
        // Subtract one if birthday hasn't occurred this year
        if (birthDate > today.AddYears(-age))
            age--;
        
        return age;
    }
    
    private static bool IsUrlSafe(string value)
    {
        return value.All(c => char.IsLetterOrDigit(c) || c == '-' || c == '_');
    }

    private static string MaskSessionId(string? sessionId)
    {
        if (string.IsNullOrEmpty(sessionId) || sessionId.Length <= 8)
            return "***";
        return $"{sessionId[..4]}***{sessionId[^4..]}";
    }
}
