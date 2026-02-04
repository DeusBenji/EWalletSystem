namespace BachMitID.Domain.Interfaces;

/// <summary>
/// Interface for identity providers (MitID, BankID, eIDAS, etc.)
/// Enables multi-country support through plugin architecture.
/// </summary>
public interface IIdentityProvider
{
    /// <summary>
    /// Unique identifier for this provider (e.g., "mitid", "bankid-no", "eidas")
    /// </summary>
    string ProviderId { get; }
    
    /// <summary>
    /// ISO 3166-1 alpha-2 country code (e.g., "DK", "NO", "EU")
    /// </summary>
    string Country { get; }
    
    /// <summary>
    /// Human-readable display name
    /// </summary>
    string DisplayName { get; }
    
    /// <summary>
    /// Get the authorization URL for this provider
    /// </summary>
    Task<string> GetAuthorizationUrlAsync(string redirectUri, string state);
    
    /// <summary>
    /// Exchange authorization code for identity data
    /// </summary>
    Task<IdentityData> GetIdentityDataAsync(string authCode, CancellationToken ct = default);
    
    /// <summary>
    /// Get provider capabilities (what data can it provide?)
    /// </summary>
    ProviderCapabilities GetCapabilities();
}
