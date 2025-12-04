using System;
using System.Threading;
using System.Threading.Tasks;
using Application.DTOs;

namespace Application.Interfaces
{
    public interface IAccountService
    {
        Task<AccountDto> RegisterAccountAsync(RegisterAccountDto dto, CancellationToken ct = default);

        Task<AccountDto?> GetAccountByIdAsync(Guid id, CancellationToken ct = default);
        Task<AccountDto?> GetAccountByEmailAsync(string email, CancellationToken ct = default);
        Task<AuthenticateAccountResult> AuthenticateAsync(string email, string password, CancellationToken ct = default);

        Task<bool> ChangePasswordAsync(Guid accountId, string oldPassword, string newPassword, CancellationToken ct = default);
        Task<bool> RequestEmailChangeAsync(Guid accountId, string newEmail, CancellationToken ct = default);
        Task<bool> ConfirmEmailChangeAsync(string token, CancellationToken ct = default);

        Task<bool> DeactivateAccountAsync(Guid accountId, CancellationToken ct = default);
    }
}
