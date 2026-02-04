namespace BachMitID.Domain.Models;

/// <summary>
/// Describes what data an identity provider can provide.
/// Used for capability-based provider selection.
/// </summary>
public class ProviderCapabilities
{
    /// <summary>
    /// Can provider give date of birth or age?
    /// </summary>
    public bool CanProvideAge { get; init; }
    
    /// <summary>
    /// Can provider give full name?
    /// </summary>
    public bool CanProvideName { get; init; }
    
    /// <summary>
    /// Can provider give physical address?
    /// </summary>
    public bool CanProvideAddress { get; init; }
    
    /// <summary>
    /// Can provider give national ID (CPR, SSN, etc.)?
    /// </summary>
    public bool CanProvideNationalId { get; init; }
    
    /// <summary>
    /// Can provider give email address?
    /// </summary>
    public bool CanProvideEmail { get; init; }
    
    /// <summary>
    /// Can provider give phone number?
    /// </summary>
    public bool CanProvidePhone { get; init; }
}
