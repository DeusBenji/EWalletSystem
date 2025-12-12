using System;
using System.Text.Json;
using System.Threading.Tasks;
using Application.DTOs;
using Application.Interfaces;
using BuildingBlocks.Contracts.Events;
using BuildingBlocks.Contracts.Messaging;
using Domain.Models;
using Domain.Repositories;
using Microsoft.Extensions.Logging;
using ValidationService.Application.Interfaces;

namespace Application.BusinessLogic
{
    public class CredentialValidationService : ICredentialValidationService
    {
        private readonly IFabricLookupClient _fabricLookupClient;
        private readonly ICacheService _cache;
        private readonly IVerificationLogRepository _logRepository;

        private readonly IKafkaProducer _kafkaProducer;

        private readonly ICredentialFingerprintService _fingerprint;
        private readonly ICredentialClaimParser _claimParser;
        private readonly ILogger<CredentialValidationService> _logger;
        private readonly IJwtValidator _jwtValidator;

        public CredentialValidationService(
            IFabricLookupClient fabricLookupClient,
            ICacheService cache,
            IVerificationLogRepository logRepository,
            IKafkaProducer kafkaProducer,
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

            // 1) Validate JWT signature and claims
            var (isJwtValid, token, error) = await _jwtValidator.ValidateAsync(request.VcJwt, default);

            if (!isJwtValid || token == null)
            {
                _logger.LogWarning("JWT validation failed: {Error}", error);

                var failResult = new VerifyCredentialResultDto
                {
                    IsValid = false,
                    FailureReason = error ?? "JWT validation failed",
                    VerifiedAt = now,
                    IssuerDid = null
                };

                await LogAndMaybePublishAsync(request.VcJwt, failResult, accountId: null);
                return failResult;
            }

            // 2) Extract and validate VC claim
            if (!token.Payload.TryGetValue("vc", out var vcClaim))
            {
                var noVcResult = new VerifyCredentialResultDto
                {
                    IsValid = false,
                    FailureReason = "Missing vc claim",
                    VerifiedAt = now,
                    IssuerDid = token.Issuer
                };

                await LogAndMaybePublishAsync(request.VcJwt, noVcResult, accountId: null);
                return noVcResult;
            }

            // 3) Parse VC content
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

                await LogAndMaybePublishAsync(request.VcJwt, parseFailResult, accountId: null);
                return parseFailResult;
            }

            // 4) Validate VC content
            if (credential == null || !credential.Type.Contains("AgeOver18Credential"))
            {
                var wrongTypeResult = new VerifyCredentialResultDto
                {
                    IsValid = false,
                    FailureReason = "Wrong credential type",
                    VerifiedAt = now,
                    IssuerDid = token.Issuer
                };

                await LogAndMaybePublishAsync(request.VcJwt, wrongTypeResult, accountId: null);
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

                await LogAndMaybePublishAsync(request.VcJwt, notAdultResult, accountId: null);
                return notAdultResult;
            }

            // 5) All checks passed
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
            // 1) Log (altid)
            var hash = _fingerprint.Hash(vcJwt);

            var log = new VerificationLog(
                id: Guid.NewGuid(),
                vcJwtHash: hash,
                isValid: result.IsValid,
                failureReason: result.FailureReason,
                verifiedAt: result.VerifiedAt);

            await _logRepository.InsertAsync(log);

            // 2) Publish kun når GODKENDT
            if (!result.IsValid)
            {
                _logger.LogInformation("Credential verify rejected; skipping Kafka publish (by design).");
                return;
            }

            if (accountId is null)
            {
                _logger.LogInformation("Skipping Kafka publish because AccountId could not be determined.");
                return;
            }

            var evt = new CredentialVerified(
                AccountId: accountId.Value,
                IsValid: true,
                IssuerDid: result.IssuerDid,
                FailureReason: null,
                VerifiedAt: result.VerifiedAt
            );

            await _kafkaProducer.PublishAsync(
                Topics.CredentialVerified,
                accountId.Value.ToString(),
                evt
            );
        }
    }
}
