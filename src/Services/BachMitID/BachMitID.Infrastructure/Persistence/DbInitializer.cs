using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Data.SqlClient;
using Dapper; // Using Dapper for easy execution, assumes Dapper is referenced or we use raw SqlClient

namespace BachMitID.Infrastructure.Persistence
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
            var connectionString = _configuration.GetConnectionString("CompanyConnection");
            if (string.IsNullOrWhiteSpace(connectionString)) 
            {
                _logger.LogError("DbInitializer: Connection string 'CompanyConnection' is NULL or EMPTY.");
                return;
            }

            var builder = new SqlConnectionStringBuilder(connectionString);
            var originalDatabase = builder.InitialCatalog;
            _logger.LogInformation("DbInitializer: Checking database '{Database}' on server '{Server}'...", originalDatabase, builder.DataSource);

            // 1. Create Database if not exists
            builder.InitialCatalog = "master";
            try 
            {
                using (var masterConn = new SqlConnection(builder.ConnectionString))
                {
                    await masterConn.OpenAsync();
                    
                    // Raw SQL since we might not have Dapper here
                    using (var cmd = new SqlCommand($"SELECT 1 FROM sys.databases WHERE name = '{originalDatabase}'", masterConn))
                    {
                        var exists = await cmd.ExecuteScalarAsync();
                        if (exists == null)
                        {
                            Console.WriteLine($"[DbInitializer] Database '{originalDatabase}' does NOT exist. Creating...");
                            _logger.LogInformation("DbInitializer: Database '{Database}' does NOT exist. Creating...", originalDatabase);
                            using (var createCmd = new SqlCommand($"CREATE DATABASE [{originalDatabase}]", masterConn))
                            {
                                await createCmd.ExecuteNonQueryAsync();
                                Console.WriteLine("[DbInitializer] Database created successfully.");
                                _logger.LogInformation("DbInitializer: Database created successfully.");
                            }
                        }
                        else 
                        {
                             Console.WriteLine($"[DbInitializer] Database '{originalDatabase}' ALREADY EXISTS.");
                             _logger.LogInformation("DbInitializer: Database '{Database}' already exists.", originalDatabase);
                        }
                    }
                }
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, "DbInitializer: Failed during database creation check.");
                // Rethrowing might crash the app on startup, which is good for debugging
                throw;
            }

            // 2. Create Tables
            using (var conn = new SqlConnection(connectionString))
            {
                await conn.OpenAsync();
                
                var sql = @"
                    IF OBJECT_ID(N'Account', N'U') IS NULL
                    BEGIN
                        CREATE TABLE Account (
                            Id UNIQUEIDENTIFIER PRIMARY KEY,
                            Email NVARCHAR(255) NOT NULL
                        );
                    END;

                    IF OBJECT_ID(N'MitID_Account', N'U') IS NULL
                    BEGIN
                        CREATE TABLE MitID_Account (
                            ID UNIQUEIDENTIFIER PRIMARY KEY,
                            AccountID UNIQUEIDENTIFIER NOT NULL,
                            SubID NVARCHAR(255) NOT NULL,
                            IsAdult BIT NOT NULL,
                            FOREIGN KEY (AccountID) REFERENCES Account(Id)
                        );
                        
                        CREATE INDEX IX_MitID_Account_SubID ON MitID_Account(SubID);
                    END;";

                using (var cmd = new SqlCommand(sql, conn))
                {
                    await cmd.ExecuteNonQueryAsync();
                }
                _logger.LogInformation("BachMitID Database tables ensured.");
            }
        }
    }
}
