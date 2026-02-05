using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Application.Interfaces;
using Domain.Models;
using StackExchange.Redis;

namespace Infrastructure.Redis
{
    public class AccountAgeStatusCache : IAccountAgeStatusCache
    {
        private readonly IDistributedCache _cache;
        private readonly ILogger<AccountAgeStatusCache> _logger;
        private const string Prefix = "account-age-status:";

        public AccountAgeStatusCache(IDistributedCache cache, ILogger<AccountAgeStatusCache> logger)
        {
            _cache = cache;
            _logger = logger;
        }

        public async Task<AccountAgeStatus?> GetAsync(Guid accountId, CancellationToken ct = default)
        {
            var key = Prefix + accountId;

            var data = await _cache.GetStringAsync(key, ct);
            if (data is null)
                return null;

            try
            {
                return JsonSerializer.Deserialize<AccountAgeStatus>(data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to deserialize AccountAgeStatus from cache for {AccountId}", accountId);
                return null;
            }
        }

        public async Task SetAsync(AccountAgeStatus status, CancellationToken ct = default)
        {
            var key = Prefix + status.AccountId;
            var json = JsonSerializer.Serialize(status);

            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10)
            };

            await _cache.SetStringAsync(key, json, options, ct);
        }
    }
}
