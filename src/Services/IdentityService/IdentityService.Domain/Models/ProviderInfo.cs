namespace IdentityService.Domain.Models;

/// <summary>
/// Information about an available identity provider.
/// Used for provider discovery and selection.
/// </summary>
public class ProviderInfo
{
    public string ProviderId { get; init; } = string.Empty;
    public string Country { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public ProviderCapabilities Capabilities { get; init; } = new();
}
