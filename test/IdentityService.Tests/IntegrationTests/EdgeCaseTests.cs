using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using IdentityService.Application.DTOs;
using IdentityService.Application.Interfaces;
using IdentityService.Domain.Interfaces;
using IdentityService.Domain.Model;
using IdentityService.Infrastructure.Configuration;
using IdentityService.Infrastructure.Http;
using IdentityService.Infrastructure.Mapping;
using IdentityService.Infrastructure.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Moq;
using Xunit;
using ProviderAuthOptions = IdentityService.Infrastructure.Configuration.ProviderAuthOptions;

namespace IdentityService.Tests.IntegrationTests;

public class EdgeCaseTests
{
    private readonly Mock<ISignicatHttpClient> _httpClientMock = new();
    private readonly Mock<ISessionCache> _sessionCacheMock = new();
    private readonly Mock<IAgeVerificationRepository> _repositoryMock = new();
    private readonly Mock<ISafeLogger<SignicatAuthService>> _loggerMock = new();
    private readonly Mock<TimeProvider> _timeProviderMock = new();
    private readonly SignicatAuthService _sut;

    public EdgeCaseTests()
    {
        var fixedTime = new DateTimeOffset(2024, 1, 1, 12, 0, 0, TimeSpan.Zero); // Jan 1st 2024
        _timeProviderMock.Setup(x => x.GetUtcNow()).Returns(fixedTime);

        var config = Options.Create(new SignicatConfig
        {
             BaseUrl = "https://api.signicat.com",
             AllowedProviders = new List<string> { "mitid", "sbid" },
             Providers = new Dictionary<string, ProviderAuthOptions>
             {
                 { "mitid", new ProviderAuthOptions { GlobalCallbackUrl = "https://example.com" } },
                 { "sbid", new ProviderAuthOptions { GlobalCallbackUrl = "https://example.com" } }
             }
        });

        // Mappers with FakeTimeProvider
        var mappers = new List<IClaimsMapper>
        {
            new MitIdClaimsMapper(new Mock<ISafeLogger<MitIdClaimsMapper>>().Object, _timeProviderMock.Object),
            new BankIdSeClaimsMapper(new Mock<ISafeLogger<BankIdSeClaimsMapper>>().Object, _timeProviderMock.Object)
        };

        _sut = new SignicatAuthService(
            _httpClientMock.Object,
            _sessionCacheMock.Object,
            _repositoryMock.Object,
            mappers,
            config,
            _loggerMock.Object);
    }

    [Fact]
    public void ClockDrift_BirthdayTomorrow_ShouldBeMinor()
    {
        // Scenario: User turns 18 TOMORROW. Should be considered minor TODAY.
        // Today: 2024-01-01
        // Birthday: 2006-01-02 (18 years ago + 1 day)
        
        var dob = "2006-01-02"; 
        
        // Manual verification logic check (using Mapper directly for precision)
        var subject = new SubjectDto(Id: "subj-1", DateOfBirth: dob);
        var session = new SessionDataDto("ses-1", "SUCCESS", "mitid", null, "substantial", subject, null);
        
        var mapper = new MitIdClaimsMapper(new Mock<ISafeLogger<MitIdClaimsMapper>>().Object, _timeProviderMock.Object);
        var result = mapper.MapToAgeVerification(session);
        
        Assert.False(result.IsAdult, "User should be minor if birthday is tomorrow");
    }

    [Fact]
    public void ClockDrift_BirthdayToday_ShouldBeAdult()
    {
        // Scenario: User turns 18 TODAY. Should be adult.
        // Today: 2024-01-01
        // Birthday: 2006-01-01 (Exactly 18 years ago)
        
        var dob = "2006-01-01"; 
        
        var subject = new SubjectDto(Id: "subj-1", DateOfBirth: dob);
        var session = new SessionDataDto("ses-1", "SUCCESS", "mitid", null, "substantial", subject, null);
        
        var mapper = new MitIdClaimsMapper(new Mock<ISafeLogger<MitIdClaimsMapper>>().Object, _timeProviderMock.Object);
        var result = mapper.MapToAgeVerification(session);
        
        Assert.True(result.IsAdult, "User should be adult on their 18th birthday");
    }

    [Fact]
    public async Task ProviderMismatch_ShouldThrow_AndPreventLogin()
    {
        // Scenario: Callback for 'mitid' flow, but Session data says 'sbid'
        // This could happen if a user has tabs open for both or during a specific attack
        
        var requestedProvider = "mitid";
        var sessionId = "session-mismatch";
        
        _sessionCacheMock.Setup(x => x.ExistsAsync(sessionId)).ReturnsAsync(true);
        
        _httpClientMock.Setup(x => x.GetSessionStatusAsync(sessionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SessionDataDto(
                Id: sessionId,
                Status: "SUCCESS",
                Provider: "sbid", // MISMATCH!
                AuthenticationUrl: null,
                Loa: "substantial",
                Subject: new SubjectDto("subj-1", "2000-01-01"),
                ExpiresAt: null
            ));

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => 
            _sut.HandleCallbackAsync(requestedProvider, sessionId));
            
        Assert.Contains("Provider mismatch", ex.Message);
        
        // Verify we DID NOT save to DB
        _repositoryMock.Verify(x => x.UpsertVerificationAsync(It.IsAny<AgeVerification>()), Times.Never);
    }
}
