using Application.Interfaces;
using Domain.Repositories;
using Microsoft.Extensions.Logging;

public class MitIdVerifiedService : IMitIdVerifiedService
{
    private readonly IAccountRepository _accountRepository;
    private readonly ILogger<MitIdVerifiedService> _logger;

    public MitIdVerifiedService(IAccountRepository accountRepository, ILogger<MitIdVerifiedService> logger)
    {
        _accountRepository = accountRepository;
        _logger = logger;
    }

    public async Task HandleMitIdVerifiedAsync(Guid accountId, bool isAdult)
    {
        var account = await _accountRepository.GetByIdAsync(accountId);
        if (account == null)
        {
            _logger.LogWarning(
                "MitIdVerified received for non-existing AccountId {AccountId}. Event ignored.",
                accountId);

            return;
        }

        account.MarkMitIdVerified(isAdult);
        await _accountRepository.UpdateAsync(account);
    }
}
