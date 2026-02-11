using IdentityService.Domain.Entities;

namespace IdentityService.Application.Interfaces;

/// <summary>
/// Service for signing policy definitions and audit logs.
/// Uses issuer's private key to create cryptographic signatures.
/// </summary>
public interface IPolicySigningService
{
    /// <summary>
    /// Signs a policy definition, creating a signature over canonical policy data
    /// </summary>
    /// <param name="policy">Policy to sign</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Base64-encoded signature</returns>
    Task<string> SignPolicyAsync(
        PolicyDefinition policy,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Verifies a policy signature
    /// </summary>
    /// <param name="policy">Policy with signature to verify</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if signature is valid</returns>
    Task<bool> VerifyPolicySignatureAsync(
        PolicyDefinition policy,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Signs arbitrary data (used for audit logs)
    /// </summary>
    /// <param name="data">Data to sign (JSON string)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Base64-encoded signature</returns>
    Task<string> SignDataAsync(
        string data,
        CancellationToken cancellationToken = default);
}
