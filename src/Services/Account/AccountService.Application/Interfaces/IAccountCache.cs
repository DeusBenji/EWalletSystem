using Application.DTOs;

namespace Application.Interfaces
{
    public interface IAccountCache
    {
        Task<AccountDto?> GetAccountAsync(Guid id, CancellationToken ct = default);
        Task SetAccountAsync(AccountDto dto, CancellationToken ct = default);
        Task InvalidateAsync(Guid id, CancellationToken ct = default);
    }
}
