namespace IdentityService.Application.DTOs;

/// <summary>
/// Response from creating a new authentication session
/// </summary>
public class CreateSessionResponse
{
    /// <summary>
    /// Unique session identifier
    /// </summary>
    public string Id { get; set; } = null!;
    
    /// <summary>
    /// URL to redirect user to for authentication
    /// </summary>
    public string AuthenticationUrl { get; set; } = null!;
    
    /// <summary>
    /// When the session expires
    /// </summary>
    public DateTime ExpiresAt { get; set; }
}
