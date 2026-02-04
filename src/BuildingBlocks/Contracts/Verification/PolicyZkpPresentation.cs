using System.Text.Json.Serialization;

namespace BuildingBlocks.Contracts.Verification;

/// <summary>
/// Presentation format for policy-based zero-knowledge proofs.
/// Contains the verifiable credential, ZKP proof, and public inputs.
/// </summary>
public record PolicyZkpPresentation
{
    /// <summary>
    /// Signed PolicyProofCredential in JWT format
    /// </summary>
    [JsonPropertyName("vcJwt")]
    public required string VcJwt { get; init; }

    /// <summary>
    /// Groth16 proof in JSON format (pi_a, pi_b, pi_c, protocol)
    /// </summary>
    [JsonPropertyName("proof")]
    public required string Proof { get; init; }

    /// <summary>
    /// Public inputs to the ZKP circuit
    /// </summary>
    [JsonPropertyName("publicInputs")]
    public required PolicyZkpPublicInputs PublicInputs { get; init; }

    /// <summary>
    /// Presentation type identifier
    /// </summary>
    [JsonPropertyName("presentationType")]
    public string PresentationType { get; init; } = "policy-zkp-v1";

    /// <summary>
    /// Contract version for forward compatibility
    /// </summary>
    [JsonPropertyName("contractVersion")]
    public string ContractVersion { get; init; } = "1.0";
}

/// <summary>
/// Public inputs to the policy ZKP circuit
/// </summary>
public record PolicyZkpPublicInputs
{
    /// <summary>
    /// Commitment to the subject's wallet secret: Poseidon(walletSecret)
    /// Must match the commitment in the VC for binding.
    /// </summary>
    [JsonPropertyName("subjectCommitment")]
    public required string SubjectCommitment { get; init; }

    /// <summary>
    /// Hash of the challenge for replay protection: Poseidon(challenge)
    /// </summary>
    [JsonPropertyName("challengeHash")]
    public required string ChallengeHash { get; init; }

    /// <summary>
    /// Hash of the policy ID to prevent cross-policy proof reuse: Poseidon(policyId)
    /// Optional but recommended for enhanced security.
    /// </summary>
    [JsonPropertyName("policyHash")]
    public string? PolicyHash { get; init; }
}
