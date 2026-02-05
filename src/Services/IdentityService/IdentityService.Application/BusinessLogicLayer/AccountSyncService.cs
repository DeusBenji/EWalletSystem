using System;
using System.Threading.Tasks;
using IdentityService.Application.BusinessLogicLayer.Interface;
using IdentityService.Domain.Interfaces;
using IdentityService.Domain.Model;

namespace IdentityService.Application.BusinessLogicLayer
{
    /// <summary>
    /// Bruges af Kafka-consumers til at sync'e Account-data ind i IdentityService's egen database.
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

            // 2) Opret en ny account i IdentityService's egen database
            var account = new Account(
                id: accountId,
                email: email
            );

            await _accDbAccess.CreateAccountAsync(account);
        }
    }
}
