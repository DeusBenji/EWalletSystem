using AutoMapper;
using AccountService.API.Contracts;
using AccountService.API.Controllers;
using Application.DTOs;
using Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace AccountSerrvicesTest.API
{
    public class AccountControllerTests
    {
        private readonly Mock<IAccountService> _serviceMock;
        private readonly Mock<ILogger<AccountController>> _loggerMock;
        private readonly Mock<IMapper> _mapperMock;
        private readonly AccountController _sut;

        public AccountControllerTests()
        {
            _serviceMock = new Mock<IAccountService>();
            _loggerMock = new Mock<ILogger<AccountController>>();
            _mapperMock = new Mock<IMapper>();

            // --- Mapper-setup: API <-> Application DTOs ---

            // API -> Application (Register)
            _mapperMock
                .Setup(m => m.Map<RegisterAccountDto>(It.IsAny<AccountRegisterRequest>()))
                .Returns((AccountRegisterRequest r) => new RegisterAccountDto
                {
                    Email = r.Email,
                    Password = r.Password
                });

            // Application -> API (AccountDto -> AccountResponse)
            _mapperMock
                .Setup(m => m.Map<AccountResponse>(It.IsAny<AccountDto>()))
                .Returns((AccountDto d) => new AccountResponse
                {
                    Id = d.Id,
                    Email = d.Email
                });

            // Application -> API (AccountDto -> AccountStatusResponse)
            _mapperMock
                .Setup(m => m.Map<AccountStatusResponse>(It.IsAny<AccountDto>()))
                .Returns((AccountDto d) => new AccountStatusResponse
                {
                    IsAdult = d.IsAdult,
                    IsMitIdLinked = d.IsMitIdLinked
                });

            _sut = new AccountController(
                _serviceMock.Object,
                _loggerMock.Object,
                _mapperMock.Object);
        }

        // Hjælpe-metode til at læse properties på anonyme typer
        private static TProp GetAnonymousProperty<TProp>(object obj, string name)
        {
            Assert.NotNull(obj);
            var type = obj.GetType();
            var prop = type.GetProperty(name);
            Assert.NotNull(prop);

            var value = prop!.GetValue(obj);
            return Assert.IsType<TProp>(value);
        }

        [Fact]
        public async Task Register_ReturnsCreated_WhenSuccessful()
        {
            // Arrange
            var request = new AccountRegisterRequest
            {
                Email = "test@example.com",
                Password = "Secret123!"
            };

            var registeredAccount = new AccountDto
            {
                Id = Guid.NewGuid(),
                Email = request.Email,
                IsAdult = false,
                IsMitIdLinked = false
            };

            _serviceMock
                .Setup(s => s.RegisterAccountAsync(
                    It.Is<RegisterAccountDto>(d =>
                        d.Email == request.Email &&
                        d.Password == request.Password),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(registeredAccount);

            // Act
            var result = await _sut.Register(request, CancellationToken.None);

            // Assert
            var createdResult = Assert.IsType<CreatedAtActionResult>(result);
            Assert.Equal(nameof(AccountController.GetById), createdResult.ActionName);

            var response = Assert.IsType<AccountResponse>(createdResult.Value);
            Assert.Equal(registeredAccount.Id, response.Id);
            Assert.Equal(registeredAccount.Email, response.Email);
        }

        [Fact]
        public async Task Register_ReturnsBadRequest_WhenServiceThrowsInvalidOperation()
        {
            // Arrange
            var request = new AccountRegisterRequest
            {
                Email = "duplicate@example.com",
                Password = "Secret123!"
            };

            _serviceMock
                .Setup(s => s.RegisterAccountAsync(
                    It.IsAny<RegisterAccountDto>(),
                    It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("Email already in use"));

            // Act
            var result = await _sut.Register(request, CancellationToken.None);

            // Assert
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            var body = badRequest.Value!;
            var error = GetAnonymousProperty<string>(body, "error");

            Assert.Equal("Email already in use", error);
        }

        [Fact]
        public async Task Login_ReturnsUnauthorized_WhenAuthenticationFails()
        {
            // Arrange
            var authResult = new AuthenticateAccountResult(
                success: false,
                accountId: null,
                failure: "Invalid credentials"
            );

            var request = new AccountLoginRequest
            {
                Email = "wrong@example.com",
                Password = "bad-password"
            };

            _serviceMock
                .Setup(s => s.AuthenticateAsync(
                    request.Email,
                    request.Password,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(authResult);

            // Act
            var result = await _sut.Login(request, CancellationToken.None);

            // Assert
            var unauthorized = Assert.IsType<UnauthorizedObjectResult>(result);
            var body = unauthorized.Value!;
            var error = GetAnonymousProperty<string>(body, "error");

            Assert.Equal("Invalid credentials", error);
        }

        [Fact]
        public async Task Login_ReturnsOkWithAccountId_WhenAuthenticationSucceeds()
        {
            // Arrange
            var accountId = Guid.NewGuid();

            var authResult = new AuthenticateAccountResult(
                success: true,
                accountId: accountId,
                failure: null
            );

            var request = new AccountLoginRequest
            {
                Email = "user@example.com",
                Password = "CorrectPassword123!"
            };

            _serviceMock
                .Setup(s => s.AuthenticateAsync(
                    request.Email,
                    request.Password,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(authResult);

            // Act
            var result = await _sut.Login(request, CancellationToken.None);

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result);
            var body = ok.Value!;
            var returnedAccountId = GetAnonymousProperty<Guid>(body, "AccountId");

            Assert.Equal(accountId, returnedAccountId);
        }

        [Fact]
        public async Task GetById_ReturnsNotFound_WhenServiceReturnsNull()
        {
            // Arrange
            var id = Guid.NewGuid();
            _serviceMock
                .Setup(s => s.GetAccountByIdAsync(id, It.IsAny<CancellationToken>()))
                .ReturnsAsync((AccountDto?)null);

            // Act
            var result = await _sut.GetById(id, CancellationToken.None);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task GetById_ReturnsOkWithAccount_WhenFound()
        {
            // Arrange
            var id = Guid.NewGuid();
            var dto = new AccountDto
            {
                Id = id,
                Email = "test@example.com",
                IsAdult = true,
                IsMitIdLinked = true
            };

            _serviceMock
                .Setup(s => s.GetAccountByIdAsync(id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(dto);

            // Act
            var result = await _sut.GetById(id, CancellationToken.None);

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<AccountResponse>(ok.Value);
            Assert.Equal(dto.Id, response.Id);
            Assert.Equal(dto.Email, response.Email);
        }

        [Fact]
        public async Task GetStatus_ReturnsNotFound_WhenAccountMissing()
        {
            // Arrange
            var id = Guid.NewGuid();
            _serviceMock
                .Setup(s => s.GetAccountByIdAsync(id, It.IsAny<CancellationToken>()))
                .ReturnsAsync((AccountDto?)null);

            // Act
            var result = await _sut.GetStatus(id, CancellationToken.None);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task GetStatus_ReturnsOkWithStatus_WhenAccountExists()
        {
            // Arrange
            var id = Guid.NewGuid();
            var dto = new AccountDto
            {
                Id = id,
                Email = "test@example.com",
                IsAdult = true,
                IsMitIdLinked = false
            };

            _serviceMock
                .Setup(s => s.GetAccountByIdAsync(id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(dto);

            // Act
            var result = await _sut.GetStatus(id, CancellationToken.None);

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<AccountStatusResponse>(ok.Value);
            Assert.True(response.IsAdult);
            Assert.False(response.IsMitIdLinked);
        }

        [Fact]
        public void Health_ReturnsOkWithStatus()
        {
            // Act
            var result = _sut.Health();

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result);
            var body = ok.Value!;

            var status = GetAnonymousProperty<string>(body, "status");
            var service = GetAnonymousProperty<string>(body, "service");
            var timestamp = GetAnonymousProperty<DateTime>(body, "timestamp");

            Assert.Equal("healthy", status);
            Assert.Equal("account-service", service);
            Assert.True(timestamp <= DateTime.UtcNow.AddSeconds(5));
        }
    }
}
