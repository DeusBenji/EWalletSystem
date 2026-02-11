using IdentityService.Domain.Entities;

namespace IdentityService.Application.Interfaces;

/// <summary>
/// Service for managing issuer signing keys.
/// Handles key rotation lifecycle and JWKS endpoint.
/// </summary>
public interface IKeyManagementService
{
    /// <summary>
    /// Retrieves the current signing key (for signing new credentials)
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Current signing key, or null if none exists</returns>
    Task<IssuerSigningKey?> GetCurrentSigningKeyAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a specific key by its ID
    /// </summary>
    /// <param name="keyId">Key identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Key if found, null otherwise</returns>
    Task<IssuerSigningKey?> GetKeyByIdAsync(
        string keyId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all keys that can verify credentials (current + deprecated in grace period)
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of keys that can verify</returns>
    Task<IReadOnlyList<IssuerSigningKey>> GetVerificationKeysAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new signing key and marks it as current
    /// If a current key exists, it is deprecated
    /// </summary>
    /// <param name="algorithm">Algorithm to use (e.g., "ES256")</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created key</returns>
    Task<IssuerSigningKey> RotateKeyAsync(
        string algorithm = "ES256",
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks a key as deprecated (starts grace period)
    /// </summary>
    /// <param name="keyId">Key to deprecate</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task DeprecateKeyAsync(
        string keyId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks a key as retired (immediate invalidation, no grace period)
    /// Use when key is compromised
    /// </summary>
    /// <param name="keyId">Key to retire</param>
    /// <param name="reason">Reason for retirement (for audit log)</param>
    /// <param name="actor">Who is retiring the key</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task RetireKeyAsync(
        string keyId,
        string reason,
        string actor,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns JWKS (JSON Web Key Set) for public key distribution
    /// Used by validators to fetch verification keys
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>JWKS object containing all verification keys</returns>
    Task<object> GetJwksAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Auto-retires deprecated keys that have exceeded their grace period
    /// Should be called periodically (e.g., daily cron job)
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of keys retired</returns>
    Task<int> AutoRetireExpiredKeysAsync(
        CancellationToken cancellationToken = default);
}
