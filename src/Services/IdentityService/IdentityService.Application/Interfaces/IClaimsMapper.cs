using IdentityService.Application.DTOs;

namespace IdentityService.Application.Interfaces;

public interface IClaimsMapper
{
    string ProviderId { get; }
    AgeVerificationDto MapToAgeVerification(SessionDataDto sessionData);
}
