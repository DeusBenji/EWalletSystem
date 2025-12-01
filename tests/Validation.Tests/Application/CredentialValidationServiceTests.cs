using Application.BusinessLogic;
using Application.Interfaces;
using ValidationService.Application.Interfaces;
using Domain.Models;
using Domain.Repositories;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Threading.Tasks;
using Application.DTOs;
using Xunit;

namespace ValidationenTest.Application
{
    public class CredentialValidationServiceTests
    {
        private readonly Mock<IFabricLookupClient> _fabric;
        private readonly Mock<ICacheService> _cache;
        private readonly Mock<IVerificationLogRepository> _logRepo;
        private readonly Mock<IKafkaEventProducer> _kafka;
        private readonly Mock<ICredentialFingerprintService> _fingerprint;
        private readonly Mock<ICredentialClaimParser> _claimParser;
        private readonly Mock<ILogger<CredentialValidationService>> _logger;
        private readonly Mock<IJwtValidator> _jwtValidator;

        private readonly CredentialValidationService _sut;

        public CredentialValidationServiceTests()
        {
            _fabric = new Mock<IFabricLookupClient>();
            _cache = new Mock<ICacheService>();
            _logRepo = new Mock<IVerificationLogRepository>();
            _kafka = new Mock<IKafkaEventProducer>();
            _fingerprint = new Mock<ICredentialFingerprintService>();
            _claimParser = new Mock<ICredentialClaimParser>();
            _logger = new Mock<ILogger<CredentialValidationService>>();
            _jwtValidator = new Mock<IJwtValidator>();

            // default fingerprint-opførsel, så constructor-logikken ikke fejler
            _fingerprint
                .Setup(x => x.Hash(It.IsAny<string>()))
                .Returns("hash-123");

            _sut = new CredentialValidationService(
                _fabric.Object,
                _cache.Object,
                _logRepo.Object,
                _kafka.Object,
                _fingerprint.Object,
                _claimParser.Object,
                _logger.Object,
                _jwtValidator.Object);
        }

        [Fact]
        public async Task VerifyAsync_Throws_When_VcJwt_Is_Empty()
        {
            // Arrange
            var dto = new VerifyCredentialDto { VcJwt = "   " };

            // Act
            var ex = await Assert.ThrowsAsync<ArgumentException>(() => _sut.VerifyAsync(dto));

            // Assert
            Assert.Equal("VcJwt", ex.ParamName);
            Assert.StartsWith("VC JWT must be provided", ex.Message);
        }


        [Fact]
        public async Task VerifyAsync_ReturnsInvalid_When_JwtValidator_Fails()
        {
            // Arrange
            var dto = new VerifyCredentialDto { VcJwt = "invalid-jwt" };
            
            _jwtValidator.Setup(x => x.ValidateAsync(dto.VcJwt, It.IsAny<CancellationToken>()))
                .ReturnsAsync((false, null, "Invalid VC JWT format"));

            // Act
            var result = await _sut.VerifyAsync(dto);

            // Assert
            Assert.False(result.IsValid);
            Assert.Equal("Invalid VC JWT format", result.FailureReason);
            Assert.Null(result.IssuerDid);

            _logRepo.Verify(
                x => x.InsertAsync(
                    It.IsAny<VerificationLog>(),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }


        /*
        [Fact]
        public async Task VerifyAsync_HappyPath_ResolvesDid_And_PublishesKafka()
        {
            // Arrange
            var issuerDid = "did:example:happy";
            var dto = new VerifyCredentialDto { VcJwt = "valid-jwt" };
            var accountId = Guid.NewGuid();

            // Create a token with the VC claim
            var vcJson = "{\"Type\":[\"VerifiableCredential\",\"AgeOver18Credential\"],\"CredentialSubject\":{\"AgeOver18\":true}}";
            var token = new JwtSecurityToken(
                issuer: issuerDid,
                claims: new[] 
                { 
                    new Claim("vc", vcJson) 
                });

            _jwtValidator.Setup(x => x.ValidateAsync(dto.VcJwt, It.IsAny<CancellationToken>()))
                .ReturnsAsync((true, token, null));

            _claimParser
                .Setup(x => x.ExtractAccountId(It.IsAny<JwtSecurityToken>()))
                .Returns(accountId);

            // Ensure fingerprint hash works
            _fingerprint.Setup(x => x.Hash(It.IsAny<string>())).Returns("hash-123");

            // Act
            var result = await _sut.VerifyAsync(dto);

            // Assert
            Assert.True(result.IsValid, $"Validation failed: {result.FailureReason}");
            Assert.Null(result.FailureReason);
            Assert.Equal(issuerDid, result.IssuerDid);

            _logRepo.Verify(
                x => x.InsertAsync(
                    It.Is<VerificationLog>(l => l.IsValid == true && l.VcJwtHash == "hash-123"),
                    It.IsAny<CancellationToken>()),
                Times.Once);

            _kafka.Verify(
                x => x.PublishAsync(
                    "credential-verified",
                    accountId.ToString(),
                    It.IsAny<string>()),
                Times.Once);
        }
        */
        [Fact]
        public async Task VerifyAsync_ShouldNotCallFabricDirectly()
        {
            // Arrange
            var dto = new VerifyCredentialDto { VcJwt = "valid-jwt" };
            var issuerDid = "did:example:issuer";
            var token = new JwtSecurityToken(issuer: issuerDid, claims: new[] { new Claim("vc", "{\"Type\":[\"VerifiableCredential\",\"AgeOver18Credential\"],\"CredentialSubject\":{\"AgeOver18\":true}}") });

            _jwtValidator.Setup(x => x.ValidateAsync(dto.VcJwt, It.IsAny<CancellationToken>()))
                .ReturnsAsync((true, token, null));

            // Act
            await _sut.VerifyAsync(dto);

            // Assert
            _fabric.Verify(x => x.ResolveDidAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        /*
        [Fact]
        public async Task VerifyAsync_WhenLogRepoFails_ThrowsException()
        {
            // Arrange
            var dto = new VerifyCredentialDto { VcJwt = "valid-jwt" };
            _jwtValidator.Setup(x => x.ValidateAsync(dto.VcJwt, It.IsAny<CancellationToken>()))
                .ReturnsAsync((false, null, "Invalid JWT"));

            _logRepo.Setup(x => x.InsertAsync(It.IsAny<VerificationLog>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("Database error"));

            // Act & Assert
            var ex = await Assert.ThrowsAsync<Exception>(() => _sut.VerifyAsync(dto));
            Assert.Equal("Database error", ex.Message);
        }
        */
    }
}
