using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
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
        private const string TableName = "Account";

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
            var connection = new SqlConnection(_connectionString);
            _logger.LogDebug("Created SQL connection for {Database}", connection.Database);
            return connection;
        }

        private static string NormalizeEmail(string? email) =>
            (email ?? string.Empty).Trim().ToLowerInvariant();

        public async Task<Account?> GetByIdAsync(Guid id, CancellationToken ct = default)
        {
            var sql = $@"
                SELECT 
                    ID          AS Id,
                    Email,
                    PasswordHash,
                    CreatedAt,
                    IsActive
                FROM {TableName}
                WHERE ID = @Id";

            using var conn = CreateConnection();

            var row = await conn.QueryFirstOrDefaultAsync<AccountData>(
                new CommandDefinition(sql, new { Id = id }, cancellationToken: ct));

            return row is null
                ? null
                : Account.Reconstruct(
                    row.Id,
                    row.Email,
                    row.PasswordHash,
                    row.CreatedAt,
                    row.IsActive);
        }

        public async Task<Account?> GetByEmailAsync(string email, CancellationToken ct = default)
        {
            var normalized = NormalizeEmail(email);

            var sql = $@"
                SELECT 
                    ID          AS Id,
                    Email,
                    PasswordHash,
                    CreatedAt,
                    IsActive
                FROM {TableName}
                WHERE Email = @Email";

            using var conn = CreateConnection();

            var row = await conn.QueryFirstOrDefaultAsync<AccountData>(
                new CommandDefinition(sql, new { Email = normalized }, cancellationToken: ct));

            return row is null
                ? null
                : Account.Reconstruct(
                    row.Id,
                    row.Email,
                    row.PasswordHash,
                    row.CreatedAt,
                    row.IsActive);
        }

        public async Task<Account> CreateAsync(Account account, CancellationToken ct = default)
        {
            var sql = $@"
                INSERT INTO {TableName} (ID, Email, PasswordHash, CreatedAt, IsActive)
                VALUES (@Id, @Email, @PasswordHash, @CreatedAt, @IsActive)";

            using var conn = CreateConnection();

            try
            {
                await conn.ExecuteAsync(new CommandDefinition(
                    sql,
                    new
                    {
                        Id = account.Id,
                        account.Email,
                        account.PasswordHash,
                        account.CreatedAt,
                        account.IsActive
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
            var normalized = NormalizeEmail(email);

            var sql = $@"SELECT 1 FROM {TableName} WHERE Email = @Email";

            using var conn = CreateConnection();

            var exists = await conn.ExecuteScalarAsync<int?>(
                new CommandDefinition(sql, new { Email = normalized }, cancellationToken: ct));

            return exists.HasValue;
        }

        public async Task UpdateAsync(Account account, CancellationToken ct = default)
        {
            var sql = $@"
                UPDATE {TableName}
                SET Email        = @Email,
                    PasswordHash = @PasswordHash,
                    IsActive     = @IsActive
                WHERE ID = @Id";

            using var conn = CreateConnection();

            var rows = await conn.ExecuteAsync(new CommandDefinition(
                sql,
                new
                {
                    Id = account.Id,
                    account.Email,
                    account.PasswordHash,
                    account.IsActive
                },
                cancellationToken: ct));

            if (rows == 0)
            {
                _logger.LogWarning("No rows updated for account {AccountId}", account.Id);
            }
        }

        private sealed record AccountData(
            Guid Id,
            string Email,
            string? PasswordHash,
            DateTime CreatedAt,
            bool IsActive);
    }
}
