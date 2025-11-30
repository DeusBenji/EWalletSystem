using BachMitID.Application.BusinessLogicLayer.Interface;
using BachMitID.Application.DTOs;
using StackExchange.Redis;
using System.Text.Json;

namespace BachMitID.Infrastructure.Cache
{
    public class MitIdAccountCache : IMitIdAccountCache
    {
        private readonly IDatabase _db;

        public MitIdAccountCache(IConnectionMultiplexer redis)
        {
            _db = redis.GetDatabase();
        }

        private static string Key(Guid accountId)
            => $"mitid:account:{accountId}";

        public async Task<MitIdAccountDto?> GetAsync(Guid accountId)
        {
            var value = await _db.StringGetAsync(Key(accountId));
            if (value.IsNullOrEmpty)
                return null;

            return JsonSerializer.Deserialize<MitIdAccountDto>(value!);
        }

        public async Task SetAsync(MitIdAccountDto dto, TimeSpan ttl)
        {
            var json = JsonSerializer.Serialize(dto);
            await _db.StringSetAsync(Key(dto.AccountId), json, ttl);
        }

        public async Task RemoveAsync(Guid accountId)
        {
            await _db.KeyDeleteAsync(Key(accountId));
        }
    }
}
