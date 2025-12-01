using Microsoft.IdentityModel.Tokens;

namespace TokenService.Application.Interfaces
{
    public interface IKeyProvider
    {
        RsaSecurityKey GetPrivateKey();
        RsaSecurityKey GetPublicKey();
    }
}
