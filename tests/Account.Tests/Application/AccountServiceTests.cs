using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Moq;
using AutoMapper;
using Microsoft.Extensions.Logging;
using Application.BusinessLogic;
using Application.DTOs;
using Application.Interfaces;
using Domain.Models;
using Domain.Repositories;
using BuildingBlocks.Contracts.Events;
using BuildingBlocks.Contracts.Messaging;

namespace Account.Tests.Application
{
    public class AccountServiceTests
    {
        private readonly Mock<IAccountRepository> _repoMock;
        private readonly Mock<IPasswordHasher> _hasherMock;
        private readonly Mock<BuildingBlocks.Contracts.Messaging.IKafkaProducer> _kafkaMock;
        private readonly Mock<IAccountCache> _cacheMock;
        private readonly Mock<IMapper> _mapperMock;
        private readonly Mock<ILogger<global::Application.BusinessLogic.AccountService>> _loggerMock;
        private readonly global::Application.BusinessLogic.AccountService _sut;

        public AccountServiceTests()
        {
            _repoMock = new Mock<IAccountRepository>();
            _hasherMock = new Mock<IPasswordHasher>();
            _kafkaMock = new Mock<BuildingBlocks.Contracts.Messaging.IKafkaProducer>();
            _cacheMock = new Mock<IAccountCache>();
            _mapperMock = new Mock<IMapper>();
            _loggerMock = new Mock<ILogger<global::Application.BusinessLogic.AccountService>>();

            _sut = new global::Application.BusinessLogic.AccountService(
                _repoMock.Object,
                _hasherMock.Object,
                new Infrastructure.Kafka.AccountCreatedProducer(_kafkaMock.Object),
                _cacheMock.Object,
                _mapperMock.Object,
                _loggerMock.Object);
        }

        [Fact]
        public async Task RegisterAccountAsync_HappyPath_CreatesAccountAndPublishesEvent()
        {
            // Arrange
            var dto = new RegisterAccountDto { Email = "test@example.com", Password = "password123" };
            var hashedPassword = "hashed_password";

            _repoMock.Setup(r => r.EmailExistsAsync(dto.Email, It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            _hasherMock.Setup(h => h.HashPassword(dto.Password)).Returns(hashedPassword);
            
            _mapperMock.Setup(m => m.Map<AccountDto>(It.IsAny<Domain.Models.Account>()))
                .Returns((Domain.Models.Account a) => new AccountDto { Id = a.Id, Email = a.Email });

            // Act
            var result = await _sut.RegisterAccountAsync(dto, CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(dto.Email, result.Email);
            Assert.NotEqual(Guid.Empty, result.Id);

            _repoMock.Verify(r => r.CreateAsync(It.Is<Domain.Models.Account>(a => 
                a.Email == dto.Email && 
                a.PasswordHash == hashedPassword), It.IsAny<CancellationToken>()), Times.Once);

            _kafkaMock.Verify(k => k.PublishAsync(
                Topics.AccountCreated, 
                It.Is<AccountCreated>(e => e.AccountId == result.Id && e.Email == result.Email), 
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task RegisterAccountAsync_WhenEmailExists_ThrowsInvalidOperationException()
        {
            // Arrange
            var dto = new RegisterAccountDto { Email = "existing@example.com", Password = "password123" };
            var existingAccount = Domain.Models.Account.Reconstruct(Guid.NewGuid(), dto.Email, "hash");

            _repoMock.Setup(r => r.EmailExistsAsync(dto.Email, It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => 
                _sut.RegisterAccountAsync(dto, CancellationToken.None));

            _repoMock.Verify(r => r.CreateAsync(It.IsAny<Domain.Models.Account>(), It.IsAny<CancellationToken>()), Times.Never);
            _kafkaMock.Verify(k => k.PublishAsync(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task AuthenticateAsync_HappyPath_ReturnsSuccess()
        {
            // Arrange
            var email = "test@example.com";
            var password = "password123";
            var account = Domain.Models.Account.Reconstruct(Guid.NewGuid(), email, "hashed_password");

            _repoMock.Setup(r => r.GetByEmailAsync(email, It.IsAny<CancellationToken>()))
                .ReturnsAsync(account);

            _hasherMock.Setup(h => h.Verify(password, account.PasswordHash)).Returns(true);

            // Act
            var result = await _sut.AuthenticateAsync(email, password, CancellationToken.None);

            // Assert
            Assert.True(result.Success);
            Assert.Equal(account.Id, result.AccountId);
            Assert.Null(result.Failure);
        }

        [Fact]
        public async Task AuthenticateAsync_WhenAccountNotFound_ReturnsFailure()
        {
            // Arrange
            var email = "unknown@example.com";
            var password = "password123";

            _repoMock.Setup(r => r.GetByEmailAsync(email, It.IsAny<CancellationToken>()))
                .ReturnsAsync((Domain.Models.Account?)null);

            // Act
            var result = await _sut.AuthenticateAsync(email, password, CancellationToken.None);

            // Assert
            Assert.False(result.Success);
            Assert.Null(result.AccountId);
            Assert.Equal("Invalid credentials", result.Failure);
        }

        [Fact]
        public async Task AuthenticateAsync_WhenPasswordInvalid_ReturnsFailure()
        {
            // Arrange
            var email = "test@example.com";
            var password = "wrong_password";
            var account = Domain.Models.Account.Reconstruct(Guid.NewGuid(), email, "hashed_password");

            _repoMock.Setup(r => r.GetByEmailAsync(email, It.IsAny<CancellationToken>()))
                .ReturnsAsync(account);

            _hasherMock.Setup(h => h.Verify(password, account.PasswordHash)).Returns(false);

            // Act
            var result = await _sut.AuthenticateAsync(email, password, CancellationToken.None);

            // Assert
            Assert.False(result.Success);
            Assert.Null(result.AccountId);
            Assert.Equal("Invalid credentials", result.Failure);
        }

        [Fact]
        public async Task GetAccountByIdAsync_HappyPath_ReturnsAccount()
        {
            // Arrange
            var id = Guid.NewGuid();
            var account = Domain.Models.Account.Reconstruct(id, "test@example.com", "hash");

            _repoMock.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(account);

            _cacheMock.Setup(c => c.GetAccountAsync(id, It.IsAny<CancellationToken>()))
                .ReturnsAsync((AccountDto?)null);

            _mapperMock.Setup(m => m.Map<AccountDto>(It.IsAny<Domain.Models.Account>()))
                .Returns((Domain.Models.Account a) => new AccountDto { Id = a.Id, Email = a.Email });

            // Act
            var result = await _sut.GetAccountByIdAsync(id, CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(account.Id, result.Id);
            Assert.Equal(account.Email, result.Email);
        }

        [Fact]
        public async Task GetAccountByIdAsync_WhenNotFound_ReturnsNull()
        {
            // Arrange
            var id = Guid.NewGuid();

            _repoMock.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
                .ReturnsAsync((Domain.Models.Account?)null);

            // Act
            var result = await _sut.GetAccountByIdAsync(id, CancellationToken.None);

            // Assert
            Assert.Null(result);
        }
    }
}
