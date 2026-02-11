using IdentityService.Domain.Entities;
using IdentityService.Domain.ValueObjects;

namespace IdentityService.Application.Interfaces;

/// <summary>
/// Service for validating policy credentials.
/// Enforces security invariants including clock skew, key retirement, and expiry checks.
/// </summary>
public interface ICredentialValidationService
{
    /// <summary>
    /// Validates a credential's authenticity and validity
    /// </summary>
    /// <param name="credentialJwt">JWT credential to validate</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Validation result with success/failure and reason code</returns>
    Task<CredentialValidationResult> ValidateCredentialAsync(
        string credentialJwt,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of credential validation
/// </summary>
public record CredentialValidationResult
{
    public required bool Valid { get; init; }
    public required ValidationReasonCode ReasonCode { get; init; }
    public string? Message { get; init; }
    public PolicyCredential? Credential { get; init; }
}

/// <summary>
/// Reason codes for validation failure
/// </summary>
public enum ValidationReasonCode
{
    Valid = 0,
    
    // Clock/timestamp issues
    ClockSkewExceeded = 1,
    NotYetValid = 2,
    Expired = 3,
    
    // Key issues
    RetiredKeyUsed = 10,
    UnknownKey = 11,
    InvalidSignature = 12,
    
    // Format issues
    MalformedJwt = 20,
    MissingRequiredClaim = 21,
    
    // Other
    PolicyNotFound = 30,
    PolicyInactive = 31
}
