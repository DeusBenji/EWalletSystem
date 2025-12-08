using Application.BusinessLogic;
using Application.DTOs;
using Application.Interfaces;
using BuildingBlocks.Contracts.Events;
using BuildingBlocks.Contracts.Messaging;
using Domain.Models;
using Domain.Repositories;
using Moq;
using System;
using System.Threading;
using System.Threading.Tasks;
using TokenService.Application.Interfaces;
using TokenService.Domain.Models;
using Xunit;

namespace TokenSerrvice.Application
{
    public class TokenIssuanceServiceTests
    {
        private readonly Mock<IAccountAgeStatusRepository> _ageStatusRepoMock = new();
        private readonly Mock<IAccountAgeStatusCache> _ageStatusCacheMock = new();
        private readonly Mock<IAttestationRepository> _attestationRepoMock = new();
        private readonly Mock<IFabricAnchorClient> _fabricMock = new();
        private readonly Mock<ITokenHashCalculator> _hashCalculatorMock = new();
        private readonly Mock<IKafkaProducer> _eventProducerMock = new();
        private readonly Mock<IVcSigningService> _vcSigningMock = new();

        private TokenIssuanceService CreateSut()
        {
            return new TokenIssuanceService(
                _ageStatusRepoMock.Object,
                _ageStatusCacheMock.Object,
                _attestationRepoMock.Object,
                _fabricMock.Object,
                _hashCalculatorMock.Object,
                _eventProducerMock.Object,
                _vcSigningMock.Object);
        }

