using BachMitID.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace BachMitID.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class IdentityProvidersController : ControllerBase
{
    private readonly IIdentityProviderService _providerService;
    private readonly ILogger<IdentityProvidersController> _logger;
    
    public IdentityProvidersController(
        IIdentityProviderService providerService,
        ILogger<IdentityProvidersController> logger)
    {
        _providerService = providerService;
        _logger = logger;
    }
    
    /// <summary>
    /// Get all available identity providers
    /// </summary>
    [HttpGet]
    public IActionResult GetProviders()
    {
        try
        {
            var providers = _providerService.GetAvailableProviders();
            
            _logger.LogInformation("Returning {Count} available identity providers", providers.Count);
            
            return Ok(providers);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get identity providers");
            return StatusCode(500, new { error = "Failed to retrieve identity providers" });
        }
    }
    
    /// <summary>
    /// Get providers that support age verification
    /// </summary>
    [HttpGet("age-capable")]
    public IActionResult GetAgeCapableProviders()
    {
        try
        {
            var providers = _providerService.GetProvidersByCapability(c => c.CanProvideAge);
            
            _logger.LogInformation("Returning {Count} age-capable providers", providers.Count);
            
            return Ok(providers);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get age-capable providers");
            return StatusCode(500, new { error = "Failed to retrieve providers" });
        }
    }
}
