namespace IdentityService.Domain.Entities;

/// <summary>
/// Represents an issuer signing key used to sign policy credentials.
/// Keys have a lifecycle: current -> deprecated -> retired.
/// </summary>
public class IssuerSigningKey
{
    /// <summary>
    /// Unique identifier for this key (e.g., "key-2026-02", "key-prod-v1")
    /// </summary>
    public required string KeyId { get; init; }

    /// <summary>
    /// Algorithm used by this key (e.g., "ES256", "RS256")
    /// </summary>
    public required string Algorithm { get; init; }

    /// <summary>
    /// Public key in JWK format (JSON Web Key)
    /// Used by validators to verify credentials
    /// </summary>
    public required string PublicKeyJwk { get; init; }

    /// <summary>
    /// Private key (encrypted at rest)
    /// NEVER exposed via API - only used internally for signing
    /// </summary>
    public required string EncryptedPrivateKey { get; init; }

    /// <summary>
    /// Lifecycle status of this key
    /// </summary>
    public KeyStatus Status { get; set; } = KeyStatus.Current;

    /// <summary>
    /// When this key was created
    /// </summary>
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// When this key was deprecated (if applicable)
    /// Deprecated keys can still verify old credentials during grace period
    /// </summary>
    public DateTime? DeprecatedAt { get; set; }

    /// <summary>
    /// When this key was retired (if applicable)
    /// Retired keys immediately invalidate all credentials signed with them
    /// </summary>
    public DateTime? RetiredAt { get; set; }

    /// <summary>
    /// Grace period duration for deprecated keys (default: 7 days)
    /// After grace period, deprecated key is automatically retired
    /// </summary>
    public TimeSpan GracePeriod { get; init; } = TimeSpan.FromDays(7);

    /// <summary>
    /// Checks if this key can be used for signing NEW credentials
    /// Only 'Current' keys can sign new credentials
    /// </summary>
    public bool CanSign()
    {
        return Status == KeyStatus.Current;
    }

    /// <summary>
    /// Checks if this key can be used for VERIFYING credentials
    /// Current and deprecated (within grace period) keys can verify
    /// </summary>
    public bool CanVerify()
    {
        if (Status == KeyStatus.Retired)
        {
            return false;
        }

        if (Status == KeyStatus.Deprecated)
        {
            // Check if still within grace period
            return DeprecatedAt.HasValue && 
                   DateTime.UtcNow < DeprecatedAt.Value + GracePeriod;
        }

        return Status == KeyStatus.Current;
    }

    /// <summary>
    /// Checks if this key should be auto-retired (grace period expired)
    /// </summary>
    public bool ShouldBeRetired()
    {
        return Status == KeyStatus.Deprecated &&
               DeprecatedAt.HasValue &&
               DateTime.UtcNow >= DeprecatedAt.Value + GracePeriod;
    }
}

/// <summary>
/// Lifecycle states for issuer signing keys
/// </summary>
public enum KeyStatus
{
    /// <summary>
    /// Active key, used for signing new credentials
    /// Only one key should be 'Current' at a time
    /// </summary>
    Current = 0,

    /// <summary>
    /// Deprecated key, no longer signing but still verifying (grace period)
    /// Used during key rotation to allow existing credentials to remain valid
    /// </summary>
    Deprecated = 1,

    /// <summary>
    /// Retired key, cannot sign or verify
    /// Used when key is compromised - immediate invalidation
    /// </summary>
    Retired = 2
}
