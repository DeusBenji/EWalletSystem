namespace IdentityService.Infrastructure.Configuration;

/// <summary>
/// Configuration for Signicat EID Hub REST API
/// </summary>
public class SignicatConfig
{
    public const string SectionName = "Signicat";
    
    /// <summary>
    /// Base URL for Signicat API (e.g., https://api.signicat.com or preprod host)
    /// </summary>
    public string BaseUrl { get; set; } = null!;
    
    /// <summary>
    /// API Key for authentication (if using API key auth model)
    /// </summary>
    public string? ApiKey { get; set; }
    
    /// <summary>
    /// Client ID for OAuth2 authentication (if using client credentials)
    /// </summary>
    public string? ClientId { get; set; }
    
    /// <summary>
    /// Client Secret for OAuth2 authentication (if using client credentials)
    /// </summary>
    public string? ClientSecret { get; set; }
    
    /// <summary>
    /// Product reference for billing/tracking
    /// </summary>
    public string? ProductReference { get; set; }
    
    /// <summary>
    /// Tenant reference for multi-tenant setups
    /// </summary>
    public string? TenantReference { get; set; }
    
    /// <summary>
    /// Default callback base URL for authentication redirects
    /// </summary>
    public string DefaultCallbackBaseUrl { get; set; } = null!;
    
    /// <summary>
    /// Attributes to request from identity provider (e.g., dateOfBirth, name, nationalId)
    /// </summary>
    public List<string> RequestedAttributes { get; set; } = new() { "dateOfBirth", "name", "nationalId" };
    
    /// <summary>
    /// Allowed identity providers (e.g., mitid, bankid)
    /// </summary>
    public List<string> AllowedProviders { get; set; } = new() { "mitid" };
    
    /// <summary>
    /// Requested Level of Assurance (e.g., substantial, high)
    /// </summary>
    public string RequestedLoa { get; set; } = "substantial";
    
    /// <summary>
    /// Usage reference for billing grouping
    /// </summary>
    public string UsageReference { get; set; } = "ewallet";
    
    /// <summary>
    /// Session lifetime in seconds (default: 600 = 10 minutes)
    /// </summary>
    public int SessionLifetimeSeconds { get; set; } = 600;
    
    /// <summary>
    /// Validate configuration on startup
    /// </summary>
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(BaseUrl))
            throw new InvalidOperationException("Signicat BaseUrl is required");
        
        if (string.IsNullOrWhiteSpace(ApiKey) && 
            (string.IsNullOrWhiteSpace(ClientId) || string.IsNullOrWhiteSpace(ClientSecret)))
            throw new InvalidOperationException("Either ApiKey or ClientId/ClientSecret must be provided");
        
        if (string.IsNullOrWhiteSpace(DefaultCallbackBaseUrl))
            throw new InvalidOperationException("Signicat DefaultCallbackBaseUrl is required");
        
        if (SessionLifetimeSeconds < 60 || SessionLifetimeSeconds > 3600)
            throw new InvalidOperationException("SessionLifetimeSeconds must be between 60 and 3600");
    }
}
