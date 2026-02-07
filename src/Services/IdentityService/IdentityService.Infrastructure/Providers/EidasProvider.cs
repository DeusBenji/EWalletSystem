using IdentityService.Domain.Enums;
using IdentityService.Domain.Interfaces;
using IdentityService.Domain.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;

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
    
    public AuthMechanism AuthMechanism => AuthMechanism.OAuth; // Placeholder

    public ProviderCapabilities GetCapabilities() => new(
        CanProvideAge: false,
        CanProvideDateOfBirth: false);

    public Task<IdentityData> GetIdentityDataAsync(string authorizationCode, CancellationToken ct = default)
        => throw new NotSupportedException(
            "Legacy GetIdentityDataAsync is not supported. Use /api/auth/{providerId} session flow.");

    public Task<string> GetAuthorizationUrlAsync(string redirectUri, string state)
    {
        throw new NotImplementedException("eIDAS integration not yet implemented");
    }

    public EidasProvider(ILogger<EidasProvider> logger)
    {
        _logger = logger;
    }
    

}
