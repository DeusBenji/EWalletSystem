using Blazored.LocalStorage;
using Wallet.Wasm.Models;

namespace Wallet.Wasm.Services;

public class WalletStorage
{
    private readonly ILocalStorageService _localStorage;

    private const string TokensStorageKey = "my_wallet_tokens";
    private const string AccountIdStorageKey = "my_wallet_account_id";

    // NEW: store JWT for authorized API calls (e.g. POST /api/tokens)
    private const string JwtStorageKey = "my_wallet_jwt";

    public WalletStorage(ILocalStorageService localStorage)
    {
        _localStorage = localStorage;
    }

    // -------------------------
    // AccountId (MVP "session")
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
        await ClearJwtAsync();

        if (clearTokens)
        {
            await _localStorage.RemoveItemAsync(TokensStorageKey);
        }
    }

    // -------------------------
    // JWT (Auth token for calling backend)
    // -------------------------
    public async Task SetJwtAsync(string jwt)
    {
        if (string.IsNullOrWhiteSpace(jwt))
            throw new ArgumentException("JWT cannot be empty.", nameof(jwt));

        await _localStorage.SetItemAsync(JwtStorageKey, jwt);
    }

    public async Task<string?> GetJwtAsync()
    {
        var jwt = await _localStorage.GetItemAsync<string>(JwtStorageKey);
        return string.IsNullOrWhiteSpace(jwt) ? null : jwt;
    }

    public async Task ClearJwtAsync()
    {
        await _localStorage.RemoveItemAsync(JwtStorageKey);
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
