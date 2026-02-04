using BachMitID.Application.Interfaces;
using BachMitID.Domain.Interfaces;
using BachMitID.Domain.Models;
using Microsoft.Extensions.Logging;

namespace BachMitID.Application.Services;

/// <summary>
/// Service for managing identity providers.
/// Implements provider discovery, selection, and authentication.
/// </summary>
public class IdentityProviderService : IIdentityProviderService
{
    private readonly IEnumerable<IIdentityProvider> _providers;
    private readonly ILogger<IdentityProviderService> _logger;
    
    public IdentityProviderService(
        IEnumerable<IIdentityProvider> providers,
        ILogger<IdentityProviderService> logger)
    {
        _providers = providers ?? throw new ArgumentNullException(nameof(providers));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        
        _logger.LogInformation(
            "IdentityProviderService initialized with {Count} providers: {Providers}",
            _providers.Count(),
            string.Join(", ", _providers.Select(p => p.ProviderId)));
    }
    
    public IIdentityProvider GetProvider(string providerId)
    {
        if (string.IsNullOrWhiteSpace(providerId))
        {
            throw new ArgumentException("Provider ID cannot be null or empty", nameof(providerId));
        }
        
        var provider = _providers.FirstOrDefault(p => 
            p.ProviderId.Equals(providerId, StringComparison.OrdinalIgnoreCase));
        
        if (provider == null)
        {
            throw new InvalidOperationException(
                $"Identity provider '{providerId}' not found. " +
                $"Available providers: {string.Join(", ", _providers.Select(p => p.ProviderId))}");
        }
        
        return provider;
    }
    
    public List<ProviderInfo> GetAvailableProviders()
    {
        return _providers.Select(p => new ProviderInfo
        {
            ProviderId = p.ProviderId,
            Country = p.Country,
            DisplayName = p.DisplayName,
            Capabilities = p.GetCapabilities()
        }).ToList();
    }
    
    public async Task<IdentityData> AuthenticateAsync(
        string providerId, 
        string authCode, 
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(authCode))
        {
            throw new ArgumentException("Authorization code cannot be null or empty", nameof(authCode));
        }
        
        var provider = GetProvider(providerId);
        
        _logger.LogInformation(
            "Authenticating user with provider {ProviderId}",
            providerId);
        
        try
        {
            var identityData = await provider.GetIdentityDataAsync(authCode, ct);
            
            _logger.LogInformation(
                "Successfully authenticated user {Subject} via {ProviderId}",
                identityData.Subject,
                providerId);
            
            return identityData;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to authenticate user with provider {ProviderId}",
                providerId);
            throw;
        }
    }
    
    public List<ProviderInfo> GetProvidersByCapability(Func<ProviderCapabilities, bool> predicate)
    {
        if (predicate == null)
        {
            throw new ArgumentNullException(nameof(predicate));
        }
        
        return _providers
            .Where(p => predicate(p.GetCapabilities()))
            .Select(p => new ProviderInfo
            {
                ProviderId = p.ProviderId,
                Country = p.Country,
                DisplayName = p.DisplayName,
                Capabilities = p.GetCapabilities()
            })
            .ToList();
    }
}
