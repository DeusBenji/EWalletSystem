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

    public async Task ImportTokenAsync(string tokenId, string type, string issuer)
    {
        // SIMULATION: In a real app, this would call the API to get a signed VC.
        // Here we simulate fetching/creating a valid token.
        
        // Mock backend call simulation
        await Task.Delay(500); // Simulate network

        if (string.IsNullOrWhiteSpace(tokenId)) tokenId = Guid.NewGuid().ToString();

        var token = new LocalWalletToken
        {
            TokenId = tokenId,
            Type = type,
            Issuer = issuer,
            IssuedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddYears(1),
            Claims = new Dictionary<string, string>
            {
                { "Age", "25" }, // Mock Claim
                { "Name", "John Doe" }
            }
        };

        // Simulate a backend signature (hash of the content + secret)
        // In reality, this signature comes from the server.
        // For off-chain verification demo, we might just store the hash.
        var dataToSign = token.ComputeHash(); 
        var mockSignature = $"signed-{dataToSign}-valid"; 

        // Update token with mock anchor
        var signedToken = token with { 
            Signature = mockSignature, 
            AnchorHashOnChain = dataToSign // Demo match
        };

        await _storage.SaveTokenAsync(signedToken);
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
