using System.Text.Json;
using System.Text.Json.Serialization;
using IdentityService.Application.Interfaces;
using IdentityService.Infrastructure.Configuration;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace IdentityService.Infrastructure.Services;

public class SignicatAccessTokenCache : ISignicatAccessTokenCache
{
    private readonly IMemoryCache _cache;
    private readonly HttpClient _httpClient;
    private readonly SignicatConfig _config;
    private readonly ILogger<SignicatAccessTokenCache> _logger;
    private const string CacheKey = "SignicatAccessToken";

    public SignicatAccessTokenCache(
        IMemoryCache cache, 
        HttpClient httpClient, 
        IOptions<SignicatConfig> config,
        ILogger<SignicatAccessTokenCache> logger)
    {
        _cache = cache;
        _httpClient = httpClient;
        _config = config.Value;
        _logger = logger;
    }

    public async Task<string> GetAccessTokenAsync(CancellationToken ct = default)
    {
        if (_cache.TryGetValue(CacheKey, out string? cachedToken) && !string.IsNullOrEmpty(cachedToken))
        {
            return cachedToken!;
        }

        // Double-check locking not strictly necessary for memory cache but good for reducing concurrent requests
        // IMemoryCache is thread-safe, but we might have multiple requests hitting endpoint. 
        // For simplicity, we'll just let the race happen (last one wins), it's rare enough.
        
        return await FetchAndCacheTokenAsync(ct);
    }

    private async Task<string> FetchAndCacheTokenAsync(CancellationToken ct)
    {
        _logger.LogInformation("Fetching new access token from Signicat...");

        var tokenUrl = !string.IsNullOrEmpty(_config.TokenEndpoint) 
            ? _config.TokenEndpoint 
            : $"{_config.BaseUrl.TrimEnd('/')}/auth/open/connect/token"; 
        
        var request = new HttpRequestMessage(HttpMethod.Post, tokenUrl);
        var credentials = Convert.ToBase64String(System.Text.Encoding.ASCII.GetBytes($"{_config.ClientId}:{_config.ClientSecret}"));
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", credentials);
        
        request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "grant_type", "client_credentials" },
            { "scope", "signicat-api" } // Verify scope needed
        });

        var response = await _httpClient.SendAsync(request, ct);
        
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(ct);
            _logger.LogError("Failed to fetch access token: {StatusCode} {Error}", response.StatusCode, error);
            response.EnsureSuccessStatusCode();
        }

        using var contentStream = await response.Content.ReadAsStreamAsync(ct);
        var tokenResponse = await JsonSerializer.DeserializeAsync<TokenResponse>(contentStream, cancellationToken: ct);
        
        if (tokenResponse == null || string.IsNullOrEmpty(tokenResponse.AccessToken))
        {
            throw new InvalidOperationException("Received invalid token response from Signicat");
        }

        // Cache with buffer (e.g. 60 seconds less than expiry)
        var expiry = TimeSpan.FromSeconds(Math.Max(tokenResponse.ExpiresIn - 60, 60));
        
        _cache.Set(CacheKey, tokenResponse.AccessToken, expiry);
        
        _logger.LogInformation("Successfully cached new access token. Expires in {Seconds}s", tokenResponse.ExpiresIn);

        return tokenResponse.AccessToken;
    }

    private class TokenResponse
    {
        [JsonPropertyName("access_token")]
        public string? AccessToken { get; set; }

        [JsonPropertyName("expires_in")]
        public int ExpiresIn { get; set; }
        
        [JsonPropertyName("token_type")]
        public string? TokenType { get; set; }
    }
}
