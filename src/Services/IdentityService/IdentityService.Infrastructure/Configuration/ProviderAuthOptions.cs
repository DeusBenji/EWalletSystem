using System.Collections.Generic;

namespace IdentityService.Infrastructure.Configuration;

public class ProviderAuthOptions
{
    public string ProviderId { get; set; } = string.Empty;
    public List<string> RequestedAttributes { get; set; } = new();
    public string RequestedLoa { get; set; } = "substantial";
    public string GlobalCallbackUrl { get; set; } = string.Empty;
}
