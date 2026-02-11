namespace IdentityService.Domain.Entities;

/// <summary>
/// Defines a policy that can be verified via Zero-Knowledge Proofs.
/// Policies are versioned cryptographic contracts that specify what attributes can be proven.
/// </summary>
public class PolicyDefinition
{
    /// <summary>
    /// Unique identifier for this policy (e.g., "age_over_18", "licensed_doctor")
    /// </summary>
    public required string PolicyId { get; init; }

    /// <summary>
    /// Semantic version of this policy (e.g., "1.2.0")
    /// Major version changes indicate breaking changes (new circuit, incompatible signals)
    /// </summary>
    public required string Version { get; init; }

    /// <summary>
    /// Identifier of the zk-SNARK circuit used for this policy
    /// </summary>
    public required string CircuitId { get; init; }

    /// <summary>
    /// Identifier of the verification key for this policy
    /// </summary>
    public required string VerificationKeyId { get; init; }

    /// <summary>
    /// SHA-256 fingerprint of the verification key (for integrity verification)
    /// Format: "sha256:abc123..."
    /// </summary>
    public required string VerificationKeyFingerprint { get; init; }

    /// <summary>
    /// Semver range of compatible versions (e.g., "^1.0.0", "1.x")
    /// Used for version compatibility checks during proof verification
    /// </summary>
    public required string CompatibleVersions { get; init; }

    /// <summary>
    /// Default expiry duration for credentials issued under this policy
    /// Format: ISO 8601 duration (e.g., "PT24H" for 24 hours, "PT72H" for 72 hours)
    /// </summary>
    public required string DefaultExpiry { get; init; }

    /// <summary>
    /// JSON schema defining the required public signals for this policy
    /// Example: { "challengeHash": "field", "credentialHash": "field", "policyResult": "bool" }
    /// </summary>
    public required string RequiredPublicSignalsSchema { get; init; }

    /// <summary>
    /// Lifecycle state of this policy
    /// </summary>
    public PolicyStatus Status { get; set; } = PolicyStatus.Active;

    /// <summary>
    /// Timestamp when this policy was deprecated (if applicable)
    /// Policies in deprecated state can still be used but will be phased out
    /// </summary>
    public DateTime? DeprecatedAt { get; set; }

    /// <summary>
    /// Cryptographic signature of this policy definition by the issuer
    /// Format: Base64-encoded signature
    /// Allows websites to verify the authenticity of policy metadata
    /// </summary>
    public string? Signature { get; set; }

    /// <summary>
    /// Timestamp when this policy was created
    /// </summary>
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Timestamp when this policy was last updated
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Computes the policy hash used for challenge binding
    /// PolicyHash = SHA256(policyId || version || circuitId)
    /// </summary>
    public string ComputePolicyHash()
    {
        var combined = $"{PolicyId}:{Version}:{CircuitId}";
        var bytes = System.Text.Encoding.UTF8.GetBytes(combined);
        var hash = System.Security.Cryptography.SHA256.HashData(bytes);
        return $"sha256:{Convert.ToHexString(hash).ToLowerInvariant()}";
    }
}

/// <summary>
/// Lifecycle states for a policy definition
/// </summary>
public enum PolicyStatus
{
    /// <summary>
    /// Active and ready for production use
    /// </summary>
    Active = 0,

    /// <summary>
    /// Being phased out (grace period active)
    /// Can still be used but will be removed after deprecation window
    /// </summary>
    Deprecated = 1,

    /// <summary>
    /// Blocked due to security issue or emergency
    /// Cannot be used for new proofs
    /// </summary>
    Blocked = 2
}
