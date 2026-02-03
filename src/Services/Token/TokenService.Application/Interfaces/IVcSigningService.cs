using TokenService.Domain.Models;

namespace TokenService.Application.Interfaces
{
    public interface IVcSigningService
    {
        string CreateSignedVcJwt<T>(T credential, string subjectId, DateTimeOffset expirationDate);
        string GetIssuerDid();
    }
}
