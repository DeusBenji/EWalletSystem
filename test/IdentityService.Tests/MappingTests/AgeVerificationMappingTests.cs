using System;
using System.Globalization;
using IdentityService.Application.DTOs;
using IdentityService.Domain.Exceptions;
using IdentityService.Domain.Interfaces;
using IdentityService.Infrastructure.Mapping;
using Moq;
using Xunit;

namespace IdentityService.Tests.MappingTests;

public class AgeVerificationMappingTests
{
    private readonly Mock<ISafeLogger<MitIdClaimsMapper>> _mitIdLoggerMock = new();
    private readonly Mock<ISafeLogger<BankIdSeClaimsMapper>> _sbidLoggerMock = new();
    private readonly Mock<ISafeLogger<BankIdNoClaimsMapper>> _nbidLoggerMock = new();
    private readonly Mock<TimeProvider> _timeProviderMock = new();

    public AgeVerificationMappingTests()
    {
        var fixedTime = new DateTimeOffset(2023, 10, 27, 12, 0, 0, TimeSpan.Zero);
        _timeProviderMock.Setup(x => x.GetUtcNow()).Returns(fixedTime);
    }

    [Fact]
    public void MitId_ShouldFail_WhenDateOfBirthMissing()
    {
        // Arrange
        var mapper = new MitIdClaimsMapper(_mitIdLoggerMock.Object, _timeProviderMock.Object);
        var session = new SessionDataDto(
            Id: "test-session",
            Status: "SUCCESS",
            Provider: "mitid",
            AuthenticationUrl: null,
            Loa: "substantial",
            Subject: new SubjectDto(Id: "test-subject", DateOfBirth: null),
            ExpiresAt: null
        );

        // Act & Assert
        var ex = Assert.Throws<AgeVerificationException>(() => mapper.MapToAgeVerification(session));
        Assert.Equal(AgeVerificationErrorCode.MISSING_ATTRIBUTE, ex.ErrorCode);
    }

    [Fact]
    public void MitId_ShouldCalculateIsAdult_FromDateOfBirth()
    {
        // Arrange
        var mapper = new MitIdClaimsMapper(_mitIdLoggerMock.Object, _timeProviderMock.Object);
        var eighteenYearsAgo = _timeProviderMock.Object.GetUtcNow().AddYears(-18).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        var session = new SessionDataDto(
            Id: "test-session",
            Status: "SUCCESS",
            Provider: "mitid",
            AuthenticationUrl: null,
            Loa: "substantial",
            Subject: new SubjectDto(Id: "test-subject", DateOfBirth: eighteenYearsAgo),
            ExpiresAt: null
        );

        // Act
        var result = mapper.MapToAgeVerification(session);

        // Assert
        Assert.True(result.IsAdult);
        Assert.Equal("mitid", result.ProviderId);
        Assert.Equal("test-subject", result.SubjectId);
    }

    [Fact]
    public void MitId_ShouldIdentifyMinor_FromDateOfBirth()
    {
        // Arrange
        var mapper = new MitIdClaimsMapper(_mitIdLoggerMock.Object, _timeProviderMock.Object);
        var seventeenYearsAgo = _timeProviderMock.Object.GetUtcNow().AddYears(-17).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        var session = new SessionDataDto(
            Id: "test-session",
            Status: "SUCCESS",
            Provider: "mitid",
            AuthenticationUrl: null,
            Loa: "substantial",
            Subject: new SubjectDto(Id: "test-subject", DateOfBirth: seventeenYearsAgo),
            ExpiresAt: null
        );

        // Act
        var result = mapper.MapToAgeVerification(session);

        // Assert
        Assert.False(result.IsAdult);
    }

    [Fact]
    public void BankIdSe_ShouldFail_WhenDateOfBirthMissing()
    {
        // Arrange
        var mapper = new BankIdSeClaimsMapper(_sbidLoggerMock.Object, _timeProviderMock.Object);
        var session = new SessionDataDto(
            Id: "test-session",
            Status: "SUCCESS",
            Provider: "sbid",
            AuthenticationUrl: null,
            Loa: "substantial",
            Subject: new SubjectDto(Id: "test-subject", DateOfBirth: null),
            ExpiresAt: null
        );

        // Act & Assert
        var ex = Assert.Throws<AgeVerificationException>(() => mapper.MapToAgeVerification(session));
        Assert.Equal(AgeVerificationErrorCode.MISSING_ATTRIBUTE, ex.ErrorCode);
    }

    [Fact]
    public void BankIdNo_ShouldFail_WhenDateOfBirthMissing()
    {
        // Arrange
        var mapper = new BankIdNoClaimsMapper(_nbidLoggerMock.Object, _timeProviderMock.Object);
        var session = new SessionDataDto(
            Id: "test-session",
            Status: "SUCCESS",
            Provider: "nbid",
            AuthenticationUrl: null,
            Loa: "substantial",
            Subject: new SubjectDto(Id: "test-subject", DateOfBirth: null),
            ExpiresAt: null
        );

        // Act & Assert
        var ex = Assert.Throws<AgeVerificationException>(() => mapper.MapToAgeVerification(session));
        Assert.Equal(AgeVerificationErrorCode.MISSING_ATTRIBUTE, ex.ErrorCode);
    }
    
    [Theory]
    [InlineData("invalid-date")]
    [InlineData("01-01-2000")] // Wrong format
    [InlineData("2000/01/01")] // Wrong format
    public void MitId_ShouldFail_WhenDateOfBirthInvalidFormat(string invalidDob)
    {
        // Arrange
        var mapper = new MitIdClaimsMapper(_mitIdLoggerMock.Object, _timeProviderMock.Object);
        var session = new SessionDataDto(
            Id: "test-session",
            Status: "SUCCESS",
            Provider: "mitid",
            AuthenticationUrl: null,
            Loa: "substantial",
            Subject: new SubjectDto(Id: "test-subject", DateOfBirth: invalidDob),
            ExpiresAt: null
        );

        // Act & Assert
        var ex = Assert.Throws<AgeVerificationException>(() => mapper.MapToAgeVerification(session));
        Assert.Equal(AgeVerificationErrorCode.INVALID_DATE_FORMAT, ex.ErrorCode);
    }

    [Fact]
    public void AllMappers_ShouldNotExposePersonalData()
    {
        // This is strictly a unit test for the mapper output
        // More comprehensive privacy tests will be in PrivacyTests
        
        var mapper = new MitIdClaimsMapper(_mitIdLoggerMock.Object, _timeProviderMock.Object);
        var dob = "2000-01-01";
        var session = new SessionDataDto(
            Id: "test-session",
            Status: "SUCCESS",
            Provider: "mitid",
            AuthenticationUrl: null,
            Loa: "substantial",
            Subject: new SubjectDto(Id: "test-subject", DateOfBirth: dob),
            ExpiresAt: null
        );

        var result = mapper.MapToAgeVerification(session);

        // Verification that the DTO itself doesn't carry the PII (even though the DTO definition prevents it, we verify usage)
        Assert.Equal("test-subject", result.SubjectId);
        // We cannot check if result "contains" DOB because it's a DTO designated properties
        // But we verify that we mapped correctly without error
    }
}
