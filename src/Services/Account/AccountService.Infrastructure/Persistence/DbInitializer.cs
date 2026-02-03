using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Persistence
{
    public class DbInitializer
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<DbInitializer> _logger;

        public DbInitializer(IConfiguration configuration, ILogger<DbInitializer> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task InitializeAsync()
        {
            var connectionString = _configuration.GetConnectionString("DefaultConnection");
            if (string.IsNullOrWhiteSpace(connectionString)) return;

            var builder = new SqlConnectionStringBuilder(connectionString);
            var originalDatabase = builder.InitialCatalog;

            // 1. Create Database if not exists
            builder.InitialCatalog = "master";
            using (var masterConn = new SqlConnection(builder.ConnectionString))
            {
                await masterConn.OpenAsync();
                var dbExists = await masterConn.ExecuteScalarAsync<int?>(
                    "SELECT 1 FROM sys.databases WHERE name = @name", 
                    new { name = originalDatabase });

                if (dbExists == null)
                {
                    _logger.LogInformation("Creating database {Database}...", originalDatabase);
                    await masterConn.ExecuteAsync($"CREATE DATABASE [{originalDatabase}]");
                }
            }

            // 2. Create Tables
            using (var conn = new SqlConnection(connectionString))
            {
                await conn.OpenAsync();
                
                var sql = @"
                    IF OBJECT_ID(N'Account', N'U') IS NULL
                    BEGIN
                        CREATE TABLE Account (
                            ID UNIQUEIDENTIFIER PRIMARY KEY,
                            Email NVARCHAR(255) NOT NULL,
                            PasswordHash NVARCHAR(MAX) NULL,
                            CreatedAt DATETIME2 NOT NULL,
                            IsActive BIT NOT NULL DEFAULT 1,
                            IsMitIdVerified BIT NOT NULL DEFAULT 0,
                            IsAdult BIT NOT NULL DEFAULT 0
                        );
                        
                        CREATE UNIQUE INDEX IX_Account_Email ON Account(Email);
                    END";

                await conn.ExecuteAsync(sql);
                _logger.LogInformation("Database tables ensured.");
            }
        }
    }
}
