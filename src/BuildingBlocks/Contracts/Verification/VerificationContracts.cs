using System;
using System.Text.Json;

namespace BuildingBlocks.Contracts.Verification
{
    /// <summary>
    /// Represents a request to verify a presentation against a specific policy.
    /// </summary>
    /// <param name="ContractVersion">Version of this contract (e.g. "v1").</param>
    /// <param name="PolicyId">The identifier of the policy being verified (e.g. "age_over_18").</param>
    /// <param name="PresentationType">The type of presentation provided (e.g. "age-zkp-v1", "age-boolean-v1").</param>
    /// <param name="Presentation">The raw presentation payload (polymorphic).</param>
    /// <param name="Challenge">The nonce/challenge provided by the verifier to prevent replay.</param>
    /// <param name="Context">Optional context (e.g. jurisdiction or client info).</param>
    public record VerificationRequest(
        string ContractVersion,
        string PolicyId,
        string PresentationType,
        JsonElement Presentation,
        string Challenge,
        string Context
    );

    /// <summary>
    /// The result of a verification attempt.
    /// </summary>
    /// <param name="Valid">Whether the verification succeeded.</param>
    /// <param name="ReasonCodes">List of error/reason codes if invalid (or warnings).</param>
    /// <param name="EvidenceType">The type of evidence verified.</param>
    /// <param name="Issuer">The DID of the issuer trusted for this verification (if applicable).</param>
    /// <param name="TimestampUtc">When the verification was performed.</param>
    public record VerificationResult(
        bool Valid,
        string[] ReasonCodes,
        string EvidenceType,
        string Issuer,
        DateTimeOffset TimestampUtc
    );
}
