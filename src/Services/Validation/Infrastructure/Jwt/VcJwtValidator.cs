using System;
using System.IdentityModel.Tokens.Jwt;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Tokens;
using ValidationService.Application.Interfaces;

namespace ValidationService.Infrastructure.Jwt
{
    public class VcJwtValidator : IJwtValidator
    {
        private readonly IDidKeyResolver _keyResolver;
        private readonly JwtSecurityTokenHandler _tokenHandler;
        
        public VcJwtValidator(IDidKeyResolver keyResolver)
        {
            _keyResolver = keyResolver;
            _tokenHandler = new JwtSecurityTokenHandler();
        }
        
        public async Task<(bool IsValid, JwtSecurityToken? Token, string? Error)> ValidateAsync(
            string jwt, 
            CancellationToken ct = default)
        {
            try
            {
                // First parse without validation to get issuer
                if (!_tokenHandler.CanReadToken(jwt))
                    return (false, null, "Invalid JWT format");

                var token = _tokenHandler.ReadJwtToken(jwt);
                var issuerDid = token.Issuer;
                
                if (string.IsNullOrEmpty(issuerDid))
                    return (false, null, "Missing issuer");
                
                // Resolve public key from DID
                var publicKey = await _keyResolver.ResolvePublicKeyAsync(issuerDid, ct);
                if (publicKey == null)
                    return (false, null, "Could not resolve issuer public key");
                
                // Validate with public key
                var validationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = issuerDid,
                    ValidateAudience = false, // VC typically doesn't have audience
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = publicKey,
                    ClockSkew = TimeSpan.FromSeconds(5) // Strict clock skew
                };
                
                _tokenHandler.ValidateToken(jwt, validationParameters, out var validatedToken);
                
                return (true, validatedToken as JwtSecurityToken, null);
            }
            catch (SecurityTokenExpiredException)
            {
                return (false, null, "Token expired");
            }
            catch (SecurityTokenSignatureKeyNotFoundException)
            {
                return (false, null, "Invalid signature key");
            }
            catch (SecurityTokenInvalidSignatureException)
            {
                return (false, null, "Invalid signature");
            }
            catch (Exception ex)
            {
                return (false, null, $"Validation error: {ex.Message}");
            }
        }
    }
}
