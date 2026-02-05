using IdentityService.Domain.Model;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using IdentityService.Domain.Interfaces;

namespace IdentityService.Infrastructure.Databaselayer
{
    public partial class MitIdAccountDatabaseAccess : IMitIdDbAccess
    {
        readonly string? _connectionString;

        public MitIdAccountDatabaseAccess(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("CompanyConnection");
        }

        public MitIdAccountDatabaseAccess(string? inConnectionString)
        {
            _connectionString = inConnectionString;
        }

        public async Task<Guid> CreateMitIdAccount(MitID_Account mAcc)
        {
            string queryString = @"INSERT INTO MitID_Account (ID, AccountID, SubID, IsAdult) 
                                   OUTPUT INSERTED.ID 
                                   VALUES (@ID, @AccountID, @SubID, @IsAdult);";

            using (SqlConnection con = new SqlConnection(_connectionString))
            using (SqlCommand createCommand = new SqlCommand(queryString, con))
            {
                createCommand.Parameters.AddWithValue("@ID", mAcc.ID);
                createCommand.Parameters.AddWithValue("@AccountID", mAcc.AccountID);
                createCommand.Parameters.AddWithValue("@SubID", mAcc.SubID);
                createCommand.Parameters.AddWithValue("@IsAdult", mAcc.IsAdult);

                await con.OpenAsync();
                await createCommand.ExecuteScalarAsync();
            }

            return mAcc.ID;
        }

        public async Task<MitID_Account?> GetMitIdAccountByAccId(Guid accId)
        {
            MitID_Account? foundMitIdAccount = null;

            string queryString = @"SELECT ID, AccountID, SubID, IsAdult 
                                   FROM MitID_Account 
                                   WHERE AccountID = @AccountID;";

            using (SqlConnection con = new SqlConnection(_connectionString))
            using (SqlCommand readCommand = new SqlCommand(queryString, con))
            {
                readCommand.Parameters.AddWithValue("@AccountID", accId);
                await con.OpenAsync();

                using (SqlDataReader reader = await readCommand.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync())
                    {
                        foundMitIdAccount = MapMitIdAccount(reader);
                    }
                }
            }

            return foundMitIdAccount;
        }

        // ?? NY: slå MitID-account op på hashed SubId
        public async Task<MitID_Account?> GetMitIdAccountBySubId(string hashedSubId)
        {
            MitID_Account? foundMitIdAccount = null;

            string queryString = @"SELECT ID, AccountID, SubID, IsAdult 
                                   FROM MitID_Account 
                                   WHERE SubID = @SubID;";

            using (SqlConnection con = new SqlConnection(_connectionString))
            using (SqlCommand readCommand = new SqlCommand(queryString, con))
            {
                readCommand.Parameters.AddWithValue("@SubID", hashedSubId);
                await con.OpenAsync();

                using (SqlDataReader reader = await readCommand.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync())
                    {
                        foundMitIdAccount = MapMitIdAccount(reader);
                    }
                }
            }

            return foundMitIdAccount;
        }

        public async Task<List<MitID_Account>> GetAllMitIdAccounts()
        {
            List<MitID_Account> mitIdAccounts = new List<MitID_Account>();

            string queryString = @"SELECT ID, AccountID, SubID, IsAdult 
                                   FROM MitID_Account;";

            using (SqlConnection con = new SqlConnection(_connectionString))
            using (SqlCommand readCommand = new SqlCommand(queryString, con))
            {
                await con.OpenAsync();

                using (SqlDataReader reader = await readCommand.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        MitID_Account mitID_Account = MapMitIdAccount(reader);
                        mitIdAccounts.Add(mitID_Account);
                    }
                }
            }

            return mitIdAccounts;
        }

        public async Task<bool> UpdateMitIdAccount(MitID_Account mAcc)
        {
            bool isUpdated = false;

            string queryString = @"UPDATE MitID_Account 
                                   SET AccountID = @AccountID, 
                                       SubID = @SubID, 
                                       IsAdult = @IsAdult 
                                   WHERE ID = @ID;";

            using (SqlConnection con = new SqlConnection(_connectionString))
            using (SqlCommand updateCommand = new SqlCommand(queryString, con))
            {
                await con.OpenAsync();

                updateCommand.Parameters.AddWithValue("@AccountID", mAcc.AccountID);
                updateCommand.Parameters.AddWithValue("@SubID", mAcc.SubID);
                updateCommand.Parameters.AddWithValue("@IsAdult", mAcc.IsAdult);
                updateCommand.Parameters.AddWithValue("@ID", mAcc.ID);

                int rowsAffected = await updateCommand.ExecuteNonQueryAsync();
                isUpdated = rowsAffected > 0;
            }

            return isUpdated;
        }

        public async Task<bool> DeleteMitIdAccount(Guid id)
        {
            bool isDeleted = false;

            string queryString = @"DELETE FROM MitID_Account 
                                   WHERE ID = @ID;";

            using (SqlConnection con = new SqlConnection(_connectionString))
            using (SqlCommand deleteCommand = new SqlCommand(queryString, con))
            {
                deleteCommand.Parameters.AddWithValue("@ID", id);
                await con.OpenAsync();

                int rowsAffected = await deleteCommand.ExecuteNonQueryAsync();
                isDeleted = rowsAffected > 0;
            }

            return isDeleted;
        }

        
        public Task<MitID_Account?> GetByNationalId(string nationalId)
        {
            return GetMitIdAccountBySubId(nationalId);
        }
        private MitID_Account MapMitIdAccount(SqlDataReader reader)
        {
            return new MitID_Account(
                reader.GetGuid(reader.GetOrdinal("ID")),
                reader.GetGuid(reader.GetOrdinal("AccountID")),
                reader.GetString(reader.GetOrdinal("SubID")),
                reader.GetBoolean(reader.GetOrdinal("IsAdult"))
            );
        }
    }
}

