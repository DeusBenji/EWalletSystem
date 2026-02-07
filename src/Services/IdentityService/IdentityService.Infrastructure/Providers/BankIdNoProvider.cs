using IdentityService.Domain.Enums;
using IdentityService.Domain.Interfaces;
using IdentityService.Domain.Models;
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
    
    public AuthMechanism AuthMechanism => AuthMechanism.SessionBased;

    public ProviderCapabilities GetCapabilities() => new(
        CanProvideAge: true,
        CanProvideDateOfBirth: true);

    public Task<IdentityData> GetIdentityDataAsync(string authorizationCode, CancellationToken ct = default)
        => throw new NotSupportedException(
            "Norwegian BankID uses Signicat Session flow. Use /api/auth/nbid/start");

    public Task<string> GetAuthorizationUrlAsync(string redirectUri, string state)
    {
        throw new NotSupportedException("Norwegian BankID uses Signicat Session flow. Use /api/auth/nbid/start");
    }

    public BankIdNoProvider(ILogger<BankIdNoProvider> logger)
    {
        _logger = logger;
    }
}
