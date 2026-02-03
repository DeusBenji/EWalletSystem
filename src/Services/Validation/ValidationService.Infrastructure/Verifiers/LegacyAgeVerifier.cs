using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Application.BusinessLogic;
using BuildingBlocks.Contracts.Verification;
using Domain.Models;
using Microsoft.Extensions.Logging;
using ValidationService.Application.Interfaces;
using ValidationService.Application.Verification;

namespace ValidationService.Infrastructure.Verifiers
{
    public class LegacyAgeVerifier : IPresentationVerifier
    {
        private readonly IJwtValidator _jwtValidator;
        private readonly ILogger<LegacyAgeVerifier> _logger;

        public LegacyAgeVerifier(IJwtValidator jwtValidator, ILogger<LegacyAgeVerifier> logger)
        {
            _jwtValidator = jwtValidator;
            _logger = logger;
        }

        // We use a specific string for the legacy flow
        public string PresentationType => "age-boolean-legacy";

        public async Task<VerificationResult> VerifyAsync(VerificationRequest request, CancellationToken ct)
        {
            // For legacy, the "Presentation" is just the VcJwt string (or an object containing it).
            // We expect the router to have passed it as a JsonElement. 
            // We assume it's just the JWT string directly or a wrapper object.
            
            string vcJwt = null;
            
            if (request.Presentation.ValueKind == JsonValueKind.String)
            {
                vcJwt = request.Presentation.GetString();
            }
            else if (request.Presentation.ValueKind == JsonValueKind.Object)
            {
                // Try to find "vcJwt" property if it's a wrapper
                if (request.Presentation.TryGetProperty("vcJwt", out var jwtProp))
                {
                    vcJwt = jwtProp.GetString();
                }
            }

            if (string.IsNullOrWhiteSpace(vcJwt))
            {
                return new VerificationResult(false, new[] { ReasonCodes.MALFORMED_PRESENTATION }, null, null, DateTimeOffset.UtcNow);
            }

            // 1) Validate JWT signature
            var (isJwtValid, token, error) = await _jwtValidator.ValidateAsync(vcJwt, ct);

            if (!isJwtValid || token == null)
            {
                _logger.LogWarning("Legacy JWT validation failed: {Error}", error);
                return new VerificationResult(false, new[] { ReasonCodes.VC_SIGNATURE_INVALID }, null, null, DateTimeOffset.UtcNow);
            }

            // 2) Extract VC claim
            if (!token.Payload.TryGetValue("vc", out var vcClaim))
            {
                return new VerificationResult(false, new[] { ReasonCodes.MALFORMED_PRESENTATION }, null, token.Issuer, DateTimeOffset.UtcNow);
            }

            // 3) Parse content
            string vcJson = vcClaim.ToString();
            AgeOver18Credential? credential;

            try
            {
                credential = JsonSerializer.Deserialize<AgeOver18Credential>(vcJson);
            }
            catch (Exception)
            {
                return new VerificationResult(false, new[] { ReasonCodes.MALFORMED_PRESENTATION }, null, token.Issuer, DateTimeOffset.UtcNow);
            }

            // 4) Logic check
            if (credential == null || !credential.Type.Contains("AgeOver18Credential"))
            {
                 return new VerificationResult(false, new[] { ReasonCodes.POLICY_MISMATCH }, "AgeOver18Credential", token.Issuer, DateTimeOffset.UtcNow);
            }

            if (!credential.CredentialSubject.AgeOver18)
            {
                 return new VerificationResult(false, new[] { "AGE_REQUIREMENT_NOT_MET" }, "AgeOver18Credential", token.Issuer, DateTimeOffset.UtcNow);
            }

            // Success
            return new VerificationResult(
                Valid: true,
                ReasonCodes: Array.Empty<string>(),
                EvidenceType: "AgeOver18Credential",
                Issuer: token.Issuer,
                TimestampUtc: DateTimeOffset.UtcNow
            );
        }
    }
}
