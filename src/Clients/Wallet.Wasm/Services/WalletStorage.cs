using Blazored.LocalStorage;
using Wallet.Wasm.Models;

namespace Wallet.Wasm.Services;

public class WalletStorage
{
    private readonly ILocalStorageService _localStorage;
    private const string StorageKey = "my_wallet_tokens";

    public WalletStorage(ILocalStorageService localStorage)
    {
        _localStorage = localStorage;
    }

    public async Task SaveTokenAsync(LocalWalletToken token)
    {
        var tokens = await GetTokensAsync();
        // Remove existing if any (update)
        tokens.RemoveAll(t => t.TokenId == token.TokenId);
        tokens.Add(token);
        
        await _localStorage.SetItemAsync(StorageKey, tokens);
    }

    public async Task<List<LocalWalletToken>> GetTokensAsync()
    {
        var tokens = await _localStorage.GetItemAsync<List<LocalWalletToken>>(StorageKey);
        return tokens ?? new List<LocalWalletToken>();
    }

    public async Task RemoveTokenAsync(string tokenId)
    {
        var tokens = await GetTokensAsync();
        var count = tokens.RemoveAll(t => t.TokenId == tokenId);
        
        if (count > 0)
        {
            await _localStorage.SetItemAsync(StorageKey, tokens);
        }
    }
}
