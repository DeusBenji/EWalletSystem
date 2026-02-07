using System.Net.Http.Json;
using Wallet.Wasm.Models;

namespace Wallet.Wasm.Services;

public class WalletService
{
    private readonly WalletStorage _storage;
    private readonly HttpClient _http;
    private readonly IZkpProverService _zkp;

    public WalletService(WalletStorage storage, HttpClient http, IZkpProverService zkp)
    {
        _storage = storage;
        _http = http;
        _zkp = zkp;
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
        try 
        {
            // 1. Generate ZKP Proof (Client-side)
            // In a real flow, we get a challenge from the server first or generate one.
            // For MVP, we presume the server accepts a self-generated challenge or we use a fixed context.
            var challenge = Guid.NewGuid().ToString(); 
            var proofJson = await _zkp.GenerateAgeProofAsync(token, challenge);

            // 2. Send to API
            // The API expects { VcJwt: string } where string can now be the ZKP JSON
            var payload = new { VcJwt = proofJson };
            var response = await _http.PostAsJsonAsync("api/Validation/verify", payload);

            if (!response.IsSuccessStatusCode)
            {
                // Log/Handle error
                return false;
            }

            var result = await response.Content.ReadFromJsonAsync<VerifyCredentialResponse>();
            return result?.Success ?? false;
        }
        catch
        {
            return false;
        }
    }
}

// Helper class for JSON deserialization
public class VerifyCredentialResponse
{
    public bool Success { get; set; }
    public string? FailureReason { get; set; }
}
