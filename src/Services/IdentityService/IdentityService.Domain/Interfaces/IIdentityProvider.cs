using IdentityService.Domain.Enums;
using IdentityService.Domain.Models;

namespace IdentityService.Domain.Interfaces;

/// <summary>
/// Interface for identity providers (MitID, BankID, eIDAS, etc.)
/// Enables multi-country support through plugin architecture.
/// </summary>
public interface IIdentityProvider
{
    /// <summary>
    /// Unique identifier for this provider (e.g., "mitid", "sbid", "nbid")
    /// </summary>
    string ProviderId { get; }
    
    /// <summary>
    /// ISO 3166-1 alpha-2 country code (e.g., "DK", "SE", "NO")
    /// </summary>
    string Country { get; }
    
    /// <summary>
    /// Human-readable display name
    /// </summary>
    string DisplayName { get; }
    
    /// <summary>
    /// Authentication mechanism used by this provider (e.g. "redirect", "embedded")
    /// </summary>
    string AuthMechanism { get; }
    
    /// <summary>
    /// Get the authorization URL for this provider
    /// </summary>
    Task<string> GetAuthorizationUrlAsync(string redirectUri, string state);
    
    /// <summary>
    /// Get provider capabilities (what data can it provide?)
    /// </summary>
    ProviderCapabilities GetCapabilities();
}
