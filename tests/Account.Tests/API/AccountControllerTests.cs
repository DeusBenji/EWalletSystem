using AutoMapper;
using AccountService.API.Contracts;
using AccountService.API.Controllers;
using AccountService.API.Security;
using Application.DTOs;
using Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
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

            _mapperMock
                .Setup(m => m.Map<RegisterAccountDto>(It.IsAny<AccountRegisterRequest>()))
                .Returns((AccountRegisterRequest r) => new RegisterAccountDto
                {
                    Email = r.Email,
                    Password = r.Password
                });

            _mapperMock
                .Setup(m => m.Map<AccountResponse>(It.IsAny<AccountDto>()))
                .Returns((AccountDto d) => new AccountResponse
                {
                    Id = d.Id,
                    Email = d.Email
                });

            // 🔐 JwtTokenService (minimal test-config)
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new System.Collections.Generic.Dictionary<string, string?>
                {
                    ["Jwt:Issuer"] = "test-issuer",
                    ["Jwt:Audience"] = "test-audience",
                    ["Jwt:SigningKey"] = "THIS_IS_A_TEST_SIGNING_KEY_32_CHARS_MIN!",
                    ["Jwt:ExpiryMinutes"] = "60"
                })
                .Build();

            var jwtTokenService = new JwtTokenService(config);

            // ✅ FIX: 4. parameter med
            _sut = new AccountController(
                _serviceMock.Object,
                _loggerMock.Object,
                _mapperMock.Object,
                jwtTokenService);
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
            var request = new AccountRegisterRequest
            {
                Email = "test@example.com",
                Password = "Secret123!"
            };

            var registeredAccount = new AccountDto
            {
                Id = Guid.NewGuid(),
                Email = request.Email,
            };
             
            _serviceMock
                .Setup(s => s.RegisterAccountAsync(It.IsAny<RegisterAccountDto>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(registeredAccount);

            var result = await _sut.Register(request, CancellationToken.None);

            var created = Assert.IsType<CreatedAtActionResult>(result);
            var response = Assert.IsType<AccountResponse>(created.Value);
            Assert.Equal(registeredAccount.Id, response.Id);
        }

        [Fact]
        public async Task Register_ReturnsBadRequest_WhenDuplicate()
        {
            _serviceMock
                .Setup(s => s.RegisterAccountAsync(It.IsAny<RegisterAccountDto>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("Email already in use"));

            var result = await _sut.Register(
                new AccountRegisterRequest { Email = "dup@test.com", Password = "Secret123!" },
                CancellationToken.None);

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task Login_ReturnsUnauthorized_WhenInvalidCredentials()
        {
            _serviceMock
                .Setup(s => s.AuthenticateAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new AuthenticateAccountResult(false, null, "Invalid credentials"));

            var result = await _sut.Login(
                new AccountLoginRequest { Email = "x", Password = "y" },
                CancellationToken.None);

            Assert.IsType<UnauthorizedObjectResult>(result);
        }

        [Fact]
        public async Task Login_ReturnsAccountIdAndToken_WhenSuccess()
        {
            var id = Guid.NewGuid();

            _serviceMock
                .Setup(s => s.AuthenticateAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new AuthenticateAccountResult(true, id, null));

            var result = await _sut.Login(
                new AccountLoginRequest { Email = "ok@test.com", Password = "123" },
                CancellationToken.None);

            var ok = Assert.IsType<OkObjectResult>(result);
            var body = ok.Value!;

            Assert.Equal(id, GetAnonymousProperty<Guid>(body, "accountId"));
            Assert.False(string.IsNullOrWhiteSpace(GetAnonymousProperty<string>(body, "accessToken")));
        }

        [Fact]
        public async Task GetById_ReturnsNotFound_WhenMissing()
        {
            _serviceMock
                .Setup(s => s.GetAccountByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((AccountDto?)null);

            var result = await _sut.GetById(Guid.NewGuid(), CancellationToken.None);
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public void Health_ReturnsHealthy()
        {
            var ok = Assert.IsType<OkObjectResult>(_sut.Health());
            Assert.Equal("healthy", GetAnonymousProperty<string>(ok.Value!, "status"));
        }
    }
}
