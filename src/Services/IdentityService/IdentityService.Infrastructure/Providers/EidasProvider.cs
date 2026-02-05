using IdentityService.Domain.Interfaces;
using IdentityService.Domain.Models;
using Microsoft.Extensions.Logging;

namespace IdentityService.Infrastructure.Providers;

/// <summary>
/// eIDAS identity provider for EU (placeholder implementation).
/// </summary>
public class EidasProvider : IIdentityProvider
{
    private readonly ILogger<EidasProvider> _logger;
    
    public string ProviderId => "eidas";
    public string Country => "EU";
    public string DisplayName => "eIDAS (European Union)";
    
    public EidasProvider(ILogger<EidasProvider> logger)
    {
        _logger = logger;
    }
    
    public ProviderCapabilities GetCapabilities() => new()
    {
        CanProvideAge = true,  // Sometimes available
        CanProvideName = true,
        CanProvideNationalId = false,  // Privacy-preserving
        CanProvideAddress = false,
        CanProvideEmail = false,
        CanProvidePhone = false
    };
    
    public Task<string> GetAuthorizationUrlAsync(string redirectUri, string state)
    {
        throw new NotImplementedException("eIDAS integration not yet implemented");
    }
    
    public Task<IdentityData> GetIdentityDataAsync(string authCode, CancellationToken ct = default)
    {
        throw new NotImplementedException("eIDAS integration not yet implemented");
    }
}
