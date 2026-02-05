using Application.DTOs;
using Application.Interfaces;
using BuildingBlocks.Contracts.Credentials;
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
        private readonly IEnumerable<IEligibilityProvider> _eligibilityProviders;

        public TokenIssuanceService(
            IAccountAgeStatusRepository ageStatusRepo,
            IAccountAgeStatusCache ageStatusCache,
            IAttestationRepository attestationRepo,
            IFabricAnchorClient fabric,
            ITokenHashCalculator hashCalculator,
            IKafkaProducer kafkaProducer,
            IVcSigningService vcSigning,
            IEnumerable<IEligibilityProvider> eligibilityProviders)
        {
            _ageStatusRepo = ageStatusRepo;
            _ageStatusCache = ageStatusCache;
            _attestationRepo = attestationRepo;
            _fabric = fabric;
            _hashCalculator = hashCalculator;
            _kafkaProducer = kafkaProducer;
            _vcSigning = vcSigning;
            _eligibilityProviders = eligibilityProviders;
        }

        public async Task<IssuedTokenDto> IssuePolicyCredentialAsync(IssuePolicyCredentialDto dto, CancellationToken ct = default)
        {
            // 1. Find eligibility provider for requested policy
            var provider = _eligibilityProviders.FirstOrDefault(p => p.PolicyId == dto.PolicyId);
            if (provider is null)
            {
                throw new InvalidOperationException($"No eligibility provider found for policy: {dto.PolicyId}");
            }

            // 2. Check eligibility
            var isEligible = await provider.IsEligibleAsync(dto.AccountId, ct);
            if (!isEligible)
            {
                throw new InvalidOperationException($"Account {dto.AccountId} is not eligible for policy: {dto.PolicyId}");
            }

            // 3. Build PolicyProofCredential (no PII, only commitment)
            var issuedAt = DateTime.UtcNow;
            var expiresAt = issuedAt.AddHours(24); // Policy credentials valid for 24 hours

            var policyCredential = new PolicyProofCredential
            {
                PolicyId = dto.PolicyId,
                SubjectCommitment = dto.SubjectCommitment,
                Issuer = _vcSigning.GetIssuerDid(),
                IssuedAt = issuedAt,
                ExpiresAt = expiresAt,
                Signature = "", // Will be populated by signing
                Metadata = new Dictionary<string, string>
                {
                    { "provider", provider.GetType().Name },
                    { "accountId", dto.AccountId.ToString() }
                }
            };

            // 4. Sign credential as JWT
            var subjectId = $"did:commitment:{dto.SubjectCommitment.Substring(0, 16)}";
            var vcJwt = _vcSigning.CreateSignedVcJwt(policyCredential, subjectId, expiresAt);

            // 5. Hash for anchoring
            var hash = _hashCalculator.ComputeHash(vcJwt);

            // 6. Create attestation record
            var attestation = new AgeAttestation(
                accountId: dto.AccountId,
                subjectId: subjectId,
                isAdult: true, // For age policy; generalize this later
                issuedAt: issuedAt,
                expiresAt: expiresAt,
                token: vcJwt,
                hash: hash,
                commitment: dto.SubjectCommitment
            );

            // 7. Anchor + persist
            await _fabric.AnchorHashAsync(hash, ct);
            await _attestationRepo.SaveAsync(attestation, ct);

            // 8. Publish TokenIssued event
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

            // 9. Return credential to client
            return new IssuedTokenDto
            {
                Token = vcJwt,
                IssuedAt = issuedAt,
                ExpiresAt = expiresAt
            };
        }

        public async Task<IssuedTokenDto> IssueTokenAsync(IssueTokenDto dto, CancellationToken ct = default)
        {
            // 1) Hent alder-status (cache ? repo)
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
