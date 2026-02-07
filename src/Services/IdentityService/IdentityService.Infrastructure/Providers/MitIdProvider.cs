using IdentityService.Domain.Models;
using IdentityService.Domain.Interfaces;
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
    }; // Via claims mapper

    public Task<string> GetAuthorizationUrlAsync(string redirectUri, string state)
    {
        throw new NotSupportedException(
            "Direct authorization URL generation is not supported. Use ISignicatAuthService.StartAuthenticationAsync.");
    }
}
