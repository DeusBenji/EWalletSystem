using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BuildingBlocks.Contracts.Verification;
using Microsoft.Extensions.Logging;

namespace ValidationService.Application.Verification
{
    public class VerificationEngine : IVerificationEngine
    {
        private readonly Dictionary<string, IPresentationVerifier> _verifiers;
        private readonly ILogger<VerificationEngine> _logger;

        public VerificationEngine(
            IEnumerable<IPresentationVerifier> verifiers,
            ILogger<VerificationEngine> logger)
        {
            _verifiers = verifiers.ToDictionary(v => v.PresentationType, v => v);
            _logger = logger;
        }

        public async Task<VerificationResult> VerifyAsync(VerificationRequest request, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(request?.PresentationType))
            {
                return InvalidResult("Missing presentation type", ReasonCodes.MALFORMED_PRESENTATION);
            }

            if (!_verifiers.TryGetValue(request.PresentationType, out var verifier))
            {
                _logger.LogWarning("Unsupported presentation type: {Type}", request.PresentationType);
                return InvalidResult($"Unsupported presentation type: {request.PresentationType}", ReasonCodes.UNSUPPORTED_PRESENTATION);
            }

            try
            {
                return await verifier.VerifyAsync(request, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during verification of type {Type}", request.PresentationType);
                return InvalidResult("Internal verification error", "INTERNAL_ERROR");
            }
        }

        private static VerificationResult InvalidResult(string reason, string code)
        {
            return new VerificationResult(
                Valid: false,
                ReasonCodes: new[] { code },
                EvidenceType: null,
                Issuer: null,
                TimestampUtc: DateTimeOffset.UtcNow
            );
        }
    }
}
