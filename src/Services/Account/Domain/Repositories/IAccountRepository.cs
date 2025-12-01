using Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Repositories
{
    /// <summary>
    /// Defines the contract for account data access.
    /// This interface belongs to the Domain layer and is implemented by the Infrastructure layer.
    /// </summary>
    public interface IAccountRepository
    {
        /// <summary>
        /// Checks if an email already exists in the system.
        /// </summary>
        Task<bool> EmailExistsAsync(string email, CancellationToken ct = default);

        /// <summary>
        /// Persists a new account entity.
        /// </summary>
        Task<Account> CreateAsync(Account account, CancellationToken ct = default);

        /// <summary>
        /// Retrieves an account by its unique identifier.
        /// </summary>
        Task<Account?> GetByIdAsync(Guid id, CancellationToken ct = default);

        /// <summary>
        /// Retrieves an account by its email address.
        /// </summary>
        Task<Account?> GetByEmailAsync(string email, CancellationToken ct = default);

        /// <summary>
        /// Updates an existing account entity.
        /// </summary>
        Task UpdateAsync(Account account, CancellationToken ct = default);
    }
}
