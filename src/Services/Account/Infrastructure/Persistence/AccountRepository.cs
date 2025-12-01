using System.Data;
using Domain.Models;
using Domain.Repositories;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Persistence
{
    public sealed class AccountRepository : IAccountRepository
    {
        private readonly string _connectionString;
        private readonly ILogger<AccountRepository> _logger;

        public AccountRepository(IConfiguration configuration, ILogger<AccountRepository> logger)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("Missing connection string 'DefaultConnection'.");
            _logger = logger;
        }

        private IDbConnection CreateConnection()
        {
            try
            {
                var connection = new SqlConnection(_connectionString);
                _logger.LogInformation("Attempting to open SQL connection...");
                return connection;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create SQL connection");
                throw;
            }
        }

        public async Task<Account?> GetByIdAsync(Guid id, CancellationToken ct = default)
        {
            const string sql = @"
                SELECT Id, Email, PasswordHash
                FROM dbo.Accounts
                WHERE Id = @Id";

            using var conn = CreateConnection();
            var row = await conn.QueryFirstOrDefaultAsync<AccountData>(
                new CommandDefinition(sql, new { Id = id }, cancellationToken: ct));

            return row is null
                ? null
                : Account.Reconstruct(row.Id, row.Email, row.PasswordHash);
        }

        public async Task<Account?> GetByEmailAsync(string email, CancellationToken ct = default)
        {
            var normalized = (email ?? string.Empty).Trim().ToLowerInvariant();

            const string sql = @"
                SELECT Id, Email, PasswordHash
                FROM dbo.Accounts
                WHERE Email = @Email";

            using var conn = CreateConnection();
            var row = await conn.QueryFirstOrDefaultAsync<AccountData>(
                new CommandDefinition(sql, new { Email = normalized }, cancellationToken: ct));

            return row is null
                ? null
                : Account.Reconstruct(row.Id, row.Email, row.PasswordHash);
        }

        public async Task<Account> CreateAsync(Account account, CancellationToken ct = default)
        {
            const string sql = @"
                INSERT INTO dbo.Accounts (Id, Email, PasswordHash)
                VALUES (@Id, @Email, @PasswordHash)";

            using var conn = CreateConnection();
            try
            {
                await conn.ExecuteAsync(new CommandDefinition(
                    sql,
                    new
                    {
                        account.Id,
                        account.Email,
                        account.PasswordHash
                    },
                    cancellationToken: ct));
            }
            catch (SqlException ex) when (ex.Number is 2627 or 2601) // unique violation
            {
                _logger.LogWarning(ex, "Duplicate email attempted: {Email}", account.Email);
                throw new InvalidOperationException("Email already exists");
            }

            return account;
        }

        public async Task<bool> EmailExistsAsync(string email, CancellationToken ct = default)
        {
            var normalized = (email ?? string.Empty).Trim().ToLowerInvariant();

            const string sql = @"SELECT 1 FROM dbo.Accounts WHERE Email = @Email";

            using var conn = CreateConnection();
            var exists = await conn.ExecuteScalarAsync<int?>(
                new CommandDefinition(sql, new { Email = normalized }, cancellationToken: ct));

            return exists.HasValue;
        }

        public async Task UpdateAsync(Account account, CancellationToken ct = default)
        {
            const string sql = @"
                UPDATE dbo.Accounts
                SET Email        = @Email,
                    PasswordHash = @PasswordHash
                WHERE Id = @Id";

            using var conn = CreateConnection();
            var rows = await conn.ExecuteAsync(new CommandDefinition(
                sql,
                new
                {
                    account.Id,
                    account.Email,
                    account.PasswordHash
                },
                cancellationToken: ct));

            if (rows == 0)
            {
                _logger.LogWarning("No rows updated for account {AccountId}", account.Id);
            }
        }

        private sealed record AccountData(Guid Id, string Email, string? PasswordHash);
    }
}
