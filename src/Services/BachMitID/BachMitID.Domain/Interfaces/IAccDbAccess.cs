using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BachMitID.Domain.Model;

namespace BachMitID.Domain.Interfaces
{
    public interface IAccDbAccess
    {
        Task<Account?> GetAccountByIdAsync(Guid accountId);
        Task<Guid> CreateAccountAsync(Account acc);
        Task<List<Account>> GetAccountsAsync();
        Task<bool> DeleteAccountAsync(Guid accountId);
        Task<bool> UpdateAccountAsync(Account acc);


    }
}
