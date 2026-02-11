namespace IdentityService.Domain.Entities;

/// <summary>
/// Represents an attestation that a subject (user) meets the requirements of a specific policy.
/// Replaces the legacy AgeVerification model with a generic policy-based approach.
/// </summary>
public class PolicyAttestation
{
    /// <summary>
    /// Unique identifier for this attestation
    /// </summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>
    /// Policy identifier (e.g., "age_over_18", "licensed_doctor")
    /// </summary>
    public required string PolicyId { get; init; }

    /// <summary>
    /// Subject identifier (user who was verified)
    /// This is a pseudonymized identifier, NOT a CPR or national ID
    /// </summary>
    public required string SubjectId { get; init; }

    /// <summary>
    /// Provider identifier (who performed the verification, e.g., "mitid", "sbid", "nbid")
    /// </summary>
    public required string ProviderId { get; init; }

    /// <summary>
    /// Whether the subject meets the policy requirements
    /// </summary>
    public required bool Verified { get; init; }

    /// <summary>
    /// Timestamp when verification was performed
    /// </summary>
    public DateTime VerifiedAt { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Timestamp when this attestation expires
    /// After expiry, a new verification is required
    /// </summary>
    public DateTime ExpiresAt { get; init; }

    /// <summary>
    /// Assurance level of this attestation (e.g., "substantial", "high")
    /// Maps to eIDAS assurance levels
    /// </summary>
    public string AssuranceLevel { get; init; } = "substantial";

    /// <summary>
    /// Hash of the policy definition used for this attestation
    /// Used for tamper detection and audit trail
    /// </summary>
    public string? PolicyHash { get; init; }

    /// <summary>
    /// Optional metadata (JSON) for audit purposes
    /// MUST NOT contain PII - only verification metadata
    /// Example: { "verificationMethod": "eidas", "loa": "high" }
    /// </summary>
    public string? Metadata { get; set; }

    /// <summary>
    /// Timestamp when this record was created
    /// </summary>
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Checks if this attestation is still valid (not expired)
    /// </summary>
    public bool IsValid()
    {
        return Verified && DateTime.UtcNow < ExpiresAt;
    }

    /// <summary>
    /// Checks if this attestation is expired
    /// </summary>
    public bool IsExpired()
    {
        return DateTime.UtcNow >= ExpiresAt;
    }

    /// <summary>
    /// Time until expiry (or negative if already expired)
    /// </summary>
    public TimeSpan TimeUntilExpiry()
    {
        return ExpiresAt - DateTime.UtcNow;
    }
}
