using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Domain.Models;

namespace Application.Interfaces
{
    public interface IAccountAgeStatusCache
    {
        Task<AccountAgeStatus?> GetAsync(Guid accountId, CancellationToken ct = default);
        Task SetAsync(AccountAgeStatus status, CancellationToken ct = default);
    }
}
