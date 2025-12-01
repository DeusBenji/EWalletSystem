using System.IdentityModel.Tokens.Jwt;

namespace ValidationService.Application.Interfaces
{
    public interface IJwtValidator
    {
        Task<(bool IsValid, JwtSecurityToken? Token, string? Error)> ValidateAsync(
            string jwt, 
            CancellationToken ct = default);
    }
}
