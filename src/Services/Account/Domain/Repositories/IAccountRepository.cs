using Domain.Models;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Domain.Repositories
{
    public interface IAccountRepository
    {
        Task<bool> EmailExistsAsync(string email, CancellationToken ct = default);
        Task<Account> CreateAsync(Account account, CancellationToken ct = default);
        Task<Account?> GetByIdAsync(Guid id, CancellationToken ct = default);
        Task<Account?> GetByEmailAsync(string email, CancellationToken ct = default);
        Task UpdateAsync(Account account, CancellationToken ct = default);
    }
}
