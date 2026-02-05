using IdentityService.Domain.Interfaces;
using IdentityService.Domain.Models;

namespace IdentityService.Application.Interfaces;

/// <summary>
/// Service for managing identity providers.
/// Provides provider discovery, selection, and authentication.
/// </summary>
public interface IIdentityProviderService
{
    /// <summary>
    /// Get a specific identity provider by ID
    /// </summary>
    IIdentityProvider GetProvider(string providerId);
    
    /// <summary>
    /// Get all available identity providers
    /// </summary>
    List<ProviderInfo> GetAvailableProviders();
    
    /// <summary>
    /// Authenticate a user with a specific provider
    /// </summary>
    Task<IdentityData> AuthenticateAsync(string providerId, string authCode, CancellationToken ct = default);
    
    /// <summary>
    /// Get providers that support a specific capability
    /// </summary>
    List<ProviderInfo> GetProvidersByCapability(Func<ProviderCapabilities, bool> predicate);
}
