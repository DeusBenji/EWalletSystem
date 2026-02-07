using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using IdentityService.Application.DTOs;
using IdentityService.Application.Interfaces;
using IdentityService.Domain.Interfaces;
using IdentityService.Domain.Model;
using IdentityService.Infrastructure.Configuration;
using IdentityService.Infrastructure.Mapping;
using IdentityService.Infrastructure.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace IdentityService.Tests.IntegrationTests;

public class AuthFlowTests
{
    private readonly Mock<ISignicatHttpClient> _httpClientMock = new();
    private readonly Mock<ISessionCache> _sessionCacheMock = new();
    private readonly Mock<IAgeVerificationRepository> _repositoryMock = new();
    private readonly Mock<ISafeLogger<SignicatAuthService>> _loggerMock = new();
    private readonly IOptions<SignicatConfig> _config;
    private readonly List<IClaimsMapper> _mappers;

    public AuthFlowTests()
    {
        // Config
        var config = new SignicatConfig
        {
            BaseUrl = "https://api.signicat.com",
            AllowedProviders = new List<string> { "mitid", "sbid", "nbid" },
            Providers = new Dictionary<string, IdentityService.Infrastructure.Configuration.ProviderAuthOptions>
            {
                { "mitid", new IdentityService.Infrastructure.Configuration.ProviderAuthOptions { GlobalCallbackUrl = "https://example.com" } }
            }
        };
        _config = Options.Create(config);

        // Real Mappers
        var mitIdLogger = new Mock<ISafeLogger<MitIdClaimsMapper>>();
        var sbidLogger = new Mock<ISafeLogger<BankIdSeClaimsMapper>>();
        var nbidLogger = new Mock<ISafeLogger<BankIdNoClaimsMapper>>();
        var timeProvider = TimeProvider.System;

        _mappers = new List<IClaimsMapper>
        {
            new MitIdClaimsMapper(mitIdLogger.Object, timeProvider),
            new BankIdSeClaimsMapper(sbidLogger.Object, timeProvider),
            new BankIdNoClaimsMapper(nbidLogger.Object, timeProvider)
        };
    }

    [Fact]
    public async Task StartAuthenticationAsync_ShouldReturnAuthUrl()
    {
        // Arrange
        var providerId = "mitid";
        var expectedUrl = "https://signicat.com/auth/123";
        var expectedSessionId = "session-123";

        _httpClientMock.Setup(x => x.CreateSessionAsync(It.IsAny<SessionRequestDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SessionDataDto(
                Id: expectedSessionId,
                Status: "SUCCESS",
                Provider: providerId,
                AuthenticationUrl: expectedUrl,
                Loa: null,
                Subject: null,
                ExpiresAt: null
            ));

        var sut = new SignicatAuthService(
            _httpClientMock.Object,
            _sessionCacheMock.Object,
            _repositoryMock.Object,
            _mappers,
            _config,
            _loggerMock.Object);

        // Act
        var result = await sut.StartAuthenticationAsync(providerId);

        // Assert
        Assert.Equal(expectedUrl, result.authUrl);
        Assert.Equal(expectedSessionId, result.sessionId);
        
        _sessionCacheMock.Verify(x => x.SetSessionAsync(expectedSessionId, It.IsAny<string>(), providerId, It.IsAny<TimeSpan>()), Times.Once);
    }

    [Fact]
    public async Task HandleCallbackAsync_ShouldVerifyAge_AndStoreResult()
    {
        // Arrange
        var providerId = "mitid";
        var sessionId = "session-123";
        var dob = DateTime.UtcNow.AddYears(-20).ToString("yyyy-MM-dd"); // Adult

        _sessionCacheMock.Setup(x => x.ExistsAsync(sessionId)).ReturnsAsync(true);
        
        _httpClientMock.Setup(x => x.GetSessionStatusAsync(sessionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SessionDataDto(
                Id: sessionId,
                Status: "SUCCESS",
                Provider: "mitid",
                AuthenticationUrl: null,
                Loa: "substantial",
                Subject: new SubjectDto(Id: "subject-123", DateOfBirth: dob),
                ExpiresAt: null
            ));

        var sut = new SignicatAuthService(
            _httpClientMock.Object,
            _sessionCacheMock.Object,
            _repositoryMock.Object,
            _mappers,
            _config,
            _loggerMock.Object);

        // Act
        var result = await sut.HandleCallbackAsync(providerId, sessionId);

        // Assert
        Assert.True(result.IsAdult);
        Assert.Equal("subject-123", result.SubjectId);
        
        _repositoryMock.Verify(x => x.UpsertVerificationAsync(It.Is<AgeVerification>(av => 
            av.ProviderId == providerId && 
            av.SubjectId == "subject-123" && 
            av.IsAdult == true)), Times.Once);

        _sessionCacheMock.Verify(x => x.RemoveSessionAsync(sessionId), Times.Once);
    }

    [Fact]
    public async Task HandleCallbackAsync_ShouldFail_WhenSessionInvalid()
    {
         // Arrange
        var providerId = "mitid";
        var sessionId = "invalid-session";

        _sessionCacheMock.Setup(x => x.ExistsAsync(sessionId)).ReturnsAsync(false);

        var sut = new SignicatAuthService(
            _httpClientMock.Object,
            _sessionCacheMock.Object,
            _repositoryMock.Object,
            _mappers,
            _config,
            _loggerMock.Object);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.HandleCallbackAsync(providerId, sessionId));
    }
}
