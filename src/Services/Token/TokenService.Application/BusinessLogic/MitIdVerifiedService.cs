using Application.Interfaces;
using Domain.Models;
using Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace Application.BusinessLogic
{
    public class MitIdVerifiedService : IMitIdVerifiedService
    {
        private readonly IAccountAgeStatusRepository _repo;
        private readonly IAccountAgeStatusCache _cache;
        private readonly ILogger<MitIdVerifiedService> _logger;

        public MitIdVerifiedService(
            IAccountAgeStatusRepository repo,
            IAccountAgeStatusCache cache,
            ILogger<MitIdVerifiedService> logger)
        {
            _repo = repo;
            _cache = cache;
            _logger = logger;
        }

        public async Task HandleMitIdVerifiedAsync(Guid accountId, bool isAdult, DateTime verifiedAt, CancellationToken ct = default)
        {
            // Build new domain object
            var status = new AccountAgeStatus(
                accountId: accountId,
                isAdult: isAdult,
                verifiedAt: verifiedAt
            );

            // Save to DB
            await _repo.SaveAsync(status, ct);

            // Update cache
            await _cache.SetAsync(status, ct);

            _logger.LogInformation(
                "Updated AccountAgeStatus for AccountId {AccountId}: IsAdult={IsAdult}, VerifiedAt={VerifiedAt}",
                accountId, isAdult, verifiedAt);
        }
    }
}
