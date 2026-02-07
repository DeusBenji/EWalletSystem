using IdentityService.Application.DTOs;
using IdentityService.Application.Interfaces;
using IdentityService.Domain.Interfaces;
using IdentityService.Infrastructure.Configuration;
using IdentityService.Infrastructure.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using ConfigProviderAuthOptions = IdentityService.Infrastructure.Configuration.ProviderAuthOptions;

namespace IdentityService.Tests.IntegrationTests;

public class AuthFlowTests
{
    private readonly Mock<ISignicatHttpClient> _httpClientMock = new();
    private readonly Mock<ISessionCache> _sessionCacheMock = new();
    private readonly Mock<IAgeVerificationRepository> _repositoryMock = new();
    private readonly Mock<ISafeLogger<SignicatAuthService>> _loggerMock = new();
    private readonly List<IClaimsMapper> _mappers = new();
    private readonly SignicatConfig _config;

    public AuthFlowTests()
    {
        _config = new SignicatConfig
        {
            SessionLifetimeSeconds = 3600,
            AllowedProviders = new List<string> { "mitid", "sbid" },
            Providers = new Dictionary<string, ConfigProviderAuthOptions>
            {
                ["mitid"] = new ConfigProviderAuthOptions { GlobalCallbackUrl = "https://example.com" },
                ["sbid"] = new ConfigProviderAuthOptions { GlobalCallbackUrl = "https://example.com" }
            }
        };
    }

    [Fact]
    public async Task StartAuthenticationAsync_ShouldReturnAuthUrl()
    {
        // Arrange
        var service = CreateService();
        var providerId = "mitid";
        
        _httpClientMock.Setup(x => x.CreateSessionAsync(It.IsAny<SessionRequestDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SessionDataDto(
                "session-123",
                "created",
                null, 
                "https://signicat.com/auth/session-123",
                null,
                null,
                null
            ));

        // Act
        var result = await service.StartAuthenticationAsync(providerId, Guid.NewGuid());
        
        // Assert
        Assert.Equal("https://signicat.com/auth/session-123", result.authUrl);
        Assert.Equal("session-123", result.sessionId);
    }
    
    [Fact]
    public async Task HandleCallbackAsync_ShouldFail_WhenSessionNotFound()
    {
        // Arrange
        var service = CreateService();
        _sessionCacheMock.Setup(x => x.ExistsAsync("session-123")).ReturnsAsync(false);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => 
            service.HandleCallbackAsync("mitid", "session-123"));
    }

    private SignicatAuthService CreateService()
    {
        return new SignicatAuthService(
            _httpClientMock.Object,
            _sessionCacheMock.Object,
            _repositoryMock.Object,
            _mappers,
            Options.Create(_config),
            _loggerMock.Object
        );
    }
}
