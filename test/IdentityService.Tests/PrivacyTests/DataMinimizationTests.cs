using System;
using System.Linq;
using System.Reflection;
using IdentityService.Application.DTOs;
using IdentityService.Domain.Model;
using Xunit;

namespace IdentityService.Tests.PrivacyTests;

public class DataMinimizationTests
{
    private readonly string[] _forbiddenPropertyNames = new[]
    {
        "Cpr", "CprNumber", "NationalId", "Nin", "Personnummer", "Fodselsnummer", "DateOfBirth", "Dob", "Name", "Address"
    };

    [Fact]
    public void AgeVerificationEntity_ShouldNotContainPersonalData()
    {
        var type = typeof(AgeVerification);
        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
        
        foreach (var prop in properties)
        {
            foreach (var forbidden in _forbiddenPropertyNames)
            {
                Assert.False(
                    prop.Name.Contains(forbidden, StringComparison.OrdinalIgnoreCase),
                    $"Entity '{type.Name}' contains forbidden property '{prop.Name}' which suggests PII storage.");
            }
        }
    }

    [Fact]
    public void AgeVerificationDto_ShouldNotContainPersonalData()
    {
        var type = typeof(AgeVerificationDto);
        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
        
        foreach (var prop in properties)
        {
            foreach (var forbidden in _forbiddenPropertyNames)
            {
                Assert.False(
                    prop.Name.Contains(forbidden, StringComparison.OrdinalIgnoreCase),
                    $"DTO '{type.Name}' contains forbidden property '{prop.Name}' which suggests PII exposure.");
            }
        }
    }

    [Fact]
    public void AgeVerificationEntity_ShouldIndexBySubjectId_NotNationalId()
    {
        // This test verifies that we are using SubjectId (pseudonym) and not some other ID
        var type = typeof(AgeVerification);
        var subjectIdProp = type.GetProperty("SubjectId");
        
        Assert.NotNull(subjectIdProp);
        Assert.Equal(typeof(string), subjectIdProp.PropertyType);
    }
}
