using System;
using System.Threading.Tasks;

namespace IdentityService.Application.BusinessLogicLayer.Interface
{
    public interface IAccountSyncService
    {
        Task SyncAccountAsync(Guid accountId, string email);
    }
}