        [Fact]
        public async Task IssueTokenAsync_WhenAccountIsAdult_EmitsTokenAndPublishesEvent()
        {
            // Arrange
            var accountId = Guid.NewGuid();
            var dto = new IssueTokenDto { AccountId = accountId };

            var ageStatus = new AccountAgeStatus(
                accountId,
                isAdult: true,
                verifiedAt: DateTime.UtcNow);

            _ageStatusCacheMock
                .Setup(x => x.GetAsync(accountId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(ageStatus);

            _hashCalculatorMock
                .Setup(x => x.ComputeHash(It.IsAny<string>()))
                .Returns("dummy-hash");

            AgeAttestation? savedAttestation = null;

            _attestationRepoMock
                .Setup(x => x.SaveAsync(It.IsAny<AgeAttestation>(), It.IsAny<CancellationToken>()))
                .Callback<AgeAttestation, CancellationToken>((att, _) => savedAttestation = att)
                .Returns(Task.CompletedTask);

            string? publishedTopic = null;
            string? publishedKey = null;
            TokenIssued? publishedEvent = null;

            _eventProducerMock
                .Setup(x => x.PublishAsync(
                    Topics.TokenIssued,
                    It.IsAny<string>(),
                    It.IsAny<TokenIssued>(),
                    It.IsAny<CancellationToken>()))
                .Callback<string, string, TokenIssued, CancellationToken>((topic, key, @event, _) =>
                {
                    publishedTopic = topic;
                    publishedKey = key;
                    publishedEvent = @event;
                })
                .Returns(Task.CompletedTask);

            _fabricMock
                .Setup(x => x.AnchorHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            _vcSigningMock.Setup(x => x.GetIssuerDid()).Returns("did:example:issuer");
            _vcSigningMock
                .Setup(x => x.CreateSignedVcJwt(It.IsAny<AgeOver18Credential>()))
                .Returns("dummy-vc-jwt");

            var sut = CreateSut();

            // Act
            var result = await sut.IssueTokenAsync(dto, CancellationToken.None);

            // Assert – basic token-egenskaber
            Assert.NotNull(result);
            Assert.False(string.IsNullOrWhiteSpace(result.Token));
            Assert.True(result.ExpiresAt > result.IssuedAt);

            // Hash beregnet
            _hashCalculatorMock.Verify(
                x => x.ComputeHash(It.IsAny<string>()),
                Times.Once);

            // Hash forankret i Fabric
            _fabricMock.Verify(
                x => x.AnchorHashAsync("dummy-hash", It.IsAny<CancellationToken>()),
                Times.Once);

            // Attestation gemt
            _attestationRepoMock.Verify(
                x => x.SaveAsync(It.IsAny<AgeAttestation>(), It.IsAny<CancellationToken>()),
                Times.Once);

            Assert.NotNull(savedAttestation);
            Assert.Equal(accountId, savedAttestation!.AccountId);
            Assert.True(savedAttestation.IsAdult);
            Assert.Equal("dummy-hash", savedAttestation.Hash);

            // Event sendt på korrekt topic med korrekt data
            _eventProducerMock.Verify(
                x => x.PublishAsync(
                    Topics.TokenIssued,
                    It.IsAny<string>(),
                    It.IsAny<TokenIssued>(),
                    It.IsAny<CancellationToken>()),
                Times.Once);

            Assert.Equal(Topics.TokenIssued, publishedTopic);
            Assert.NotNull(publishedEvent);
            Assert.Equal(savedAttestation.Id, publishedEvent!.AttestationId);
            Assert.Equal(savedAttestation.AccountId, publishedEvent.AccountId);
            Assert.Equal(savedAttestation.Hash, publishedEvent.AttestationHash);
        }

        [Fact]
        public async Task IssueTokenAsync_WhenAccountIsNotAdult_ThrowsAndDoesNotEmit()
        {
            // Arrange
            var accountId = Guid.NewGuid();
            var dto = new IssueTokenDto { AccountId = accountId };

            var ageStatus = new AccountAgeStatus(
                accountId,
                isAdult: false,
                verifiedAt: DateTime.UtcNow);

            _ageStatusCacheMock
                .Setup(x => x.GetAsync(accountId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(ageStatus);

            var sut = CreateSut();

            // Act
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(
                () => sut.IssueTokenAsync(dto, CancellationToken.None));

            // Assert
            Assert.Equal("Account is not verified as 18+.", ex.Message);

            _fabricMock.Verify(
                x => x.AnchorHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
                Times.Never);

            _attestationRepoMock.Verify(
                x => x.SaveAsync(It.IsAny<AgeAttestation>(), It.IsAny<CancellationToken>()),
                Times.Never);

            _eventProducerMock.Verify(
                x => x.PublishAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<TokenIssued>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task IssueTokenAsync_WhenRepoFails_ThrowsException()
        {
            // Arrange
            var accountId = Guid.NewGuid();
            var dto = new IssueTokenDto { AccountId = accountId };

            var ageStatus = new AccountAgeStatus(accountId, isAdult: true, verifiedAt: DateTime.UtcNow);

            _ageStatusCacheMock
                .Setup(x => x.GetAsync(accountId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(ageStatus);

            _hashCalculatorMock
                .Setup(x => x.ComputeHash(It.IsAny<string>()))
                .Returns("dummy-hash");

            _vcSigningMock.Setup(x => x.GetIssuerDid()).Returns("did:example:issuer");
            _vcSigningMock
                .Setup(x => x.CreateSignedVcJwt(It.IsAny<AgeOver18Credential>()))
                .Returns("dummy-vc-jwt");

            _attestationRepoMock
                .Setup(x => x.SaveAsync(It.IsAny<AgeAttestation>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("Database failure"));

            var sut = CreateSut();

            // Act & Assert
            var ex = await Assert.ThrowsAsync<Exception>(
                () => sut.IssueTokenAsync(dto, CancellationToken.None));

            Assert.Equal("Database failure", ex.Message);

            _eventProducerMock.Verify(
                x => x.PublishAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<TokenIssued>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task IssueTokenAsync_WhenKafkaFails_ThrowsException()
        {
            // Arrange
            var accountId = Guid.NewGuid();
            var dto = new IssueTokenDto { AccountId = accountId };

            var ageStatus = new AccountAgeStatus(accountId, isAdult: true, verifiedAt: DateTime.UtcNow);

            _ageStatusCacheMock
                .Setup(x => x.GetAsync(accountId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(ageStatus);

            _hashCalculatorMock
                .Setup(x => x.ComputeHash(It.IsAny<string>()))
                .Returns("dummy-hash");

            _vcSigningMock.Setup(x => x.GetIssuerDid()).Returns("did:example:issuer");
            _vcSigningMock
                .Setup(x => x.CreateSignedVcJwt(It.IsAny<AgeOver18Credential>()))
                .Returns("dummy-vc-jwt");

            _attestationRepoMock
                .Setup(x => x.SaveAsync(It.IsAny<AgeAttestation>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            _eventProducerMock
                .Setup(x => x.PublishAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<TokenIssued>(),
                    It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("Kafka failure"));

            var sut = CreateSut();

            // Act & Assert
            var ex = await Assert.ThrowsAsync<Exception>(
                () => sut.IssueTokenAsync(dto, CancellationToken.None));

            Assert.Equal("Kafka failure", ex.Message);
        }
    }
}
