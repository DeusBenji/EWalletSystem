namespace IdentityService.Domain.Models;

/// <summary>
/// Capabilities of an identity provider.
/// </summary>
public sealed class ProviderCapabilities
{
    // Canonical provider key: "mitid", "sbid", "nbid"
    public string ProviderId { get; init; } = string.Empty;

    // Human friendly name
    public string DisplayName { get; init; } = string.Empty;

    // "redirect" / "embedded" (avoiding enum dependency issues)
    public string AuthMechanism { get; init; } = "redirect";

    // True if status endpoint/polling gives meaningful results
    public bool SupportsStatusPolling { get; init; } = true;

    // Attributes provider can deliver
    public IReadOnlyCollection<string> SupportedAttributes { get; init; } = Array.Empty<string>();

    // Legacy compatibility properties
    public bool CanProvideAge { get; init; } = true;
    public bool CanProvideDateOfBirth { get; init; } = true;

    // Compatibility constructor for existing code
    public ProviderCapabilities(bool CanProvideAge = true, bool CanProvideDateOfBirth = true)
    {
        this.CanProvideAge = CanProvideAge;
        this.CanProvideDateOfBirth = CanProvideDateOfBirth;
    }
    
    // Default constructor for object initializer
    public ProviderCapabilities() { }
}
