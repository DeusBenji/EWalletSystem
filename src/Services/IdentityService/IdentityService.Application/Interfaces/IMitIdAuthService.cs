using IdentityService.Application.DTOs;

namespace IdentityService.Application.Interfaces;

/// <summary>
/// Service for handling MitID authentication via Signicat session flow
/// </summary>
public interface IMitIdAuthService
{
    /// <summary>
    /// Start authentication session
    /// </summary>
    /// <param name="accountId">Account ID to associate with this authentication</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Authentication URL to redirect user to</returns>
    Task<string> StartAuthenticationAsync(Guid accountId, CancellationToken ct = default);
    
    /// <summary>
    /// Handle callback from Signicat after authentication
    /// </summary>
    /// <param name="sessionId">Signicat session ID</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>MitID account result if successful, null if failed</returns>
    Task<MitIdAccountResult?> HandleCallbackAsync(string sessionId, CancellationToken ct = default);
    
    /// <summary>
    /// Get session status (for polling)
    /// </summary>
    /// <param name="sessionId">Signicat session ID</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Session status</returns>
    Task<GetSessionResponse> GetSessionStatusAsync(string sessionId, CancellationToken ct = default);
}
