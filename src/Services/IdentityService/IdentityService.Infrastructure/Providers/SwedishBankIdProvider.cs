using System;
using System.Threading;
using System.Threading.Tasks;
using IdentityService.Domain.Enums;
using IdentityService.Domain.Interfaces;
using IdentityService.Domain.Models; // Ensure this exists or use DTOs if IIdentityProvider uses them
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
    
    // AuthMechanism must match IdentityService.Domain.Enums.AuthMechanism
    public AuthMechanism AuthMechanism => AuthMechanism.SessionBased;
    
    public ProviderCapabilities GetCapabilities() => new(
        CanProvideAge: true,
        CanProvideDateOfBirth: true
    );

    // Legacy methods - not used for Signicat flow but required by interface
    public Task<string> GetAuthorizationUrlAsync(string redirectUri, string state)
    {
        throw new NotSupportedException("Swedish BankID uses Signicat Session flow. Use /api/auth/sbid/start");
    }

    public Task<IdentityData> GetIdentityDataAsync(string authorizationCode, CancellationToken ct = default)
    {
         throw new NotSupportedException("Swedish BankID uses Signicat Session flow.");
    }
}
