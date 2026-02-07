namespace IdentityService.Application.DTOs;

/// <summary>
/// Configuration options for a specific provider's authentication requirements
/// </summary>
public class ProviderAuthOptions
{
    /// <summary>
    /// Provider identifier (mitid, sbid, nbid)
    /// </summary>
    public required string ProviderId { get; init; }
    
    /// <summary>
    /// Attributes to request from Signicat for this provider
    /// For privacy compliance, should ONLY request "dateOfBirth"
    /// </summary>
    public List<string> RequestedAttributes { get; init; } = new();
    
    /// <summary>
    /// Required Level of Assurance (e.g., "substantial", "high")
    /// </summary>
    public string RequestedLoa { get; init; } = "substantial";
    
    /// <summary>
    /// Optional: custom callback URL for this provider
    /// If not set, uses default from SignicatConfig
    /// </summary>
    public string? CallbackUrl { get; init; }
}
