using System;
using System.IdentityModel.Tokens.Jwt;
using System.Text.Json;
using System.Threading.Tasks;
using Application.DTOs;
using Application.Interfaces;
using ValidationService.Application.Interfaces;
using Microsoft.Extensions.Logging;
using Domain.Models;
using Domain.Repositories;

namespace Application.BusinessLogic
{
    public class CredentialValidationService : ICredentialValidationService
    {
        private readonly IFabricLookupClient _fabricLookupClient;
        private readonly ICacheService _cache;
        private readonly IVerificationLogRepository _logRepository;
        private readonly IKafkaEventProducer _kafkaProducer;
        private readonly ICredentialFingerprintService _fingerprint;
        private readonly ICredentialClaimParser _claimParser;
        private readonly ILogger<CredentialValidationService> _logger;
        private readonly IJwtValidator _jwtValidator;

        public CredentialValidationService(
            IFabricLookupClient fabricLookupClient,
            ICacheService cache,
            IVerificationLogRepository logRepository,
            IKafkaEventProducer kafkaProducer,
            ICredentialFingerprintService fingerprint,
            ICredentialClaimParser claimParser,
            ILogger<CredentialValidationService> logger,
            IJwtValidator jwtValidator)
        {
            _fabricLookupClient = fabricLookupClient;
            _cache = cache;
            _logRepository = logRepository;
            _kafkaProducer = kafkaProducer;
            _fingerprint = fingerprint;
            _claimParser = claimParser;
            _logger = logger;
            _jwtValidator = jwtValidator;
        }

        public async Task<VerifyCredentialResultDto> VerifyAsync(VerifyCredentialDto request)
        {
            if (string.IsNullOrWhiteSpace(request.VcJwt))
                throw new ArgumentException("VC JWT must be provided", nameof(request.VcJwt));

            var now = DateTime.UtcNow;
            // Validate JWT signature and claims
            var (isValid, token, error) = await _jwtValidator.ValidateAsync(request.VcJwt, default);

            if (!isValid || token == null)
            {
                _logger.LogWarning("JWT validation failed: {Error}", error);

                var failResult = new VerifyCredentialResultDto
                {
                    IsValid = false,
                    FailureReason = error ?? "JWT validation failed",
                    VerifiedAt = now,
                    IssuerDid = null
                };

                await LogAndMaybePublishAsync(request.VcJwt, failResult, null);
                return failResult;
            }

            // Extract and validate VC claim
            if (!token.Payload.TryGetValue("vc", out var vcClaim))
            {
                var noVcResult = new VerifyCredentialResultDto
                {
                    IsValid = false,
                    FailureReason = "Missing vc claim",
                    VerifiedAt = now,
                    IssuerDid = token.Issuer
                };

                await LogAndMaybePublishAsync(request.VcJwt, noVcResult, null);
                return noVcResult;
            }

            // Parse VC content
            var vcJson = vcClaim.ToString();
            AgeOver18Credential? credential;

            try
            {
                credential = JsonSerializer.Deserialize<AgeOver18Credential>(vcJson);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to parse VC claim");

                var parseFailResult = new VerifyCredentialResultDto
                {
                    IsValid = false,
                    FailureReason = "Invalid VC format",
                    VerifiedAt = now,
                    IssuerDid = token.Issuer
                };

                await LogAndMaybePublishAsync(request.VcJwt, parseFailResult, null);
                return parseFailResult;
            }

            // Validate VC content
            if (credential == null || !credential.Type.Contains("AgeOver18Credential"))
            {
                var wrongTypeResult = new VerifyCredentialResultDto
                {
                    IsValid = false,
                    FailureReason = "Wrong credential type",
                    VerifiedAt = now,
                    IssuerDid = token.Issuer
                };

                await LogAndMaybePublishAsync(request.VcJwt, wrongTypeResult, null);
                return wrongTypeResult;
            }

            if (!credential.CredentialSubject.AgeOver18)
            {
                var notAdultResult = new VerifyCredentialResultDto
                {
                    IsValid = false,
                    FailureReason = "Subject is not over 18",
                    VerifiedAt = now,
                    IssuerDid = token.Issuer
                };

                await LogAndMaybePublishAsync(request.VcJwt, notAdultResult, null);
                return notAdultResult;
            }

            // All checks passed
            var successResult = new VerifyCredentialResultDto
            {
                IsValid = true,
                FailureReason = null,
                VerifiedAt = now,
                IssuerDid = token.Issuer
            };

            var accountId = _claimParser.ExtractAccountId(token);
            await LogAndMaybePublishAsync(request.VcJwt, successResult, accountId);

            return successResult;
        }

        private async Task LogAndMaybePublishAsync(
            string vcJwt,
            VerifyCredentialResultDto result,
            Guid? accountId)
        {
            // 1) Hash VC for logformål
            var hash = _fingerprint.Hash(vcJwt);

            var log = new VerificationLog(
                id: Guid.NewGuid(),
                vcJwtHash: hash,
                isValid: result.IsValid,
                failureReason: result.FailureReason,
                verifiedAt: result.VerifiedAt);

            await _logRepository.InsertAsync(log);

            // 2) Publish event til Kafka (generisk JSON payload)
            if (accountId is null)
            {
                _logger.LogInformation(
                    "Skipping Kafka event for credential verification because AccountId could not be determined.");
                return;
            }

            var payload = new
            {
                AccountId = accountId.Value,
                result.IsValid,
                result.VerifiedAt,
                result.FailureReason
            };

            var value = JsonSerializer.Serialize(payload);
            var key = accountId.Value.ToString();

            const string topic = "credential-verified";

            await _kafkaProducer.PublishAsync(topic, key, value);
        }
    }
}
