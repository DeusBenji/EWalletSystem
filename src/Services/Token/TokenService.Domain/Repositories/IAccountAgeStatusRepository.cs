using Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Repositories
{
    public interface IAccountAgeStatusRepository
    {
        Task<AccountAgeStatus?> GetAsync(Guid accountId, CancellationToken ct = default);
        Task SaveAsync(AccountAgeStatus status, CancellationToken ct = default);
    }
}