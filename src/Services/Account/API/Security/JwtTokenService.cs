using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace AccountService.API.Security;

public class JwtTokenService
{
    private readonly IConfiguration _config;

    public JwtTokenService(IConfiguration config)
    {
        _config = config;
    }

    public string CreateToken(Guid accountId)
    {
        var issuer = _config["Jwt:Issuer"] ?? throw new InvalidOperationException("Missing Jwt:Issuer");
        var audience = _config["Jwt:Audience"] ?? throw new InvalidOperationException("Missing Jwt:Audience");
        var key = _config["Jwt:Key"] ?? throw new InvalidOperationException("Missing Jwt:Key");

        var claims = new List<Claim>
        {
            new Claim("accountId", accountId.ToString()),
            new Claim(JwtRegisteredClaimNames.Sub, accountId.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
        var creds = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
