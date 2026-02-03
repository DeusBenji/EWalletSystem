using Microsoft.IdentityModel.Tokens;

namespace ValidationService.Application.Interfaces
{
    public interface IDidKeyResolver
    {
        Task<SecurityKey?> ResolvePublicKeyAsync(string issuerDid, CancellationToken ct = default);
    }
}
