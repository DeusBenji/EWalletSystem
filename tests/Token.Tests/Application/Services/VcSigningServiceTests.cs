using System;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.Extensions.Configuration;
using Moq;
using TokenService.Application.Interfaces;
using TokenService.Domain.Models;
using TokenService.Infrastructure.Signing;
using Xunit;

namespace TokenSerrviceTest
{
    public class VcSigningServiceTests
    {
        [Fact]
        public void CreateSignedVcJwt_ShouldReturnValidJwt()
        {
            // Arrange
            var configMock = new Mock<IConfiguration>();
            configMock.Setup(c => c["VcSigning:IssuerDid"]).Returns("did:example:issuer");
            
            var keyProviderMock = new Mock<IKeyProvider>();
            // We need a real key for signing to work
            var rsa = System.Security.Cryptography.RSA.Create(2048);
            var privateKey = new Microsoft.IdentityModel.Tokens.RsaSecurityKey(rsa);
            keyProviderMock.Setup(k => k.GetPrivateKey()).Returns(privateKey);
            
            var service = new VcSigningService(configMock.Object, keyProviderMock.Object);
            
            var credential = new AgeOver18Credential
            {
                Issuer = "did:example:issuer",
                IssuanceDate = DateTime.UtcNow,
                ExpirationDate = DateTime.UtcNow.AddHours(1),
                CredentialSubject = new CredentialSubject
                {
                    Id = "did:example:subject",
                    AgeOver18 = true
                }
            };
            
            // Act
            var jwt = service.CreateSignedVcJwt(credential);
            
            // Assert
            Assert.NotNull(jwt);
            Assert.Contains(".", jwt); // JWT has dots
            
            // Parse and verify structure
            var handler = new JwtSecurityTokenHandler();
            var token = handler.ReadJwtToken(jwt);
            
            Assert.Equal("did:example:issuer", token.Issuer);
            Assert.Equal("did:example:subject", token.Subject);
            Assert.True(token.Payload.ContainsKey("vc"));
        }
    }
}
