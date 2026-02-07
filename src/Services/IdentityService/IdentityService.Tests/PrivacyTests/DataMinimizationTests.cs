using System.Reflection;
using IdentityService.Domain.Model;
using IdentityService.Infrastructure.Services;

namespace IdentityService.Tests.PrivacyTests;

public class DataMinimizationTests
{
    private readonly PiiRedactionService _redactionService = new();

    [Fact]
    public void AgeVerificationEntity_ShouldNotContainPiiFields()
    {
        // Verify via reflection that AgeVerification entity does NOT have CPR, NationalId, Name, etc.
        var type = typeof(AgeVerification);
        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
        var propertyNames = properties.Select(p => p.Name).ToList();

        Assert.DoesNotContain("Cpr", propertyNames);
        Assert.DoesNotContain("NationalId", propertyNames);
        Assert.DoesNotContain("DateOfBirth", propertyNames);
        Assert.DoesNotContain("Name", propertyNames);
        Assert.DoesNotContain("Address", propertyNames);
        
        // Assert only allowed fields exist (plus technical fields)
        var allowed = new[] { "Id", "AccountId", "ProviderId", "SubjectId", "IsAdult", "VerifiedAt", "AssuranceLevel", "ExpiresAt", "CreatedAt", "UpdatedAt", "PolicyVersion" };
        foreach (var prop in propertyNames)
        {
            Assert.Contains(prop, allowed);
        }
    }

    [Fact]
    public void PiiRedactionService_ShouldRedactCpr()
    {
        var input = "User CPR is 1234567890";
        var redacted = _redactionService.RedactLogMessage(input);
        Assert.DoesNotContain("1234567890", redacted);
        Assert.Contains("**CPR**", redacted);
    }

    [Fact]
    public void PiiRedactionService_ShouldRedactJsonFields()
    {
        var json = "{\"nationalId\": \"123456-7890\", \"other\": \"value\"}";
        var redacted = _redactionService.RedactJson(json);
        Assert.DoesNotContain("123456-7890", redacted);
        Assert.Contains("\"nationalId\":\"**REDACTED**\"", redacted);
    }
    
    [Fact]
    public void PiiRedactionService_ShouldDetectPii()
    {
        Assert.True(_redactionService.ContainsPii("My cpr is 1234567890"));
        Assert.False(_redactionService.ContainsPii("Just some regular text 12345"));
    }
}
