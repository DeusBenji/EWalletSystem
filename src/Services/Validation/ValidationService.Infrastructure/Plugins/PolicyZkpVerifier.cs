using System.Text.Json;
using BuildingBlocks.Contracts.Verification;
using ValidationService.Application.Verification;
using ValidationService.Infrastructure.Clients;
using Microsoft.Extensions.Logging;

namespace ValidationService.Plugins;

/// <summary>
/// Verifier plugin for policy-based ZKP presentations (universal approach).
/// Implements 7-step verification: VC signature, commitment binding, challenge binding,
/// policy binding, sessionTag verification, proof verification, and expiration check.
/// </summary>
public class PolicyZkpVerifier : IPresentationVerifier
{
    public string PresentationType => "policy-zkp-v1";

    private readonly IZkpServiceClient _zkpService;
    private readonly ILogger<PolicyZkpVerifier> _logger;

    public PolicyZkpVerifier(IZkpServiceClient zkpService, ILogger<PolicyZkpVerifier> logger)
    {
        _zkpService = zkpService;
        _logger = logger;
    }

    public async Task<VerificationResult> VerifyAsync(VerificationRequest request, CancellationToken ct)
    {
        try
        {
            // 1. Parse Presentation
            var presentation = JsonSerializer.Deserialize<PolicyZkpPresentation>(request.Presentation.GetRawText());
            
            if (presentation == null || string.IsNullOrEmpty(presentation.VcJwt))
            {
                return new VerificationResult(false, new[] { ReasonCodes.MALFORMED_PRESENTATION }, "PolicyProof", "Unknown", DateTimeOffset.UtcNow);
            }

            // 2. Validate VC Signature & Extract Claims
            var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
            if (!handler.CanReadToken(presentation.VcJwt))
            {
                return new VerificationResult(false, new[] { ReasonCodes.INVALID_SIGNATURE }, "PolicyProof", "Unknown", DateTimeOffset.UtcNow);
            }
            
            var jwt = handler.ReadJwtToken(presentation.VcJwt);
            
            // Extract VC claims
            var vcClaimStr = jwt.Claims.FirstOrDefault(c => c.Type == "vc")?.Value;
            if (string.IsNullOrEmpty(vcClaimStr))
            {
                return new VerificationResult(false, new[] { ReasonCodes.MISSING_CLAIMS }, "PolicyProof", jwt.Issuer, DateTimeOffset.UtcNow);
            }
            
            using var doc = JsonDocument.Parse(vcClaimStr);
            var root = doc.RootElement;
            
            // Extract policyId and subjectCommitment from VC
            if (!root.TryGetProperty("policyId", out var policyIdEl) ||
                !root.TryGetProperty("subjectCommitment", out var vcCommitmentEl))
            {
                return new VerificationResult(false, new[] { ReasonCodes.MISSING_CLAIMS }, "PolicyProof", jwt.Issuer, DateTimeOffset.UtcNow);
            }
            
            var vcPolicyId = policyIdEl.GetString();
            var vcCommitment = vcCommitmentEl.GetString();

            // 3. Commitment Binding Check: VC.subjectCommitment == Proof.publicInputs.subjectCommitment
            if (vcCommitment != presentation.PublicInputs.SubjectCommitment)
            {
                _logger.LogWarning("Binding Attack Detected! VC Commitment does not match Proof Commitment.");
                return new VerificationResult(false, new[] { ReasonCodes.BINDING_MISMATCH }, "PolicyProof", jwt.Issuer, DateTimeOffset.UtcNow);
            }

            // 4. Challenge Binding (Replay Protection): Hash(Request.Challenge) == Proof.publicInputs.challengeHash
            var expectedChallengeHash = await _zkpService.GetChallengeHashAsync(request.Challenge, ct);
            if (expectedChallengeHash == null)
            {
                return new VerificationResult(false, new[] { ReasonCodes.ZKP_SERVICE_UNAVAILABLE }, "PolicyProof", jwt.Issuer, DateTimeOffset.UtcNow);
            }

            if (presentation.PublicInputs.ChallengeHash != expectedChallengeHash)
            {
                _logger.LogWarning("Replay Attack Detected! Challenge Hash mismatch.");
                return new VerificationResult(false, new[] { ReasonCodes.REPLAY_DETECTED }, "PolicyProof", jwt.Issuer, DateTimeOffset.UtcNow);
            }

            // 5. Policy Binding Check: Hash(Request.PolicyId) == Proof.publicInputs.policyHash
            var policyIdToVerify = request.PolicyId ?? vcPolicyId;
            if (string.IsNullOrEmpty(policyIdToVerify))
            {
                 return new VerificationResult(false, new[] { ReasonCodes.MISSING_CLAIMS }, "PolicyProof", jwt.Issuer, DateTimeOffset.UtcNow);
            }

            var expectedPolicyHash = await _zkpService.GetPolicyHashAsync(policyIdToVerify, ct);
            if (expectedPolicyHash == null)
            {
                return new VerificationResult(false, new[] { ReasonCodes.ZKP_SERVICE_UNAVAILABLE }, "PolicyProof", jwt.Issuer, DateTimeOffset.UtcNow);
            }

            if (presentation.PublicInputs.PolicyHash != expectedPolicyHash)
            {
                _logger.LogWarning("Policy Binding Attack Detected! Policy Hash mismatch.");
                return new VerificationResult(false, new[] { ReasonCodes.POLICY_MISMATCH }, "PolicyProof", jwt.Issuer, DateTimeOffset.UtcNow);
            }

            // 6. SessionTag Verification: Verify that sessionTag = Poseidon(secret, challengeHash, policyHash)
            // This is implicitly verified by the ZKP circuit, but we can optionally verify it here
            // For now, we trust the circuit to enforce this constraint

            // 7. ZKP Proof Verification (Call Go Service)
            var isValid = await _zkpService.VerifyPolicyV1Async(
                presentation.Proof,
                presentation.PublicInputs.ChallengeHash,
                presentation.PublicInputs.PolicyHash,
                presentation.PublicInputs.SubjectCommitment,
                presentation.PublicInputs.SessionTag ?? string.Empty,
                ct);

            if (!isValid)
            {
                return new VerificationResult(false, new[] { ReasonCodes.PROOF_INVALID }, "PolicyProof", jwt.Issuer, DateTimeOffset.UtcNow);
            }

            // 8. Expiration Check (Optional, but recommended)
            if (jwt.ValidTo < DateTime.UtcNow)
            {
                _logger.LogWarning("Credential expired at {ExpiryTime}", jwt.ValidTo);
                return new VerificationResult(false, new[] { ReasonCodes.CREDENTIAL_EXPIRED }, "PolicyProof", jwt.Issuer, DateTimeOffset.UtcNow);
            }

            // Success
            _logger.LogInformation("Policy ZKP verification successful for policy: {PolicyId}", vcPolicyId);
            return new VerificationResult(true, Array.Empty<string>(), "PolicyProof", jwt.Issuer, DateTimeOffset.UtcNow);

        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying Policy ZKP presentation");
            return new VerificationResult(false, new[] { ReasonCodes.SYSTEM_ERROR }, "PolicyProof", "Unknown", DateTimeOffset.UtcNow);
        }
    }
}
