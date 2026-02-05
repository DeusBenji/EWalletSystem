namespace IdentityService.Application.Interfaces;

/// <summary>
/// Cache for tracking active MitID authentication sessions
/// Provides CSRF protection and one-time-use enforcement
/// </summary>
public interface IMitIdSessionCache
{
    /// <summary>
    /// Store a session in cache with TTL
    /// </summary>
    /// <param name="sessionId">Signicat session ID</param>
    /// <param name="externalReference">Internal correlation ID (GUID)</param>
    /// <param name="ttl">Time to live for the cache entry</param>
    Task SetSessionAsync(string sessionId, string externalReference, TimeSpan ttl);
    
    /// <summary>
    /// Get the external reference for a session
    /// </summary>
    /// <param name="sessionId">Signicat session ID</param>
    /// <returns>External reference if found, null otherwise</returns>
    Task<string?> GetExternalReferenceAsync(string sessionId);
    
    /// <summary>
    /// Check if a session exists in cache (CSRF validation)
    /// </summary>
    /// <param name="sessionId">Signicat session ID</param>
    /// <returns>True if session exists and is valid</returns>
    Task<bool> ExistsAsync(string sessionId);
    
    /// <summary>
    /// Remove a session from cache (one-time-use enforcement)
    /// </summary>
    /// <param name="sessionId">Signicat session ID</param>
    Task RemoveSessionAsync(string sessionId);
}
