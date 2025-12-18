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

    public async Task ImportTokenAsync(string tokenId, string type, string issuer, Dictionary<string, string>? customClaims = null)
    {
        // Require claims, no more "John Doe" fallback
        if (customClaims == null || !customClaims.Any())
        {
            throw new ArgumentException("Claims are required to import a token.");
        }

        // DUPLICATE PREVENTION:
        // If this is an AdultCredential, ensure we only have ONE.
        // Remove any existing AdultCredential (including old "John Doe" mocks)
        if (type == "AdultCredential")
        {
            var existingTokens = await GetMyTokensAsync();
            var tokensToRemove = existingTokens.Where(t => t.Type == "AdultCredential").ToList();
            foreach (var t in tokensToRemove)
            {
                await _storage.RemoveTokenAsync(t.TokenId);
            }
        }

        // Mock backend call simulation
        await Task.Delay(200); 

        if (string.IsNullOrWhiteSpace(tokenId)) tokenId = Guid.NewGuid().ToString();

        var token = new LocalWalletToken
        {
            TokenId = tokenId,
            Type = type,
            Issuer = issuer,
            IssuedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddYears(1),
            Claims = customClaims
        };

        // Simulate a backend signature (hash of the content + secret)
        var dataToSign = token.ComputeHash(); 
        var mockSignature = $"signed-{dataToSign}-valid"; 

        // Update token with mock anchor
        var signedToken = token with { 
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
        
        // Demo Check: Compute hash and "verify" signature
        var hash = token.ComputeHash();
        
        // Simple mock check: does signature contain the hash?
        if (!string.IsNullOrEmpty(token.Signature) && token.Signature.Contains(hash))
        {
            return true;
        }

        return false;
    }

    public async Task<bool> VerifyTokenOnServerAsync(LocalWalletToken token)
    {
        // 1. Send token to API
        // 2. API checks signature/fabric
        // 3. Return result
        
        // For MVP demo, we assume success if local check passes, 
        // to show "Server-side verification" in UI without full backend implementation here.
        await Task.Delay(800); // Simulate mock roundtrip
        return VerifyTokenLocally(token); 
    }
}
