using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using Domain.Models;
using Domain.Repositories;

namespace Infrastructure.Persistence
{
    public class VerificationLogRepository : IVerificationLogRepository
    {
        private readonly string _connectionString;
        private readonly ILogger<VerificationLogRepository> _logger;

        public VerificationLogRepository(
            IConfiguration configuration,
            ILogger<VerificationLogRepository> logger)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException(
                    "Connection string 'DefaultConnection' not found");

            _logger = logger;
        }

        private SqlConnection CreateConnection()
        {
            try
            {
                _logger.LogInformation("Creating SQL connection for ValidationService...");
                return new SqlConnection(_connectionString);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create SQL connection");
                throw;
            }
        }

        public async Task InsertAsync(VerificationLog log, CancellationToken ct = default)
        {
            const string sql = @"
INSERT INTO VerificationLogs (
    Id,
    VcJwtHash,
    IsValid,
    FailureReason,
    VerifiedAt
)
VALUES (
    @Id,
    @VcJwtHash,
    @IsValid,
    @FailureReason,
    @VerifiedAt
);";

            using var connection = CreateConnection();

            await connection.OpenAsync(ct);

            var command = new CommandDefinition(
                sql,
                new
                {
                    log.Id,
                    log.VcJwtHash,
                    log.IsValid,
                    log.FailureReason,
                    log.VerifiedAt
                },
                cancellationToken: ct
            );

            await connection.ExecuteAsync(command);
        }
    }
}
