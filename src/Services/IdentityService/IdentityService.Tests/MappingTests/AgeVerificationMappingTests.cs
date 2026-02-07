using System.Globalization;
using IdentityService.Application.DTOs;
using IdentityService.Domain.Interfaces;
using IdentityService.Domain.Exceptions; // Added
using IdentityService.Infrastructure.Mapping;
using Microsoft.Extensions.Logging;
using Moq;

namespace IdentityService.Tests.MappingTests;

public class AgeVerificationMappingTests
{
    private readonly Mock<ISafeLogger<MitIdClaimsMapper>> _loggerMock = new();
    private readonly MitIdClaimsMapper _mitIdMapper;

    public AgeVerificationMappingTests()
    {
        _mitIdMapper = new MitIdClaimsMapper(_loggerMock.Object, TimeProvider.System);
    }

    [Fact]
    public void MapToAgeVerification_ShouldFail_WhenDateOfBirthMissing()
    {
        // Arrange
        // Positional record: Id, Status, Provider, AuthenticationUrl, Loa, Subject, ExpiresAt
        var session = new SessionDataDto(
            "session-123",
            null,
            null,
            null,
            null,
            new SubjectDto("subj-123", null), // Missing DateOfBirth
            null
        );

        // Act & Assert
        var ex = Assert.Throws<AgeVerificationException>(() => _mitIdMapper.MapToAgeVerification(session));
        Assert.Equal(AgeVerificationErrorCode.MISSING_ATTRIBUTE, ex.ErrorCode);
    }

    [Fact]
    public void MapToAgeVerification_ShouldCalculateIsAdult_FromDateOfBirth()
    {
        // Arrange
        var dob = DateTime.UtcNow.AddYears(-20).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        var session = new SessionDataDto(
            "session-123",
            null,
            null,
            null,
            null,
            new SubjectDto("subj-123", dob),
            null
        );

        // Act
        var result = _mitIdMapper.MapToAgeVerification(session);

        // Assert
        Assert.True(result.IsAdult);
        Assert.Equal("mitid", result.ProviderId);
    }

    [Fact]
    public void MapToAgeVerification_ShouldCalculateIsNotAdult_FromDateOfBirth()
    {
        // Arrange
        var dob = DateTime.UtcNow.AddYears(-17).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        var session = new SessionDataDto(
            "session-123",
            null,
            null,
            null,
            null,
            new SubjectDto("subj-123", dob),
            null
        );

        // Act
        var result = _mitIdMapper.MapToAgeVerification(session);

        // Assert
        Assert.False(result.IsAdult);
    }

    [Fact]
    public void MapToAgeVerification_ShouldUseUtcForAgeCalculation()
    {
        // Arrange: Born exactly 18 years ago today (UTC)
        var today = DateTime.UtcNow.Date;
        var dob = today.AddYears(-18).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        
        var session = new SessionDataDto(
            "session-123",
            null,
            null,
            null,
            null,
            new SubjectDto("subj-123", dob),
            null
        );

        // Act
        var result = _mitIdMapper.MapToAgeVerification(session);

        // Assert
        Assert.True(result.IsAdult);
    }

    [Fact]
    public void MapToAgeVerification_ShouldNotExposePersonalData()
    {
        // Arrange
        var dob = "2000-01-01";
        // Verify via reflection that we cannot even pass NationalId to SubjectDto
        // because it doesn't exist on the type.
        // So strict PII firewall is enforced by type system.
        
        var session = new SessionDataDto(
            "session-123",
            null,
            null,
            null,
            null,
            new SubjectDto("subj-123", dob),
            null
        );

        // Act
        var result = _mitIdMapper.MapToAgeVerification(session);

        // Assert
        Assert.Equal("subj-123", result.SubjectId); // Only SubjectId is kept
        
        // Assert result type does not have sensitive fields
        var type = result.GetType();
        Assert.Null(type.GetProperty("NationalId"));
        Assert.Null(type.GetProperty("Cpr"));
    }
}
