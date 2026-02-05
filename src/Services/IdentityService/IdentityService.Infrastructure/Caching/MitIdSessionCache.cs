using IdentityService.Application.Interfaces;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace IdentityService.Infrastructure.Caching;

/// <summary>
/// Redis-based implementation of MitID session cache
/// Provides CSRF protection and automatic TTL cleanup
/// </summary>
public class MitIdSessionCache : IMitIdSessionCache
{
    private readonly IDistributedCache _cache;
    private readonly ILogger<MitIdSessionCache> _logger;
    private const string KeyPrefix = "mitid:session:";
    
    public MitIdSessionCache(IDistributedCache cache, ILogger<MitIdSessionCache> logger)
    {
        _cache = cache;
        _logger = logger;
    }
    
    public async Task SetSessionAsync(string sessionId, string externalReference, TimeSpan ttl)
    {
        var key = GetKey(sessionId);
        await _cache.SetStringAsync(key, externalReference, new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = ttl
        });
        
        _logger.LogInformation(
            "Cached MitID session {SessionId} with TTL {TTL}s", 
            MaskSessionId(sessionId), 
            ttl.TotalSeconds);
    }
    
    public async Task<string?> GetExternalReferenceAsync(string sessionId)
    {
        var key = GetKey(sessionId);
        var value = await _cache.GetStringAsync(key);
        
        if (value != null)
        {
            _logger.LogDebug("Found external reference for session {SessionId}", MaskSessionId(sessionId));
        }
        else
        {
            _logger.LogWarning("Session {SessionId} not found in cache", MaskSessionId(sessionId));
        }
        
        return value;
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
            "Removed MitID session {SessionId} from cache (one-time-use)", 
            MaskSessionId(sessionId));
    }
    
    private static string GetKey(string sessionId) => $"{KeyPrefix}{sessionId}";
    
    /// <summary>
    /// Mask session ID for logging (security)
    /// Shows first 4 and last 4 characters only
    /// </summary>
    private static string MaskSessionId(string sessionId)
    {
        if (string.IsNullOrEmpty(sessionId))
            return "***";
        
        if (sessionId.Length <= 8)
            return "***";
        
        return $"{sessionId[..4]}***{sessionId[^4..]}";
    }
}
