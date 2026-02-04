using Domain.Repositories;
using TokenService.Application.Interfaces;

namespace TokenService.Application.Providers;

/// <summary>
/// Eligibility provider for age verification policy.
/// Checks if account is verified as 18+ via MitID.
/// </summary>
public class MitIdEligibilityProvider : IEligibilityProvider
{
    private readonly IAccountAgeStatusRepository _ageStatusRepo;
    private readonly IAccountAgeStatusCache _ageStatusCache;

    public string PolicyId => "age_over_18";

    public MitIdEligibilityProvider(
        IAccountAgeStatusRepository ageStatusRepo,
        IAccountAgeStatusCache ageStatusCache)
    {
        _ageStatusRepo = ageStatusRepo;
        _ageStatusCache = ageStatusCache;
    }

    public async Task<bool> IsEligibleAsync(Guid accountId, CancellationToken ct = default)
    {
        // 1. Check cache first
        var status = await _ageStatusCache.GetAsync(accountId, ct);

        // 2. If not in cache, fetch from repository
        if (status is null)
        {
            status = await _ageStatusRepo.GetAsync(accountId, ct);
            if (status is not null)
            {
                await _ageStatusCache.SetAsync(status, ct);
            }
        }

        // 3. Return eligibility (must be verified as adult)
        return status is not null && status.IsAdult;
    }

    public PolicyMetadata GetMetadata()
    {
        return new PolicyMetadata
        {
            PolicyId = PolicyId,
            DisplayName = "Age Over 18",
            Description = "Verified as 18 years or older via MitID",
            RequirementsDescription = "MitID verification with age attestation"
        };
    }
}
