namespace IdentityService.Application.DTOs;

/// <summary>
/// Request to create a new authentication session with Signicat
/// </summary>
public class CreateSessionRequest
{
    /// <summary>
    /// Authentication flow type (default: "redirect")
    /// </summary>
    public string Flow { get; set; } = "redirect";
    
    /// <summary>
    /// Callback URLs for different outcomes
    /// </summary>
    public CallbackUrls CallbackUrls { get; set; } = null!;
    
    /// <summary>
    /// Attributes to request from the identity provider
    /// </summary>
    public List<string> RequestedAttributes { get; set; } = new();
    
    /// <summary>
    /// Allowed identity providers (e.g., "mitid", "bankid")
    /// </summary>
    public List<string>? AllowedProviders { get; set; }
    
    /// <summary>
    /// Requested Level of Assurance (e.g., "substantial", "high")
    /// </summary>
    public string? RequestedLoa { get; set; }
    
    /// <summary>
    /// Usage reference for billing/tracking
    /// </summary>
    public string? UsageReference { get; set; }
    
    /// <summary>
    /// Session lifetime in seconds
    /// </summary>
    public int? SessionLifetime { get; set; }
    
    /// <summary>
    /// External reference for correlation (GUID)
    /// </summary>
    public string? ExternalReference { get; set; }
}

/// <summary>
/// Callback URLs for authentication session
/// </summary>
public class CallbackUrls
{
    /// <summary>
    /// URL to redirect to on successful authentication
    /// </summary>
    public string Success { get; set; } = null!;
    
    /// <summary>
    /// URL to redirect to if user aborts authentication
    /// </summary>
    public string Abort { get; set; } = null!;
    
    /// <summary>
    /// URL to redirect to on authentication error
    /// </summary>
    public string Error { get; set; } = null!;
}
