using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Tokens;
using Moq;
using ValidationService.Application.Interfaces;
using ValidationService.Infrastructure.Jwt;
using Xunit;

namespace ValidationenTest
{
    public class VcJwtValidatorTests
    {
        [Fact]
        public async Task ValidateAsync_WithValidSignature_ShouldReturnTrue()
        {
            // Arrange
            var rsa = RSA.Create(2048);
            var privateKey = new RsaSecurityKey(rsa);
            var publicKey = new RsaSecurityKey(rsa.ExportParameters(false));
            
            var keyResolverMock = new Mock<IDidKeyResolver>();
            keyResolverMock.Setup(k => k.ResolvePublicKeyAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(publicKey);
            
            var validator = new VcJwtValidator(keyResolverMock.Object);
            
            var now = DateTime.UtcNow;
            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Iss, "did:example:issuer"),
                new Claim(JwtRegisteredClaimNames.Sub, "did:example:subject"),
                new Claim(JwtRegisteredClaimNames.Nbf, new DateTimeOffset(now).ToUnixTimeSeconds().ToString()),
                new Claim(JwtRegisteredClaimNames.Exp, new DateTimeOffset(now.AddHours(1)).ToUnixTimeSeconds().ToString()),
                new Claim(JwtRegisteredClaimNames.Iat, new DateTimeOffset(now).ToUnixTimeSeconds().ToString()),
                new Claim("vc", "{}")
            };
            
            var signingCredentials = new SigningCredentials(privateKey, SecurityAlgorithms.RsaSha256);
            var token = new JwtSecurityToken(
                issuer: "did:example:issuer",
                claims: claims,
                notBefore: now,
                expires: now.AddHours(1),
                signingCredentials: signingCredentials
            );
            
            var handler = new JwtSecurityTokenHandler();
            var jwt = handler.WriteToken(token);
            
            // Act
            var (isValid, validatedToken, error) = await validator.ValidateAsync(jwt);
            
            // Assert
            Assert.True(isValid);
            Assert.NotNull(validatedToken);
            Assert.Null(error);
        }
        
        [Fact]
        public async Task ValidateAsync_WithInvalidSignature_ShouldReturnFalse()
        {
            // Arrange
            var rsa1 = RSA.Create(2048);
            var privateKey1 = new RsaSecurityKey(rsa1);
            
            var rsa2 = RSA.Create(2048);
            var publicKey2 = new RsaSecurityKey(rsa2.ExportParameters(false));
            
            var keyResolverMock = new Mock<IDidKeyResolver>();
            keyResolverMock.Setup(k => k.ResolvePublicKeyAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(publicKey2); // Return WRONG public key
            
            var validator = new VcJwtValidator(keyResolverMock.Object);
            
            var now = DateTime.UtcNow;
            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Iss, "did:example:issuer"),
            };
            
            var signingCredentials = new SigningCredentials(privateKey1, SecurityAlgorithms.RsaSha256);
            var token = new JwtSecurityToken(
                issuer: "did:example:issuer",
                claims: claims,
                expires: now.AddHours(1),
                signingCredentials: signingCredentials
            );
            
            var handler = new JwtSecurityTokenHandler();
            var jwt = handler.WriteToken(token);
            
            // Act
            var (isValid, validatedToken, error) = await validator.ValidateAsync(jwt);
            
            // Assert
            Assert.False(isValid);
            Assert.Null(validatedToken);
            Assert.Contains("signature", error.ToLower());
        }
    }
}
