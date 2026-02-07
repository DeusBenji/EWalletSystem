using Microsoft.AspNetCore.Mvc;
using IdentityService.Application.Interfaces;
using IdentityService.Application.DTOs;
using IdentityService.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace IdentityService.API.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly ISignicatAuthService _authService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(ISignicatAuthService authService, ILogger<AuthController> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    /// <summary>
    /// Starts an authentication session with the specified provider.
    /// </summary>
    /// <returns>Authentication URL and Session ID</returns>
    [HttpPost("{providerId}/start")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(object), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> StartAuth(string providerId, [FromQuery] Guid? accountId)
    {
        try
        {
            var (authUrl, sessionId) = await _authService.StartAuthenticationAsync(providerId, accountId);
            return Ok(new { authUrl, sessionId });
        }
        catch (ArgumentException ex)
        {
             return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start authentication for provider {ProviderId}", providerId);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Handles the callback from Signicat after user authentication.
    /// </summary>
    /// <returns>Age Verification Result (Sanitized - No PII)</returns>
    /// <response code="200">Returns age verification result. No personal data (CPR/Name) included.</response>
    /// <response code="400">Authentication failed (e.g. CSRF mismatch, missing attribute). See error message.</response>
    [HttpGet("{providerId}/callback")]
    [ProducesResponseType(typeof(AgeVerificationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(object), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Callback(string providerId, [FromQuery] string sessionId)
    {
        if (string.IsNullOrEmpty(sessionId))
        {
            return BadRequest(new { error = "SessionId is required" });
        }

        try
        {
            var result = await _authService.HandleCallbackAsync(providerId, sessionId);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("Authentication failed: {Message}", ex.Message);
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Refusing callback for provider {ProviderId}", providerId);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Checks the status of an ongoing authentication session.
    /// </summary>
    [HttpGet("session/{sessionId}/status")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetSessionStatus(string sessionId)
    {
        try
        {
            var status = await _authService.GetSessionStatusAsync(sessionId);
            return Ok(new { status });
        }
        catch (Exception ex)
        {
             return BadRequest(new { error = ex.Message });
        }
    }
}
