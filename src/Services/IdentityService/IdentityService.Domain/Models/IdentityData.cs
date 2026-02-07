namespace IdentityService.Domain.Models;

/// <summary>
/// Legacy identity data container to satisfy IIdentityProvider contract.
/// Intentionally minimal/empty as new flow uses AgeVerificationDto.
/// </summary>
public sealed class IdentityData
{
    // Intentionally left minimal to satisfy compilation.
    // Real data flow is handled via ISignicatAuthService -> AgeVerificationDto
    public string ProviderId { get; init; } = string.Empty;
    public string Subject { get; init; } = string.Empty;
}
