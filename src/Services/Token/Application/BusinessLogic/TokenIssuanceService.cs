using Application.DTOs;
using Application.Interfaces;
using AutoMapper;
using BuildingBlocks.Contracts.Events;
using BuildingBlocks.Contracts.Messaging; // antag Topics.TokenIssued
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
        private readonly IKafkaEventProducer _eventProducer;
        private readonly IVcSigningService _vcSigning;

        public TokenIssuanceService(
            IAccountAgeStatusRepository ageStatusRepo,
            IAccountAgeStatusCache ageStatusCache,
            IAttestationRepository attestationRepo,
            IFabricAnchorClient fabric,
            ITokenHashCalculator hashCalculator,
            IKafkaEventProducer eventProducer,
            IVcSigningService vcSigning)
        {
            _ageStatusRepo = ageStatusRepo;
            _ageStatusCache = ageStatusCache;
            _attestationRepo = attestationRepo;
            _fabric = fabric;
            _hashCalculator = hashCalculator;
            _eventProducer = eventProducer;
            _vcSigning = vcSigning;
        }

        public async Task<IssuedTokenDto> IssueTokenAsync(IssueTokenDto dto, CancellationToken ct = default)
        {
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

            // Build Verifiable Credential
            var credential = new AgeOver18Credential
            {
                Issuer = _vcSigning.GetIssuerDid(),
                IssuanceDate = issuedAt,
                ExpirationDate = expiresAt,
                CredentialSubject = new CredentialSubject
                {
                    Id = $"did:example:account:{dto.AccountId}",
                    AgeOver18 = true,
                    VerifiedAt = status.VerifiedAt
                }
            };

            // Sign as JWT
            var vcJwt = _vcSigning.CreateSignedVcJwt(credential);

            // Hash for anchoring
            var hash = _hashCalculator.ComputeHash(vcJwt);

            var attestation = new AgeAttestation(
                dto.AccountId,
                subjectId: dto.AccountId.ToString(),
                isAdult: true,
                issuedAt: issuedAt,
                expiresAt: expiresAt,
                token: vcJwt,
                hash: hash);

            await _fabric.AnchorHashAsync(hash, ct);
            await _attestationRepo.SaveAsync(attestation, ct);

            var evt = new TokenIssued(
                attestation.Id,
                attestation.AccountId,
                attestation.Hash,
                attestation.IssuedAt,
                attestation.ExpiresAt);

            await _eventProducer.PublishAsync(Topics.TokenIssued, evt, ct);

            return new IssuedTokenDto
            {
                Token = vcJwt,
                IssuedAt = issuedAt,
                ExpiresAt = expiresAt
            };
        }

    }
}
