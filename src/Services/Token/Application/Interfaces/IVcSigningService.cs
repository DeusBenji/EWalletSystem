using TokenService.Domain.Models;

namespace TokenService.Application.Interfaces
{
    public interface IVcSigningService
    {
        string CreateSignedVcJwt(AgeOver18Credential credential);
        string GetIssuerDid();
    }
}
