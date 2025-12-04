using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BachMitID.Domain.Model;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using BachMitID.Domain.Interfaces;

namespace BachMitID.Infrastructure.Databaselayer
{
    public class AccountDatabaseAccess : IAccDbAccess
    {
        private readonly string? _connectionString;

        public AccountDatabaseAccess(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("CompanyConnection");
        }

        public AccountDatabaseAccess(string? inConnectionString)
        {
            _connectionString = inConnectionString;
        }

        public async Task<Account?> GetAccountByIdAsync(Guid id)
        {
            Account? foundAccount = null;

            const string queryString = @"
                SELECT Id, Email
                FROM dbo.Account
                WHERE Id = @Id;";

            using (SqlConnection con = new SqlConnection(_connectionString))
            using (SqlCommand readCommand = new SqlCommand(queryString, con))
            {
                readCommand.Parameters.AddWithValue("@Id", id);
                await con.OpenAsync();

                using (SqlDataReader reader = await readCommand.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync())
                    {
                        foundAccount = MapAccount(reader);
                    }
                }
            }

            return foundAccount;
        }

        public async Task<Guid> CreateAccountAsync(Account acc)
        {
            const string queryString = @"
                INSERT INTO dbo.Account (Id, Email)
                OUTPUT INSERTED.Id 
                VALUES (@Id, @Email);";

            using (SqlConnection con = new SqlConnection(_connectionString))
            using (SqlCommand createCommand = new SqlCommand(queryString, con))
            {
                createCommand.Parameters.AddWithValue("@Id", acc.ID);
                createCommand.Parameters.AddWithValue("@Email", acc.Email);

                await con.OpenAsync();
                await createCommand.ExecuteScalarAsync();
            }

            return acc.ID;
        }

        public async Task<List<Account>> GetAccountsAsync()
        {
            List<Account> accounts = new List<Account>();

            const string queryString = @"
                SELECT Id, Email
                FROM dbo.Account;";

            using (SqlConnection con = new SqlConnection(_connectionString))
            using (SqlCommand readCommand = new SqlCommand(queryString, con))
            {
                await con.OpenAsync();

                using (SqlDataReader reader = await readCommand.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        accounts.Add(MapAccount(reader));
                    }
                }
            }

            return accounts;
        }

        public async Task<bool> UpdateAccountAsync(Account acc)
        {
            int rowsAffected = 0;

            const string queryString = @"
                UPDATE dbo.Account
                SET Email = @Email
                WHERE Id = @Id;";

            using (SqlConnection con = new SqlConnection(_connectionString))
            using (SqlCommand updateCommand = new SqlCommand(queryString, con))
            {
                updateCommand.Parameters.AddWithValue("@Email", acc.Email);
                updateCommand.Parameters.AddWithValue("@Id", acc.ID);

                await con.OpenAsync();
                rowsAffected = await updateCommand.ExecuteNonQueryAsync();
            }

            return rowsAffected > 0;
        }

        public async Task<bool> DeleteAccountAsync(Guid id)
        {
            int rowsAffected = 0;

            const string queryString = @"
                DELETE FROM dbo.Account
                WHERE Id = @Id;";

            using (SqlConnection con = new SqlConnection(_connectionString))
            using (SqlCommand deleteCommand = new SqlCommand(queryString, con))
            {
                deleteCommand.Parameters.AddWithValue("@Id", id);

                await con.OpenAsync();
                rowsAffected = await deleteCommand.ExecuteNonQueryAsync();
            }

            return rowsAffected > 0;
        }

        private Account MapAccount(SqlDataReader reader)
        {
            Guid id = reader.GetGuid(reader.GetOrdinal("Id"));
            string email = reader.GetString(reader.GetOrdinal("Email"));

            return new Account(id, email);
        }
    }
}
