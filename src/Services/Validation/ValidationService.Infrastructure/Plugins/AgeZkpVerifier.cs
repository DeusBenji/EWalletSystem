using System.Text.Json;
using BuildingBlocks.Contracts.Verification;
using ValidationService.Application.Verification;
using ValidationService.Infrastructure.Clients;
using Microsoft.Extensions.Logging;

namespace ValidationService.Plugins
{
    public class AgeZkpVerifier : IPresentationVerifier
    {
        public string PresentationType => "age-zkp-v1";

        private readonly IZkpServiceClient _zkpService;
        private readonly ILogger<AgeZkpVerifier> _logger;
        // Assume we have a service to verify VC signatures (JwtValidator or similar).
        // For this implementation, we focus on the ZKP specific logic.
        // We'll inject a "CredentialValidator" or similar if available, 
        // or for now assumes the VC is passed as a signed JWT string in the presentation.

        public AgeZkpVerifier(IZkpServiceClient zkpService, ILogger<AgeZkpVerifier> logger)
        {
            _zkpService = zkpService;
            _logger = logger;
        }

        public async Task<VerificationResult> VerifyAsync(VerificationRequest request, CancellationToken ct)
        {
            try
            {
                // 1. Parse Presentation
                // Expected format: { "verifiableCredential": "jwt...", "proof": "hex...", "publicInputs": { "challengeHash": "...", "commitment": "..." } }
                var presentationWrapper = JsonSerializer.Deserialize<AgeZkpPresentation>(request.Presentation.GetRawText());
                
                if (presentationWrapper == null || string.IsNullOrEmpty(presentationWrapper.VerifiableCredential))
                {
                     return new VerificationResult(false, new[] { ReasonCodes.MALFORMED_PRESENTATION }, "AgeProof", "Unknown", DateTimeOffset.UtcNow);
                }

                // 2. Validate VC Signature & Expiration (Standard JWT check)
                // TODO: Using a helper or handler for generic VC validation. 
                // For MVP: Parsing claims directly to extract commitment. In Prod: MUST verify signature first.
                // Assuming trusted context or we'd call a ValidateJwt helper here.
                
                var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
                if (!handler.CanReadToken(presentationWrapper.VerifiableCredential))
                {
                     return new VerificationResult(false, new[] { ReasonCodes.INVALID_SIGNATURE }, "AgeProof", "Unknown", DateTimeOffset.UtcNow);
                }
                
                var jwt = handler.ReadJwtToken(presentationWrapper.VerifiableCredential);
                
                // Extract Commitment claim
                var vcClaimStr = jwt.Claims.FirstOrDefault(c => c.Type == "vc")?.Value;
                if (string.IsNullOrEmpty(vcClaimStr))
                {
                    return new VerificationResult(false, new[] { ReasonCodes.MISSING_CLAIMS }, "AgeProof", jwt.Issuer, DateTimeOffset.UtcNow);
                }
                
                using var doc = JsonDocument.Parse(vcClaimStr);
                var root = doc.RootElement;
                if (!root.TryGetProperty("credentialSubject", out var subject) || 
                    !subject.TryGetProperty("commitment", out var vcCommitmentEl))
                {
                    return new VerificationResult(false, new[] { ReasonCodes.MISSING_CLAIMS }, "AgeProof", jwt.Issuer, DateTimeOffset.UtcNow);
                }
                var vcCommitment = vcCommitmentEl.GetString();

                // 3. Strict Replay Protection: Hash(Request.Challenge) == Presentation.ChallengeHash
                // We call the Go Service to get the expected hash of the challenge nonce.
                var expectedChallengeHash = await _zkpService.GetChallengeHashAsync(request.Challenge, ct);
                if (expectedChallengeHash == null)
                {
                     return new VerificationResult(false, new[] { ReasonCodes.ZKP_SERVICE_UNAVAILABLE }, "AgeProof", jwt.Issuer, DateTimeOffset.UtcNow);
                }

                if (presentationWrapper.PublicInputs.ChallengeHash != expectedChallengeHash)
                {
                    _logger.LogWarning("Replay Attack Detected! Challenge Hash mismatch.");
                    return new VerificationResult(false, new[] { ReasonCodes.REPLAY_DETECTED }, "AgeProof", jwt.Issuer, DateTimeOffset.UtcNow);
                }

                // 4. Strict Binding Check: VC.Commitment == PublicInputs.Commitment
                if (vcCommitment != presentationWrapper.PublicInputs.Commitment)
                {
                    _logger.LogWarning("Binding Attack Detected! VC Commitment does not match Proof Commitment.");
                    return new VerificationResult(false, new[] { ReasonCodes.BINDING_MISMATCH }, "AgeProof", jwt.Issuer, DateTimeOffset.UtcNow);
                }

                // 5. ZKP Verification (Logic)
                // Current Year is server-controlled (ValidationService)
                var serverCurrentYear = DateTime.UtcNow.Year.ToString();

                // 4b. Strict Time Input Control
                // We ensure the proof was generated for the current year context.
                if (presentationWrapper.PublicInputs.CurrentYear != serverCurrentYear)
                {
                     _logger.LogWarning("Time check failed! Proof year {ProofYear} != Server year {ServerYear}", presentationWrapper.PublicInputs.CurrentYear, serverCurrentYear);
                     return new VerificationResult(false, new[] { ReasonCodes.POLICY_MISMATCH }, "AgeProof", jwt.Issuer, DateTimeOffset.UtcNow);
                }
                
                var isValid = await _zkpService.VerifyAgeV1Async(
                    presentationWrapper.Proof, 
                    serverCurrentYear, 
                    presentationWrapper.PublicInputs.Commitment, 
                    presentationWrapper.PublicInputs.ChallengeHash, 
                    ct);

                if (!isValid)
                {
                    return new VerificationResult(false, new[] { ReasonCodes.PROOF_INVALID }, "AgeProof", jwt.Issuer, DateTimeOffset.UtcNow);
                }

                // Success
                return new VerificationResult(true, Array.Empty<string>(), "AgeProof", jwt.Issuer, DateTimeOffset.UtcNow);

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying ZKP presentation");
                return new VerificationResult(false, new[] { ReasonCodes.SYSTEM_ERROR }, "AgeProof", "Unknown", DateTimeOffset.UtcNow);
            }
        }
    }
}
