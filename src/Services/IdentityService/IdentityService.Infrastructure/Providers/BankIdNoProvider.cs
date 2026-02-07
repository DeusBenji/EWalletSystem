using IdentityService.Domain.Models;
using IdentityService.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;

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
    
    public string AuthMechanism => "redirect";

    public ProviderCapabilities GetCapabilities() => new()
    {
        ProviderId = ProviderId,
        DisplayName = DisplayName,
        AuthMechanism = "redirect",
        SupportsStatusPolling = true,
        SupportedAttributes = new[] { "dateOfBirth" },
        CanProvideAge = true,
        CanProvideDateOfBirth = true
    };

    public Task<string> GetAuthorizationUrlAsync(string redirectUri, string state)
    {
        throw new NotSupportedException("Norwegian BankID uses Signicat Session flow. Use /api/auth/nbid/start");
    }

    public BankIdNoProvider(ILogger<BankIdNoProvider> logger)
    {
        _logger = logger;
    }
}
