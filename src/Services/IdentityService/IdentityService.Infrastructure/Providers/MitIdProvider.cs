using IdentityService.Domain.Enums;
using IdentityService.Domain.Interfaces;
using IdentityService.Domain.Models;
using Microsoft.Extensions.Logging;

namespace IdentityService.Infrastructure.Providers;

/// <summary>
/// MitID identity provider metadata.
/// Authentication flow is handled by SignicatAuthService.
/// This class serves only as a discovery plugin.
/// </summary>
public sealed class MitIdProvider : IIdentityProvider
{
    private readonly ILogger<MitIdProvider> _logger;

    public MitIdProvider(ILogger<MitIdProvider> logger)
    {
        _logger = logger;
    }

    public string ProviderId => "mitid";
    public string Country => "DK";
    public string DisplayName => "MitID (Denmark)";
    
    // Indicates this provider uses the centralized Signicat session flow
    public AuthMechanism AuthMechanism => AuthMechanism.SessionBased;

    public ProviderCapabilities GetCapabilities() => new(
        CanProvideAge: true,
        CanProvideDateOfBirth: true); // Via claims mapper

    public Task<string> GetAuthorizationUrlAsync(string redirectUri, string state)
    {
        throw new NotSupportedException(
            "Direct authorization URL generation is not supported. Use ISignicatAuthService.StartAuthenticationAsync.");
    }

    public Task<IdentityData> GetIdentityDataAsync(string authCode, CancellationToken ct = default)
    {
         throw new NotSupportedException(
            "Legacy IdentityData retrieval is not supported. Use ISignicatAuthService.HandleCallbackAsync.");
    }
}
