using IdentityService.Domain.Model;
using IdentityService.Infrastructure.Persistence;
using Microsoft.Extensions.Configuration;
using Microsoft.Data.SqlClient;

namespace IdentityService.Tests.Integration;

public class AgeVerificationRepositoryTests
{
    private readonly string _connectionString;
    private readonly AgeVerificationRepository _repository;

    public AgeVerificationRepositoryTests()
    {
        // Setup configuration
        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: true)
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                {"ConnectionStrings:CompanyConnection", "Server=(localdb)\\mssqllocaldb;Database=IdentityServiceTestDb;Trusted_Connection=True;MultipleActiveResultSets=true"}
            })
            .Build();

        _connectionString = configuration.GetConnectionString("CompanyConnection")!;
        _repository = new AgeVerificationRepository(configuration); // This line had error in syntax? No, typo in class name
    }

    [Fact]
    public async Task UpsertVerification_ShouldCreateNewRecord_WhenNotExists()
    {
        // Arrange
        if (!await IsDatabaseAvailable()) return; // Skip if no DB

        var verification = new AgeVerification
        {
            ProviderId = "mitid",
            SubjectId = Guid.NewGuid().ToString("N"), // Pseudonym
            IsAdult = true,
            VerifiedAt = DateTime.UtcNow,
            AssuranceLevel = "substantial",
            PolicyVersion = "1.0",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Act
        var result = await _repository.UpsertVerificationAsync(verification);

        // Assert
        Assert.NotNull(result);
        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal(verification.ProviderId, result.ProviderId);
        Assert.Equal(verification.SubjectId, result.SubjectId);
        Assert.True(result.IsAdult);
    }

    [Fact]
    public async Task UpsertVerification_ShouldUpdate_WhenExists()
    {
        // Arrange
        if (!await IsDatabaseAvailable()) return; // Skip if no DB

        var subjectId = Guid.NewGuid().ToString("N");
        var initial = new AgeVerification
        {
            ProviderId = "mitid",
            SubjectId = subjectId,
            IsAdult = false, // Initially false
            VerifiedAt = DateTime.UtcNow.AddDays(-1),
            AssuranceLevel = "substantial",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _repository.UpsertVerificationAsync(initial);

        var update = new AgeVerification
        {
            ProviderId = "mitid",
            SubjectId = subjectId,
            IsAdult = true, // Now true
            VerifiedAt = DateTime.UtcNow,
            AssuranceLevel = "high",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Act
        var result = await _repository.UpsertVerificationAsync(update);

        // Assert
        Assert.Equal(subjectId, result.SubjectId);
        Assert.True(result.IsAdult); // Should be updated
        Assert.Equal("high", result.AssuranceLevel);
    }

    private async Task<bool> IsDatabaseAvailable()
    {
        try
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();
            return true;
        }
        catch
        {
            return false;
        }
    }
}
