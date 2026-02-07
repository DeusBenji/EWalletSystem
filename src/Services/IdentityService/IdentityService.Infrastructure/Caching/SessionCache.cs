using IdentityService.Application.Interfaces;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace IdentityService.Infrastructure.Caching;

/// <summary>
/// Redis-based implementation of generic session cache
/// Provides CSRF protection and automatic TTL cleanup
/// </summary>
public class SessionCache : ISessionCache
{
    private readonly IDistributedCache _cache;
    private readonly ILogger<SessionCache> _logger;
    private const string KeyPrefix = "auth:session:";
    
    public SessionCache(IDistributedCache cache, ILogger<SessionCache> logger)
    {
        _cache = cache;
        _logger = logger;
    }
    
    public async Task SetSessionAsync(string sessionId, string externalReference, string providerId, TimeSpan ttl)
    {
        var key = GetKey(sessionId);
        // Store value as "providerId:reference"
        var value = $"{providerId}:{externalReference}";
        
        await _cache.SetStringAsync(key, value, new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = ttl
        });
        
        _logger.LogInformation(
            "Cached session {SessionId} for provider {ProviderId} with TTL {TTL}s", 
            MaskSessionId(sessionId), 
            providerId,
            ttl.TotalSeconds);
    }
    
    public async Task<(string? Reference, string? ProviderId)> GetSessionDataAsync(string sessionId)
    {
        var key = GetKey(sessionId);
        var value = await _cache.GetStringAsync(key);
        
        if (value != null)
        {
            _logger.LogDebug("Found data for session {SessionId}", MaskSessionId(sessionId));
            var parts = value.Split(':', 2);
            if (parts.Length == 2)
            {
                return (parts[1], parts[0]); // Reference, ProviderId
            }
        }
        else
        {
            _logger.LogWarning("Session {SessionId} not found in cache", MaskSessionId(sessionId));
        }
        
        return (null, null);
    }
    
    public async Task<bool> ExistsAsync(string sessionId)
    {
        var key = GetKey(sessionId);
        var value = await _cache.GetStringAsync(key);
        var exists = value != null;
        
        _logger.LogDebug(
            "Session {SessionId} exists check: {Exists}", 
            MaskSessionId(sessionId), 
            exists);
        
        return exists;
    }
    
    public async Task RemoveSessionAsync(string sessionId)
    {
        var key = GetKey(sessionId);
        await _cache.RemoveAsync(key);
        
        _logger.LogInformation(
            "Removed sesssion {SessionId} from cache (one-time-use)", 
            MaskSessionId(sessionId));
    }
    
    private static string GetKey(string sessionId) => $"{KeyPrefix}{sessionId}";
    
    private static string MaskSessionId(string sessionId)
    {
        if (string.IsNullOrEmpty(sessionId) || sessionId.Length <= 8)
            return "***";
        return $"{sessionId[..4]}***{sessionId[^4..]}";
    }
}
