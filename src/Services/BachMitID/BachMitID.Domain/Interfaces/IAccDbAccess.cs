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
        Task<Account?> GetAccountById(Guid accountId);
        Task<Guid> CreateAccount(Account acc);
        Task<List<Account>> GetAccounts();
        Task<bool> DeleteAccount(Guid accountId);
        Task<bool> UpdateAccount(Account acc);


    }
}
