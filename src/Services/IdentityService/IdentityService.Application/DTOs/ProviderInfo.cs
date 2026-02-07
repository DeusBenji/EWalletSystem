using IdentityService.Domain.Models;

namespace IdentityService.Application.DTOs;

public class ProviderInfo
{
    public string ProviderId { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public ProviderCapabilities Capabilities { get; set; } = null!;
}
