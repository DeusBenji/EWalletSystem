using IdentityService.Application.DTOs;

namespace IdentityService.Application.Interfaces;

/// <summary>
/// Generic authentication service supporting multiple Nordic identity providers
/// Replaces provider-specific services with unified interface
/// </summary>
public interface ISignicatAuthService
{
    /// <summary>
    /// Start authentication flow for a provider
    /// </summary>
    /// <param name="providerId">Provider identifier (mitid, sbid, nbid)</param>
    /// <param name="accountId">Optional account ID to link verification to</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Authentication URL and session ID</returns>
    Task<(string authUrl, string sessionId)> StartAuthenticationAsync(
        string providerId,
        Guid? accountId = null,
        CancellationToken ct = default);
    
    /// <summary>
    /// Handle callback from provider after authentication
    /// </summary>
    /// <param name="providerId">Provider that authenticated the user</param>
    /// <param name="sessionId">Session ID from callback</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Age verification result (minimal data only)</returns>
    Task<AgeVerificationDto> HandleCallbackAsync(
        string providerId,
        string sessionId,
        CancellationToken ct = default);
    
    /// <summary>
    /// Get authentication session status
    /// </summary>
    /// <param name="sessionId">Session ID to check</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Session status (sanitized, no PII)</returns>
    Task<string> GetSessionStatusAsync(
        string sessionId,
        CancellationToken ct = default);
}
