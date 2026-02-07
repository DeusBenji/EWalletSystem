namespace IdentityService.Domain.Models;

/// <summary>
/// Legacy capabilities model to satisfy IIdentityProvider contract.
/// </summary>
public sealed record ProviderCapabilities(
    bool CanProvideAge = true,
    bool CanProvideDateOfBirth = true
);
