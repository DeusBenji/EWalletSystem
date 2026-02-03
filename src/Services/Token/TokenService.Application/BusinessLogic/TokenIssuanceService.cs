using Application.DTOs;
using Application.Interfaces;
using BuildingBlocks.Contracts.Events;
using BuildingBlocks.Contracts.Messaging;
using Domain.Models;
using Domain.Repositories;
using TokenService.Application.Interfaces;
using TokenService.Domain.Models;

namespace Application.BusinessLogic
{
    public class TokenIssuanceService : ITokenIssuanceService
    {
        private readonly IAccountAgeStatusRepository _ageStatusRepo;
        private readonly IAccountAgeStatusCache _ageStatusCache;
        private readonly IAttestationRepository _attestationRepo;
        private readonly IFabricAnchorClient _fabric;
        private readonly ITokenHashCalculator _hashCalculator;
        private readonly IKafkaProducer _kafkaProducer;
        private readonly IVcSigningService _vcSigning;

        public TokenIssuanceService(
            IAccountAgeStatusRepository ageStatusRepo,
            IAccountAgeStatusCache ageStatusCache,
            IAttestationRepository attestationRepo,
            IFabricAnchorClient fabric,
            ITokenHashCalculator hashCalculator,
            IKafkaProducer kafkaProducer,
            IVcSigningService vcSigning)
        {
            _ageStatusRepo = ageStatusRepo;
            _ageStatusCache = ageStatusCache;
            _attestationRepo = attestationRepo;
            _fabric = fabric;
            _hashCalculator = hashCalculator;
            _kafkaProducer = kafkaProducer;
            _vcSigning = vcSigning;
        }

        public async Task<IssuedTokenDto> IssueTokenAsync(IssueTokenDto dto, CancellationToken ct = default)
        {
            // 1) Hent alder-status (cache → repo)
            var status = await _ageStatusCache.GetAsync(dto.AccountId, ct);

            if (status is null)
            {
                status = await _ageStatusRepo.GetAsync(dto.AccountId, ct);
                if (status is not null)
                {
                    await _ageStatusCache.SetAsync(status, ct);
                }
            }

            if (status is null || !status.IsAdult)
                throw new InvalidOperationException("Account is not verified as 18+.");

            var issuedAt = DateTime.UtcNow;
            var expiresAt = issuedAt.AddHours(1);
            var subjectId = $"did:example:account:{dto.AccountId}";

            string vcJwt;

            // 2) Build Verifiable Credential
            if (!string.IsNullOrEmpty(dto.Commitment))
            {
                // Strict ZKP Flow: Issue AgeProofCredential (Commitment only, No PII)
                var proofCredential = new AgeProofCredential
                {
                    Issuer = _vcSigning.GetIssuerDid(),
                    IssuanceDate = issuedAt.ToString("O"),
                    ExpirationDate = expiresAt.ToString("O"),
                    CredentialSubject = new AgeProofCredentialSubject
                    {
                        Id = subjectId,
                        Commitment = dto.Commitment
                    }
                };
                vcJwt = _vcSigning.CreateSignedVcJwt(proofCredential, subjectId, expiresAt);
            }
            else
            {
                // Legacy Flow: Issue AgeOver18Credential (Boolean flag)
                var credential = new AgeOver18Credential
                {
                    Issuer = _vcSigning.GetIssuerDid(),
                    IssuanceDate = issuedAt,
                    ExpirationDate = expiresAt,
                    CredentialSubject = new CredentialSubject
                    {
                        Id = subjectId,
                        AgeOver18 = true,
                        VerifiedAt = status.VerifiedAt
                    }
                };
                vcJwt = _vcSigning.CreateSignedVcJwt(credential, subjectId, expiresAt);
            }

            // 4) Hash for anchoring
            var hash = _hashCalculator.ComputeHash(vcJwt);

            var attestation = new AgeAttestation(
                accountId: dto.AccountId,
                subjectId: subjectId,
                isAdult: true,
                issuedAt: issuedAt,
                expiresAt: expiresAt,
                token: vcJwt,
                hash: hash,
                commitment: dto.Commitment // Persist commitment for audit
            );

            // 5) Anchor + persist
            await _fabric.AnchorHashAsync(hash, ct);
            await _attestationRepo.SaveAsync(attestation, ct);

            // 6) Publish TokenIssued-event via BuildingBlocks IKafkaProducer
            var evt = new TokenIssued(
                attestation.Id,
                attestation.AccountId,
                attestation.Hash,
                attestation.IssuedAt,
                attestation.ExpiresAt
            );

            await _kafkaProducer.PublishAsync(
                topic: Topics.TokenIssued,
                key: attestation.AccountId.ToString(),
                message: evt,
                ct: ct
            );

            // 7) Returner token til klient
            return new IssuedTokenDto
            {
                Token = vcJwt,
                IssuedAt = issuedAt,
                ExpiresAt = expiresAt
            };
        }
    }
}
