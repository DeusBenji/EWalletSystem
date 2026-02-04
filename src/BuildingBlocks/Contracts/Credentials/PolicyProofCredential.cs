using System.Text.Json.Serialization;

namespace BuildingBlocks.Contracts.Credentials;

/// <summary>
/// Policy-based credential that attests to a subject's eligibility for a specific policy
/// without revealing PII. The credential is bound to a subject via a cryptographic commitment.
/// </summary>
/// <remarks>
/// This credential type supports universal policy verification (age_over_18, lawyer, student, etc.)
/// without requiring policy-specific credential formats.
/// </remarks>
public record PolicyProofCredential
{
    /// <summary>
    /// Unique identifier for the policy being attested (e.g., "age_over_18", "lawyer", "eu_resident")
    /// </summary>
    [JsonPropertyName("policyId")]
    public required string PolicyId { get; init; }

    /// <summary>
    /// Cryptographic commitment to the subject's wallet secret: Poseidon(walletSecret)
    /// This binds the credential to the subject without revealing their identity.
    /// </summary>
    [JsonPropertyName("subjectCommitment")]
    public required string SubjectCommitment { get; init; }

    /// <summary>
    /// DID or identifier of the issuing authority
    /// </summary>
    [JsonPropertyName("issuer")]
    public required string Issuer { get; init; }

    /// <summary>
    /// Timestamp when the credential was issued (ISO 8601)
    /// </summary>
    [JsonPropertyName("issuedAt")]
    public required DateTime IssuedAt { get; init; }

    /// <summary>
    /// Timestamp when the credential expires (ISO 8601)
    /// </summary>
    [JsonPropertyName("expiresAt")]
    public required DateTime ExpiresAt { get; init; }

    /// <summary>
    /// Cryptographic signature over the credential (JWT format)
    /// </summary>
    [JsonPropertyName("signature")]
    public required string Signature { get; init; }

    /// <summary>
    /// Contract version for forward compatibility
    /// </summary>
    [JsonPropertyName("contractVersion")]
    public string ContractVersion { get; init; } = "1.0";

    /// <summary>
    /// Optional metadata (e.g., issuer-specific fields)
    /// </summary>
    [JsonPropertyName("metadata")]
    public Dictionary<string, string>? Metadata { get; init; }
}
