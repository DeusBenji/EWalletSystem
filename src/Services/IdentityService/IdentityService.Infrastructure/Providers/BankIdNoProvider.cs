using IdentityService.Domain.Interfaces;
using IdentityService.Domain.Models;
using Microsoft.Extensions.Logging;

namespace IdentityService.Infrastructure.Providers;

/// <summary>
/// BankID Norway identity provider (placeholder implementation).
/// </summary>
public class BankIdNoProvider : IIdentityProvider
{
    private readonly ILogger<BankIdNoProvider> _logger;
    
    public string ProviderId => "bankid-no";
    public string Country => "NO";
    public string DisplayName => "BankID (Norway)";
    
    public BankIdNoProvider(ILogger<BankIdNoProvider> logger)
    {
        _logger = logger;
    }
    
    public ProviderCapabilities GetCapabilities() => new()
    {
        CanProvideAge = true,
        CanProvideName = true,
        CanProvideNationalId = true,
        CanProvideAddress = true,
        CanProvideEmail = false,
        CanProvidePhone = true
    };
    
    public Task<string> GetAuthorizationUrlAsync(string redirectUri, string state)
    {
        throw new NotImplementedException("BankID Norway integration not yet implemented");
    }
    
    public Task<IdentityData> GetIdentityDataAsync(string authCode, CancellationToken ct = default)
    {
        throw new NotImplementedException("BankID Norway integration not yet implemented");
    }
}
