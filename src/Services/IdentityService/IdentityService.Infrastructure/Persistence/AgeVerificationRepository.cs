using Dapper;
using IdentityService.Domain.Interfaces;
using IdentityService.Domain.Model;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace IdentityService.Infrastructure.Persistence;

public class AgeVerificationRepository : IAgeVerificationRepository
{
    private readonly string _connectionString;

    public AgeVerificationRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("CompanyConnection") 
            ?? throw new InvalidOperationException("Connection string 'CompanyConnection' not found.");
    }

    public async Task<AgeVerification> UpsertVerificationAsync(AgeVerification verification)
    {
        using var connection = new SqlConnection(_connectionString);
        
        var sql = @"
            MERGE INTO AgeVerifications AS target
            USING (SELECT @ProviderId AS ProviderId, @SubjectId AS SubjectId) AS source
            ON (target.ProviderId = source.ProviderId AND target.SubjectId = source.SubjectId)
            WHEN MATCHED THEN
                UPDATE SET 
                    IsAdult = @IsAdult,
                    VerifiedAt = @VerifiedAt,
                    AssuranceLevel = @AssuranceLevel,
                    ExpiresAt = @ExpiresAt,
                    UpdatedAt = @UpdatedAt,
                    AccountId = COALESCE(@AccountId, target.AccountId) -- Update AccountId only if provided? Or keep existing link? Assuming if provided we link, if null we keep.
            WHEN NOT MATCHED THEN
                INSERT (Id, AccountId, ProviderId, SubjectId, IsAdult, VerifiedAt, AssuranceLevel, ExpiresAt, CreatedAt, UpdatedAt)
                VALUES (@Id, @AccountId, @ProviderId, @SubjectId, @IsAdult, @VerifiedAt, @AssuranceLevel, @ExpiresAt, @CreatedAt, @UpdatedAt);
            
            -- Prepare return query
            SELECT * FROM AgeVerifications WHERE ProviderId = @ProviderId AND SubjectId = @SubjectId;
        ";

        // Handle GUID if new
        if (verification.Id == Guid.Empty) verification.Id = Guid.NewGuid();

        var result = await connection.QuerySingleAsync<AgeVerification>(sql, verification);
        return result;
    }

    public async Task<AgeVerification?> GetByProviderSubjectIdAsync(string providerId, string subjectId)
    {
        using var connection = new SqlConnection(_connectionString);
        var sql = "SELECT * FROM AgeVerifications WHERE ProviderId = @ProviderId AND SubjectId = @SubjectId";
        return await connection.QuerySingleOrDefaultAsync<AgeVerification>(sql, new { ProviderId = providerId, SubjectId = subjectId });
    }

    public async Task<AgeVerification?> GetByAccountIdAsync(Guid accountId)
    {
        using var connection = new SqlConnection(_connectionString);
        var sql = "SELECT * FROM AgeVerifications WHERE AccountId = @AccountId";
        return await connection.QuerySingleOrDefaultAsync<AgeVerification>(sql, new { AccountId = accountId });
    }
}
