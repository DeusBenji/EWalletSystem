using System; // Basic
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Wallet.Wasm.Models;

namespace Wallet.Wasm.Services;

public class WalletStorage
{
    // In-memory storage (session only)
    private Guid _accountId = Guid.Empty;
    private readonly List<LocalWalletToken> _tokens = new();

    public WalletStorage()
    {
        // No dependencies needed for in-memory
    }

    // -------------------------
    // AccountId (MVP "session")
    // -------------------------
    public Task SetAccountIdAsync(Guid accountId)
    {
        _accountId = accountId;
        return Task.CompletedTask;
    }

    public Task<Guid> GetAccountIdAsync()
    {
        return Task.FromResult(_accountId);
    }

    public Task ClearAccountIdAsync()
    {
        _accountId = Guid.Empty;
        return Task.CompletedTask;
    }

    public async Task<bool> IsLoggedInAsync()
    {
        var id = await GetAccountIdAsync();
        return id != Guid.Empty;
    }

    /// <summary>
    /// MVP logout: clear session + optional clear wallet tokens (recommended).
    /// </summary>
    public async Task LogoutAsync(bool clearTokens = true)
    {
        await ClearAccountIdAsync();

        if (clearTokens)
        {
            _tokens.Clear();
        }
    }

    // -------------------------
    // Tokens
    // -------------------------
    public Task SaveTokenAsync(LocalWalletToken token)
    {
        _tokens.RemoveAll(t => t.TokenId == token.TokenId);
        _tokens.Add(token);
        return Task.CompletedTask;
    }

    public Task<List<LocalWalletToken>> GetTokensAsync()
    {
        // Return a copy to mimic storage behavior (isolation)
        return Task.FromResult(_tokens.ToList());
    }

    public Task RemoveTokenAsync(string tokenId)
    {
        _tokens.RemoveAll(t => t.TokenId == tokenId);
        return Task.CompletedTask;
    }
}
