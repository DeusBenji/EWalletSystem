using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BachMitID.Domain.Model;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using BachMitID.Domain.Interfaces;

namespace BachMitID.Infrastructure.Databaselayer
{
    public class AccountDatabaseAccess : IAccDbAccess
    {
        readonly string? _connectionString;
        public AccountDatabaseAccess(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("CompanyConnection");
        }
        public AccountDatabaseAccess(string? inConnectionString)
        {
            _connectionString = inConnectionString;
        }

        public async Task<Account?> GetAccountById(Guid id)
        {
            Account? foundAccount = null;
            string queryString = @"SELECT ID, Email FROM Account WHERE ID = @ID;";
            using (SqlConnection con = new SqlConnection(_connectionString))
            using (SqlCommand readCommand = new SqlCommand(queryString, con))
            {
                readCommand.Parameters.AddWithValue("@ID", id);
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

        public async Task<Guid> CreateAccount(Account acc)
        {

            string queryString = @"INSERT INTO Account (ID, Email, Password) OUTPUT INSERTED.ID VALUES ($ID, @Email, @Password);";
            using (SqlConnection con = new SqlConnection(_connectionString))
            using (SqlCommand createCommand = new SqlCommand(queryString, con))
            {
                createCommand.Parameters.AddWithValue("@ID", acc.ID);
                createCommand.Parameters.AddWithValue("@Email", acc.Email);
                createCommand.Parameters.AddWithValue("@Password", acc.Password);
                await con.OpenAsync();
                await createCommand.ExecuteScalarAsync();

            }
            return acc.ID;
        }

        public async Task<List<Account>> GetAccounts()
        {
            List<Account> accounts = new List<Account>();
            string queryString = @"SELECT ID, Email FROM Account;";
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

        public async Task<bool> UpdateAccount(Account acc)
        {
            int rowsAffected = 0;
            string queryString = @"UPDATE Account SET Email = @Email, Password = @Password WHERE ID = @ID;";
            using (SqlConnection con = new SqlConnection(_connectionString))
            using (SqlCommand updateCommand = new SqlCommand(queryString, con))
            {
                updateCommand.Parameters.AddWithValue("@Email",  acc.Email);
                updateCommand.Parameters.AddWithValue("@ID", acc.ID);
                await con.OpenAsync();
                rowsAffected = await updateCommand.ExecuteNonQueryAsync();
            }
            return rowsAffected > 0;
        }

        public async Task<bool> DeleteAccount(Guid id)
        {
            int rowsAffected = 0;
            string queryString = @"DELETE FROM Account WHERE ID = @ID;";
            using (SqlConnection con = new SqlConnection(_connectionString))
            using (SqlCommand deleteCommand = new SqlCommand(queryString, con))
            {
                deleteCommand.Parameters.AddWithValue("@ID", id);
                await con.OpenAsync();
                rowsAffected = await deleteCommand.ExecuteNonQueryAsync();
            }
            return rowsAffected > 0;
        }


        private Account MapAccount(SqlDataReader reader)
        {
            Guid id = reader.GetGuid(reader.GetOrdinal("ID"));
            string email = reader.GetString(reader.GetOrdinal("Email"));
            return new Account(id, email);
        }


    }
}
