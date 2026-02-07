using System.Globalization;
using IdentityService.Application.DTOs;
using IdentityService.Domain.Exceptions;
using IdentityService.Application.Interfaces;
using Microsoft.Extensions.Logging;
using IdentityService.Domain.Interfaces;

namespace IdentityService.Infrastructure.Mapping;

/// <summary>
/// Claims mapper for Swedish BankID.
/// Extracts ONLY age verification status from session data.
/// Uses dateOfBirth from Signicat, DOES NOT parse personnummer.
/// </summary>
public class BankIdSeClaimsMapper : IClaimsMapper
{
    private readonly ISafeLogger<BankIdSeClaimsMapper> _safeLogger;
    private readonly TimeProvider _timeProvider;

    public BankIdSeClaimsMapper(ISafeLogger<BankIdSeClaimsMapper> safeLogger, TimeProvider timeProvider)
    {
        _safeLogger = safeLogger;
        _timeProvider = timeProvider;
    }

    public string ProviderId => "sbid";

    public AgeVerificationDto MapToAgeVerification(SessionDataDto sessionResponse)
    {
        // CONTROLLED FAILURE: Return user-friendly error if dateOfBirth missing
        if (sessionResponse.Subject?.DateOfBirth == null)
        {
            _safeLogger.LogError(
                "Age verification failed: missing dateOfBirth attribute. ProviderId={ProviderId}, SessionId={SessionId}",
                "sbid",
                MaskSessionId(sessionResponse.Id));
            
            throw new AgeVerificationException(
                AgeVerificationErrorCode.MISSING_ATTRIBUTE,
                "Swedish BankID requires dateOfBirth attribute");
        }
        
        if (!DateOnly.TryParseExact(
            sessionResponse.Subject.DateOfBirth,
            "yyyy-MM-dd",
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out var dob))
        {
             _safeLogger.LogError(
                "Invalid dateOfBirth format. ProviderId={ProviderId}, SessionId={SessionId}",
                "sbid",
                MaskSessionId(sessionResponse.Id));

            throw new AgeVerificationException(
                AgeVerificationErrorCode.INVALID_DATE_FORMAT,
                "Invalid date format from provider");
        }
        
        var isAdult = CalculateAge(dob) >= 18;
        
        if (string.IsNullOrWhiteSpace(sessionResponse.Subject.Id))
        {
             throw new AgeVerificationException(
                AgeVerificationErrorCode.MISSING_SUBJECT_ID,
                "Subject.Id is required from Signicat");
        }

        return new AgeVerificationDto
        {
            ProviderId = "sbid",
            SubjectId = sessionResponse.Subject.Id,
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
        if (birthDate > today.AddYears(-age)) age--;
        return age;
    }

    private static string MaskSessionId(string? sessionId)
    {
        if (string.IsNullOrEmpty(sessionId) || sessionId.Length <= 8)
            return "***";
        return $"{sessionId[..4]}***{sessionId[^4..]}";
    }
}
