using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using IdentityService.Application.DTOs;
using IdentityService.Application.Interfaces;
using IdentityService.Domain.Interfaces;
using IdentityService.Infrastructure.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace IdentityService.Infrastructure.Http;

/// <summary>
/// HTTP client for Signicat EID Hub REST API
/// Handles authentication, retry logic, and request/response logging
/// </summary>
public class SignicatHttpClient : ISignicatHttpClient
{
    private readonly HttpClient _httpClient;
    private readonly SignicatConfig _config;
    private readonly ISafeLogger<SignicatHttpClient> _logger;
    private readonly ISignicatAccessTokenCache _tokenCache;
    private readonly JsonSerializerOptions _jsonOptions;
    
    public SignicatHttpClient(
        HttpClient httpClient, 
        IOptions<SignicatConfig> config,
        ISafeLogger<SignicatHttpClient> logger,
        ISignicatAccessTokenCache tokenCache)
    {
        _httpClient = httpClient;
        _config = config.Value;
        _logger = logger;
        _tokenCache = tokenCache;
        
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };
        
        ConfigureHttpClient();
    }
    
    private void ConfigureHttpClient()
    {
        _httpClient.BaseAddress = new Uri(_config.BaseUrl.TrimEnd('/') + "/");
        _httpClient.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/json"));
    }

    private async Task EnsureAuthenticatedAsync(CancellationToken ct)
    {
        // Add authentication header
        if (!string.IsNullOrWhiteSpace(_config.ApiKey))
        {
             if (_httpClient.DefaultRequestHeaders.Authorization == null)
             {
                _httpClient.DefaultRequestHeaders.Authorization = 
                    new AuthenticationHeaderValue("Bearer", _config.ApiKey);
             }
        }
        else if (!string.IsNullOrWhiteSpace(_config.ClientId))
        {
            // Use Client Credentials flow via Cache
            var token = await _tokenCache.GetAccessTokenAsync(ct);
            _httpClient.DefaultRequestHeaders.Authorization = 
                new AuthenticationHeaderValue("Bearer", token);
        }
    }
    
    /// <summary>
    /// Create a new authentication session
    /// </summary>
    public async Task<SessionDataDto> CreateSessionAsync(
        SessionRequestDto request, 
        CancellationToken ct = default)
    {
        await EnsureAuthenticatedAsync(ct);

        _logger.LogInformation(
            "Creating Signicat session with flow={Flow}, providers={Providers}", 
            request.Flow, 
            string.Join(",", request.AllowedProviders ?? new List<string>()));
        
        var json = JsonSerializer.Serialize(request, _jsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        
        var response = await _httpClient.PostAsync("sessions", content, ct);
        
        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(ct);
            _logger.LogError(
                "Failed to create session: {StatusCode} - {Error}", 
                response.StatusCode, 
                MaskSensitiveData(errorBody));
            response.EnsureSuccessStatusCode();
        }
        
        var responseJson = await response.Content.ReadAsStringAsync(ct);
        var result = JsonSerializer.Deserialize<SessionDataDto>(responseJson, _jsonOptions);
        
        if (result == null)
            throw new InvalidOperationException("Failed to deserialize SessionDataDto");
        
        _logger.LogInformation(
            "Created session {SessionId}, expires at {ExpiresAt}", 
            MaskSessionId(result.Id!), 
            result.ExpiresAt);
        
        return result;
    }
    
    /// <summary>
    /// Get session status
    /// </summary>
    public async Task<SessionDataDto> GetSessionStatusAsync(
        string sessionId, 
        CancellationToken ct = default)
    {
        _logger.LogDebug("Getting status for session {SessionId}", MaskSessionId(sessionId));
        
        var response = await _httpClient.GetAsync($"sessions/{sessionId}", ct);
        
        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(ct);
            _logger.LogError(
                "Failed to get session status: {StatusCode} - {Error}", 
                response.StatusCode, 
                MaskSensitiveData(errorBody));
            response.EnsureSuccessStatusCode();
        }
        
        var responseJson = await response.Content.ReadAsStringAsync(ct);
        var result = JsonSerializer.Deserialize<SessionDataDto>(responseJson, _jsonOptions);
        
        if (result == null)
            throw new InvalidOperationException("Failed to deserialize SessionDataDto");
        
        _logger.LogInformation(
            "Session {SessionId} status: {Status}, provider: {Provider}", 
            MaskSessionId(result.Id!), 
            result.Status,
            result.Provider ?? "unknown");
        
        return result;
    }
    
    /// <summary>
    /// Mask session ID for logging (show first 4 and last 4 chars)
    /// </summary>
    private static string MaskSessionId(string sessionId)
    {
        if (string.IsNullOrEmpty(sessionId) || sessionId.Length <= 8)
            return "***";
        
        return $"{sessionId[..4]}***{sessionId[^4..]}";
    }
    
    /// <summary>
    /// Mask sensitive data in logs (CPR, names, etc.)
    /// </summary>
    private static string MaskSensitiveData(string data)
    {
        if (string.IsNullOrEmpty(data))
            return data;
        
        // Mask CPR-like patterns (10 digits)
        data = System.Text.RegularExpressions.Regex.Replace(
            data, 
            @"\b\d{10}\b", 
            "**CPR**");
        
        // Mask nationalId fields in JSON
        data = System.Text.RegularExpressions.Regex.Replace(
            data, 
            @"""nationalId""\s*:\s*""[^""]+""", 
            "\"nationalId\":\"***\"");
        
        // Truncate if too long
        if (data.Length > 500)
            data = data[..500] + "... (truncated)";
        
        return data;
    }
}
