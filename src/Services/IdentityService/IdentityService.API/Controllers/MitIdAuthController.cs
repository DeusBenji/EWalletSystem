using IdentityService.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace IdentityService.API.Controllers;

/// <summary>
/// Controller for MitID authentication endpoints
/// </summary>
[ApiController]
[Route("api/mitid/auth")]
public class MitIdAuthController : ControllerBase
{
    private readonly IMitIdAuthService _authService;
    private readonly ILogger<MitIdAuthController> _logger;
    
    public MitIdAuthController(
        IMitIdAuthService authService,
        ILogger<MitIdAuthController> logger)
    {
        _authService = authService;
        _logger = logger;
    }
    
    /// <summary>
    /// Start MitID authentication
    /// </summary>
    /// <param name="request">Start auth request with accountId</param>
    /// <returns>Authentication URL to redirect user to</returns>
    [HttpPost("start")]
    [ProducesResponseType(typeof(StartAuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> StartAuth([FromBody] StartAuthRequest request)
    {
        try
        {
            _logger.LogInformation("Starting MitID auth for account {AccountId}", request.AccountId);
            
            var authUrl = await _authService.StartAuthenticationAsync(request.AccountId);
            
            return Ok(new StartAuthResponse
            {
                AuthenticationUrl = authUrl
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting MitID authentication");
            return StatusCode(500, new { error = "Failed to start authentication" });
        }
    }
    
    /// <summary>
    /// Callback endpoint for Signicat after authentication
    /// </summary>
    /// <param name="sessionId">Signicat session ID</param>
    /// <param name="status">Status from callback (success, abort, error)</param>
    /// <returns>Redirect to frontend with result</returns>
    [HttpGet("callback")]
    [ProducesResponseType(StatusCodes.Status302Found)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Callback(
        [FromQuery] string sessionId,
        [FromQuery] string? status)
    {
        try
        {
            _logger.LogInformation(
                "Received MitID callback: sessionId present={HasSession}, status={Status}",
                !string.IsNullOrEmpty(sessionId),
                status ?? "none");
            
            if (string.IsNullOrEmpty(sessionId))
            {
                _logger.LogWarning("Callback received without sessionId");
                return Redirect($"{GetFrontendUrl()}/auth/error?reason=missing_session");
            }
            
            // Handle abort/error status
            if (status == "abort")
            {
                _logger.LogInformation("User aborted authentication");
                return Redirect($"{GetFrontendUrl()}/auth/abort");
            }
            
            if (status == "error")
            {
                _logger.LogWarning("Authentication error from Signicat");
                return Redirect($"{GetFrontendUrl()}/auth/error?reason=provider_error");
            }
            
            // Handle successful authentication
            var result = await _authService.HandleCallbackAsync(sessionId);
            
            if (result == null)
            {
                _logger.LogWarning("HandleCallback returned null - authentication failed");
                return Redirect($"{GetFrontendUrl()}/auth/error?reason=auth_failed");
            }
            
            _logger.LogInformation(
                "MitID authentication successful for account {AccountId}, isNew={IsNew}",
                result.Account.AccountId,
                result.IsNew);
            
            // Redirect to frontend success page
            return Redirect($"{GetFrontendUrl()}/auth/success?accountId={result.Account.AccountId}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling MitID callback");
            return Redirect($"{GetFrontendUrl()}/auth/error?reason=server_error");
        }
    }
    
    /// <summary>
    /// Get session status (for polling from frontend)
    /// </summary>
    /// <param name="sessionId">Signicat session ID</param>
    /// <returns>Session status</returns>
    [HttpGet("sessions/{sessionId}/status")]
    [ProducesResponseType(typeof(Application.DTOs.GetSessionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetSessionStatus(string sessionId)
    {
        try
        {
            var status = await _authService.GetSessionStatusAsync(sessionId);
            return Ok(status);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Session not found: {SessionId}", sessionId);
            return NotFound(new { error = "Session not found or expired" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting session status");
            return StatusCode(500, new { error = "Failed to get session status" });
        }
    }
    
    private string GetFrontendUrl()
    {
        // TODO: Get from configuration
        return "http://localhost:5174";
    }
}

/// <summary>
/// Request to start authentication
/// </summary>
public class StartAuthRequest
{
    public Guid AccountId { get; set; }
}

/// <summary>
/// Response with authentication URL
/// </summary>
public class StartAuthResponse
{
    public string AuthenticationUrl { get; set; } = null!;
}
