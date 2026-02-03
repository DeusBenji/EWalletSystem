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
using ValidationService.Application.Verification;

namespace Application.BusinessLogic
{
    public class CredentialValidationService : ICredentialValidationService
    {
        private readonly IVerificationEngine _verificationEngine;
        private readonly IVerificationLogRepository _logRepository;
        private readonly IKafkaProducer _kafkaProducer;
        private readonly ICredentialFingerprintService _fingerprint;
        private readonly ICredentialClaimParser _claimParser;
        private readonly ILogger<CredentialValidationService> _logger;

        public CredentialValidationService(
            IVerificationEngine verificationEngine,
            IVerificationLogRepository logRepository,
            IKafkaProducer kafkaProducer,
            ICredentialFingerprintService fingerprint,
            ICredentialClaimParser claimParser,
            ILogger<CredentialValidationService> logger)
        {
            _verificationEngine = verificationEngine;
            _logRepository = logRepository;
            _kafkaProducer = kafkaProducer;
            _fingerprint = fingerprint;
            _claimParser = claimParser;
            _logger = logger;
        }

        public async Task<VerifyCredentialResultDto> VerifyAsync(VerifyCredentialDto request)
        {
            if (string.IsNullOrWhiteSpace(request?.VcJwt))
                throw new ArgumentException("VC JWT must be provided", nameof(request.VcJwt));

            var now = DateTime.UtcNow;

            // 1) Wrap as VerificationRequest
            // We interpret the existing "VerifyCredentialDto" as a legacy "age-boolean" request
            // In a real scenario, the controller might accept a richer object, but for now we adapt.
            var engineRequest = new BuildingBlocks.Contracts.Verification.VerificationRequest(
                ContractVersion: "v1",
                PolicyId: "age_over_18",
                PresentationType: "age-boolean-legacy",
                Presentation: System.Text.Json.JsonSerializer.SerializeToElement(request.VcJwt), // Just verify the JWT string
                Challenge: Guid.NewGuid().ToString(), // Legacy flow doesn't explicitly use challenge from client, so we generate one or ignore
                Context: "legacy-api"
            );

            // 2) Delegate to Engine
            var engineResult = await _verificationEngine.VerifyAsync(engineRequest, default);

            // 3) Map back to DTO
            var resultDto = new VerifyCredentialResultDto
            {
                IsValid = engineResult.Valid,
                FailureReason = engineResult.Valid ? null : string.Join(", ", engineResult.ReasonCodes),
                VerifiedAt = engineResult.TimestampUtc.DateTime,
                IssuerDid = engineResult.Issuer
            };

            // 4) Logging & Publishing (Keep existing behavior)
            // Note: We need AccountId for publishing. 
            // The Engine Result doesn't return AccountId explicitly (it's strict).
            // But we can try to extract it from the JWT if we have it, OR the Engine Result could include metadata.
            // For now, we'll try to re-parse the JWT here JUST for the account ID signal if valid.
            // Ideally, the Engine Result should pass back "extracted subjects".
            
            Guid? accountId = null;
            if (engineResult.Valid)
            {
                 // Try to partial parse just for AccountId (best effort for Kafka event)
                 try 
                 {
                     // Use the existing claim parser if possible, but we need a JwtSecurityToken.
                     // Since we don't want to re-validate, we might parse it directly.
                     // Simpler: assume legacy flow uses the JWT we passed.
                     var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
                     if (handler.CanReadToken(request.VcJwt))
                     {
                         var token = handler.ReadJwtToken(request.VcJwt);
                         accountId = _claimParser.ExtractAccountId(token);
                     }
                 }
                 catch 
                 { 
                     _logger.LogWarning("Could not extract AccountId for event publishing event though verification passed.");
                 }
            }

            await LogAndMaybePublishAsync(request.VcJwt, resultDto, accountId);

            return resultDto;
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
