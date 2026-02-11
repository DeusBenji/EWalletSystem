namespace ValidationService.Security;

/// <summary>
/// Embedded public key for verifying circuit manifest signatures.
/// This key is used to verify that circuit artifacts were signed offline
/// by an authorized signer with access to the air-gapped signing key.
/// 
/// CRITICAL SECURITY NOTE:
/// - This public key is embedded at compile-time
/// - CI can VERIFY signatures, but CANNOT SIGN
/// - Private key is kept offline on air-gapped machine
/// - Key rotation requires code deploy
/// </summary>
public static class CircuitSigningPublicKey
{
    /// <summary>
    /// Public key fingerprint (SHA256 of public key DER)
    /// Used for key identification and rotation tracking
    /// </summary>
    public const string Fingerprint = "abc123def456789...";  // TODO: Replace with actual fingerprint
    
    /// <summary>
    /// Public key in PEM format (ECDSA P-256)
    /// This key is used to verify circuit manifest signatures
    /// </summary>
    public const string PublicKeyPem = @"-----BEGIN PUBLIC KEY-----
MFkwEwYHKoZIzj0CAQYIKoZIzj0DAQcDQgAEXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
-----END PUBLIC KEY-----";  // TODO: Replace with actual public key
    
    /// <summary>
    /// Key creation date (for audit trail)
    /// </summary>
    public static readonly DateTime CreatedAt = new DateTime(2026, 2, 11, 14, 0, 0, DateTimeKind.Utc);
    
    /// <summary>
    /// Key status for rotation support
    /// </summary>
    public enum KeyStatus
    {
        Active,
        Deprecated,  // Grace period during rotation
        Revoked      // Compromised
    }
    
    public const KeyStatus Status = KeyStatus.Active;
    
    /// <summary>
    /// Deprecated keys (for grace period during rotation)
    /// Format: (PublicKeyPem, Fingerprint, DeprecatedAt, GraceEndDate)
    /// </summary>
    public static readonly (string Pem, string Fingerprint, DateTime DeprecatedAt, DateTime GraceEndDate)[] DeprecatedKeys = 
    {
        // Example: Old key with 30-day grace period
        // (@"-----BEGIN PUBLIC KEY-----...", "old-fingerprint", new DateTime(2026, 1, 1), new DateTime(2026, 1, 31))
    };
    
    /// <summary>
    /// Checks if a given public key fingerprint is valid (current or in grace period)
    /// </summary>
    public static bool IsValidKeyFingerprint(string fingerprint)
    {
        // Check current key
        if (fingerprint == Fingerprint && Status == KeyStatus.Active)
        {
            return true;
        }
        
        // Check deprecated keys (within grace period)
        var now = DateTime.UtcNow;
        foreach (var (_, fp, _, graceEnd) in DeprecatedKeys)
        {
            if (fingerprint == fp && now <= graceEnd)
            {
                return true;
            }
        }
        
        return false;
    }
    
    /// <summary>
    /// Gets the public key PEM by fingerprint
    /// </summary>
    public static string? GetPublicKeyPem(string fingerprint)
    {
        if (fingerprint == Fingerprint)
        {
            return PublicKeyPem;
        }
        
        var now = DateTime.UtcNow;
        foreach (var (pem, fp, _, graceEnd) in DeprecatedKeys)
        {
            if (fingerprint == fp && now <= graceEnd)
            {
                return pem;
            }
        }
        
        return null;
    }
}
