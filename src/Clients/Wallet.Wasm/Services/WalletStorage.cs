using Blazored.LocalStorage;
using Wallet.Wasm.Models;

namespace Wallet.Wasm.Services;

public class WalletStorage
{
    private readonly ILocalStorageService _localStorage;

    private const string TokensStorageKey = "my_wallet_tokens";
    private const string AccountIdStorageKey = "my_wallet_account_id";

    public WalletStorage(ILocalStorageService localStorage)
    {
        _localStorage = localStorage;
    }

    // -------------------------
    // AccountId (for MitID flow)
    // -------------------------
    public async Task SetAccountIdAsync(Guid accountId)
    {
        await _localStorage.SetItemAsync(AccountIdStorageKey, accountId.ToString());
    }

    public async Task<Guid> GetAccountIdAsync()
    {
        var value = await _localStorage.GetItemAsync<string>(AccountIdStorageKey);

        return Guid.TryParse(value, out var id) ? id : Guid.Empty;
    }

    public async Task ClearAccountIdAsync()
    {
        await _localStorage.RemoveItemAsync(AccountIdStorageKey);
    }

    // -------------------------
    // Tokens
    // -------------------------
    public async Task SaveTokenAsync(LocalWalletToken token)
    {
        var tokens = await GetTokensAsync();
        tokens.RemoveAll(t => t.TokenId == token.TokenId);
        tokens.Add(token);

        await _localStorage.SetItemAsync(TokensStorageKey, tokens);
    }

    public async Task<List<LocalWalletToken>> GetTokensAsync()
    {
        var tokens = await _localStorage.GetItemAsync<List<LocalWalletToken>>(TokensStorageKey);
        return tokens ?? new List<LocalWalletToken>();
    }

    public async Task RemoveTokenAsync(string tokenId)
    {
        var tokens = await GetTokensAsync();
        var count = tokens.RemoveAll(t => t.TokenId == tokenId);

        if (count > 0)
        {
            await _localStorage.SetItemAsync(TokensStorageKey, tokens);
        }
    }
}
