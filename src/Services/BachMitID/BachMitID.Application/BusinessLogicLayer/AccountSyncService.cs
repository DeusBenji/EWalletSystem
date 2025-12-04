using System;
using System.Threading.Tasks;
using BachMitID.Application.BusinessLogicLayer.Interface;
using BachMitID.Domain.Interfaces;
using BachMitID.Domain.Model;

namespace BachMitID.Application.BusinessLogicLayer
{
    /// <summary>
    /// Bruges af Kafka-consumers til at sync'e Account-data ind i BachMitID's egen database.
    /// </summary>
    public class AccountSyncService : IAccountSyncService
    {
        private readonly IAccDbAccess _accDbAccess;

        public AccountSyncService(IAccDbAccess accDbAccess)
        {
            _accDbAccess = accDbAccess;
        }

        public async Task SyncAccountAsync(Guid accountId, string email)
        {
            // 1) Tjek om account allerede findes (idempotent)
            var existing = await _accDbAccess.GetAccountByIdAsync(accountId);
            if (existing != null)
            {
                // Allerede sync'et – gør ikke noget
                return;
            }

            // 2) Opret en ny account i BachMitID's egen database
            var account = new Account(
                id: accountId,
                email: email
            );

            await _accDbAccess.CreateAccountAsync(account);
        }
    }
}
