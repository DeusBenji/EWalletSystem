using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using TokenService.Application.Interfaces;
using TokenService.Domain.Models;

namespace TokenService.Infrastructure.Signing
{
    public class VcSigningService : IVcSigningService
    {
        private readonly string _issuerDid;
        private readonly RsaSecurityKey _privateKey;

        public VcSigningService(IConfiguration config, IKeyProvider keyProvider)
        {
            _issuerDid = config["VcSigning:IssuerDid"]
                ?? throw new InvalidOperationException("IssuerDid not configured");
            _privateKey = keyProvider.GetPrivateKey();
        }

        public string CreateSignedVcJwt(AgeOver18Credential credential)
        {
            var now = DateTime.UtcNow;

            // Build claims
            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Iss, _issuerDid),
                new Claim(JwtRegisteredClaimNames.Sub, credential.CredentialSubject.Id),
                new Claim(JwtRegisteredClaimNames.Nbf, new DateTimeOffset(now).ToUnixTimeSeconds().ToString()),
                new Claim(JwtRegisteredClaimNames.Exp, new DateTimeOffset(credential.ExpirationDate).ToUnixTimeSeconds().ToString()),
                new Claim(JwtRegisteredClaimNames.Iat, new DateTimeOffset(now).ToUnixTimeSeconds().ToString()),
                new Claim("vc", JsonSerializer.Serialize(credential))
            };

            // Create signing credentials
            var signingCredentials = new SigningCredentials(_privateKey, SecurityAlgorithms.RsaSha256);

            // Create token
            var token = new JwtSecurityToken(
                issuer: _issuerDid,
                claims: claims,
                notBefore: now,
                expires: credential.ExpirationDate,
                signingCredentials: signingCredentials
            );

            var handler = new JwtSecurityTokenHandler();
            return handler.WriteToken(token);
        }

        public string GetIssuerDid() => _issuerDid;
    }
}
