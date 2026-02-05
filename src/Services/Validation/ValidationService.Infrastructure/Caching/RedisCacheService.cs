// Shared.Infrastructure.Redis / RedisCacheService.cs
using Application.Interfaces;
using StackExchange.Redis;
using System.Text.Json;

namespace Infrastructure.Caching
{
    public class RedisCacheService : ICacheService
    {
        private readonly IDatabase _database;

        public RedisCacheService(IConnectionMultiplexer multiplexer)
        {
            _database = multiplexer.GetDatabase();
        }

        public async Task<T?> GetAsync<T>(string key, CancellationToken ct = default)
        {
            var value = await _database.StringGetAsync(key);
            if (value.IsNullOrEmpty) return default;

            var json = (string)value!;
            return JsonSerializer.Deserialize<T>(json);
        }

        public async Task SetAsync<T>(string key, T value, TimeSpan ttl, CancellationToken ct = default)
        {
            var json = JsonSerializer.Serialize(value);
            await _database.StringSetAsync(key, json, ttl);
        }
    }

}