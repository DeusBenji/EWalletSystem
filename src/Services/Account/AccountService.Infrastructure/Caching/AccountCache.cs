using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Application.DTOs;
using Application.Interfaces;
using StackExchange.Redis;

namespace Infrastructure.Caching
{
    public class AccountCache : IAccountCache
    {
        private readonly IDatabase _db;

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        private static string GetKey(Guid accountId) => $"account:{accountId}";
        private static readonly TimeSpan DefaultTtl = TimeSpan.FromMinutes(10);

        public AccountCache(IConnectionMultiplexer connection)
        {
            _db = connection.GetDatabase();
        }

        public async Task<AccountDto?> GetAccountAsync(Guid id, CancellationToken ct = default)
        {
            var key = GetKey(id);
            var value = await _db.StringGetAsync(key);

            if (!value.HasValue)
                return null;

            try
            {
                return JsonSerializer.Deserialize<AccountDto>(value.ToString(), JsonOptions);
            }
            catch
            {
                await _db.KeyDeleteAsync(key);
                return null;
            }
        }

        public async Task SetAccountAsync(AccountDto dto, CancellationToken ct = default)
        {
            var key = GetKey(dto.Id);
            var json = JsonSerializer.Serialize(dto, JsonOptions);

            await _db.StringSetAsync(key, json, DefaultTtl);
        }

        public async Task InvalidateAsync(Guid id, CancellationToken ct = default)
        {
            var key = GetKey(id);
            await _db.KeyDeleteAsync(key);
        }
    }
}
