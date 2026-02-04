namespace BachMitID.Domain.Models;

/// <summary>
/// Identity data returned by an identity provider.
/// Contains only the data that the provider can and did provide.
/// </summary>
public class IdentityData
{
    /// <summary>
    /// Provider that issued this identity data
    /// </summary>
    public string ProviderId { get; init; } = string.Empty;
    
    /// <summary>
    /// Unique subject identifier from the provider
    /// </summary>
    public string Subject { get; init; } = string.Empty;
    
    /// <summary>
    /// Date of birth (if provider can provide it)
    /// </summary>
    public DateTime? DateOfBirth { get; init; }
    
    /// <summary>
    /// Full name (if provider can provide it)
    /// </summary>
    public string? Name { get; init; }
    
    /// <summary>
    /// National ID number (CPR, SSN, etc.) (if provider can provide it)
    /// </summary>
    public string? NationalId { get; init; }
    
    /// <summary>
    /// Email address (if provider can provide it)
    /// </summary>
    public string? Email { get; init; }
    
    /// <summary>
    /// Phone number (if provider can provide it)
    /// </summary>
    public string? Phone { get; init; }
    
    /// <summary>
    /// Physical address (if provider can provide it)
    /// </summary>
    public string? Address { get; init; }
    
    /// <summary>
    /// Additional custom claims from the provider
    /// </summary>
    public Dictionary<string, string> CustomClaims { get; init; } = new();
}
