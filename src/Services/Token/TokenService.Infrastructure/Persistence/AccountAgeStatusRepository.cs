using System.Data;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Domain.Models;
using Domain.Repositories;

namespace Infrastructure.Persistence
{
    public class AccountAgeStatusRepository : IAccountAgeStatusRepository
    {
        private readonly string _connectionString;
        private readonly ILogger<AccountAgeStatusRepository> _logger;

        public AccountAgeStatusRepository(
            IConfiguration configuration,
            ILogger<AccountAgeStatusRepository> logger)
        {
            _connectionString = configuration.GetConnectionString("TokenServiceConnection")
                ?? throw new InvalidOperationException("Missing connection string 'TokenServiceConnection'.");
            _logger = logger;
        }

        private SqlConnection CreateConnection()
        {
            try
            {
                return new SqlConnection(_connectionString);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create SQL connection");
                throw;
            }
        }

        public async Task<AccountAgeStatus?> GetAsync(Guid accountId, CancellationToken ct = default)
        {
            const string sql = @"
                SELECT AccountId, IsAdult, VerifiedAt
                FROM AccountAgeStatus
                WHERE AccountId = @AccountId";

            using var conn = CreateConnection();
            await conn.OpenAsync(ct);

            var command = new CommandDefinition(sql, new { AccountId = accountId }, cancellationToken: ct);

            var row = await conn.QuerySingleOrDefaultAsync<AccountAgeStatusRow>(command);

            if (row is null)
                return null;

            return new AccountAgeStatus(
                row.AccountId,
                row.IsAdult,
                row.VerifiedAt);
        }

        public async Task SaveAsync(AccountAgeStatus status, CancellationToken ct = default)
        {
            const string sql = @"
                MERGE AccountAgeStatus WITH (HOLDLOCK) AS target
                USING (SELECT @AccountId AS AccountId) AS src
                ON target.AccountId = src.AccountId
                WHEN MATCHED THEN 
                    UPDATE SET IsAdult = @IsAdult, VerifiedAt = @VerifiedAt
                WHEN NOT MATCHED THEN
                    INSERT (AccountId, IsAdult, VerifiedAt)
                    VALUES (@AccountId, @IsAdult, @VerifiedAt);";

            var parameters = new
            {
                status.AccountId,
                status.IsAdult,
                status.VerifiedAt
            };

            using var conn = CreateConnection();
            await conn.OpenAsync(ct);

            var command = new CommandDefinition(sql, parameters, cancellationToken: ct);
            await conn.ExecuteAsync(command);
        }

        private sealed class AccountAgeStatusRow
        {
            public Guid AccountId { get; set; }
            public bool IsAdult { get; set; }
            public DateTime VerifiedAt { get; set; }
        }
    }
}
