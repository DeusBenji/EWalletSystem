using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Wallet.Wasm.Models;

namespace Wallet.Wasm.Services;

public class WalletService
{
    private readonly WalletStorage _storage;
    private readonly HttpClient _http;

    public WalletService(WalletStorage storage, HttpClient http)
    {
        _storage = storage;
        _http = http;
    }

    public async Task<List<LocalWalletToken>> GetMyTokensAsync()
    {
        return await _storage.GetTokensAsync();
    }

    public async Task<LocalWalletToken?> GetLocalTokenAsync()
    {
        var tokens = await GetMyTokensAsync();
        return tokens.FirstOrDefault(t => t.Type == "AdultCredential");
    }

    /// <summary>
    /// Issues an AdultCredential from TokenService (POST api/tokens) and imports it into local wallet storage.
    /// </summary>
    public async Task IssueAndImportAdultTokenAsync()
    {
        var accountId = await _storage.GetAccountIdAsync();
        if (accountId == Guid.Empty)
            throw new InvalidOperationException("No AccountId found. Please create an account first.");

        var jwt = await _storage.GetJwtAsync();
        if (string.IsNullOrWhiteSpace(jwt))
            throw new UnauthorizedAccessException("Missing JWT. Please log in first.");

        _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwt);

        var req = new IssueTokenRequestContract { AccountId = accountId };

        HttpResponseMessage resp;
        try
        {
            resp = await _http.PostAsJsonAsync("token/api/tokens", req);

        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed calling token service: {ex.Message}");
        }

        if (resp.StatusCode == HttpStatusCode.Unauthorized)
            throw new UnauthorizedAccessException("Unauthorized. Please log in again.");

        if (!resp.IsSuccessStatusCode)
        {
            if ((int)resp.StatusCode == 403 || (int)resp.StatusCode == 400 || (int)resp.StatusCode == 500)
                throw new InvalidOperationException("Account is not MitID verified as 18+ (cannot issue token).");

            var body = await resp.Content.ReadAsStringAsync();
            throw new InvalidOperationException($"Token service error ({(int)resp.StatusCode}): {body}");
        }

        var issued = await resp.Content.ReadFromJsonAsync<IssueTokenResponseContract>();
        if (issued is null || string.IsNullOrWhiteSpace(issued.Token))
            throw new InvalidOperationException("Token service returned an empty token.");

        // Store minimal claims without parsing JWT in WASM (avoids Mono runtime crash)
        var claims = new Dictionary<string, string>
        {
            ["VC_JWT"] = issued.Token,
            ["AccountId"] = accountId.ToString(),
            ["IssuedAt"] = issued.IssuedAt.ToString("o"),
            ["ExpiresAt"] = issued.ExpiresAt.ToString("o"),
            ["Type"] = "AdultCredential"
        };

        await ImportTokenAsync(
            tokenId: Guid.NewGuid().ToString(),
            type: "AdultCredential",
            issuer: "TokenService",
            customClaims: claims,
            issuedAt: issued.IssuedAt,
            expiresAt: issued.ExpiresAt
        );
    }

    public async Task ImportTokenAsync(
        string tokenId,
        string type,
        string issuer,
        Dictionary<string, string>? customClaims = null,
        DateTime? issuedAt = null,
        DateTime? expiresAt = null)
    {
        if (customClaims == null || !customClaims.Any())
            throw new ArgumentException("Claims are required to import a token.");

        if (type == "AdultCredential")
        {
            var existingTokens = await GetMyTokensAsync();
            var tokensToRemove = existingTokens.Where(t => t.Type == "AdultCredential").ToList();
            foreach (var t in tokensToRemove)
            {
                await _storage.RemoveTokenAsync(t.TokenId);
            }
        }

        if (string.IsNullOrWhiteSpace(tokenId))
            tokenId = Guid.NewGuid().ToString();

        var token = new LocalWalletToken
        {
            TokenId = tokenId,
            Type = type,
            Issuer = issuer,
            IssuedAt = issuedAt ?? DateTime.UtcNow,
            ExpiresAt = expiresAt ?? DateTime.UtcNow.AddYears(1),
            Claims = customClaims
        };

        var dataToSign = token.ComputeHash();
        var mockSignature = $"signed-{dataToSign}-valid";

        var signedToken = token with
        {
            Signature = mockSignature,
            AnchorHashOnChain = dataToSign
        };

        await _storage.SaveTokenAsync(signedToken);
    }

    public async Task DeleteTokenAsync(string tokenId)
    {
        await _storage.RemoveTokenAsync(tokenId);
    }

    public bool VerifyTokenLocally(LocalWalletToken token)
    {
        if (token.IsExpired) return false;

        var hash = token.ComputeHash();
        return !string.IsNullOrEmpty(token.Signature) && token.Signature.Contains(hash);
    }

    public async Task<bool> VerifyTokenOnServerAsync(LocalWalletToken token)
    {
        await Task.Delay(800);
        return VerifyTokenLocally(token);
    }

    private sealed class IssueTokenRequestContract
    {
        public Guid AccountId { get; set; }
    }

    private sealed class IssueTokenResponseContract
    {
        public string Token { get; set; } = default!;
        public DateTime IssuedAt { get; set; }
        public DateTime ExpiresAt { get; set; }
    }
}
