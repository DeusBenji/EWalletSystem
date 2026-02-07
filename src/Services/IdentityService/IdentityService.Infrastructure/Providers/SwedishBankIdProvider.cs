using System;
using System.Threading;
using System.Threading.Tasks;
using IdentityService.Domain.Models;
using IdentityService.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace IdentityService.Infrastructure.Providers;

public class SwedishBankIdProvider : IIdentityProvider
{
    private readonly ILogger<SwedishBankIdProvider> _logger;

    public SwedishBankIdProvider(ILogger<SwedishBankIdProvider> logger)
    {
        _logger = logger;
    }

    public string ProviderId => "sbid"; // Must match Signicat canonical ID
    public string Country => "SE";
    public string DisplayName => "BankID (Sweden)";
    
    // AuthMechanism matches IdentityService.Domain.Models.ProviderCapabilities
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

    // Legacy methods - not used for Signicat flow but required by interface
    public Task<string> GetAuthorizationUrlAsync(string redirectUri, string state)
    {
        throw new NotSupportedException("Swedish BankID uses Signicat Session flow. Use /api/auth/sbid/start");
    }
}
