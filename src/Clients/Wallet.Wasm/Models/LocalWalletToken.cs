using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Wallet.Wasm.Models;

public record LocalWalletToken
{
    public string TokenId { get; init; } = Guid.NewGuid().ToString();
    public string Type { get; init; } = "AdultCredential"; // e.g. "AdultCredential"
    public DateTime IssuedAt { get; init; } = DateTime.UtcNow;
    public DateTime ExpiresAt { get; init; } = DateTime.UtcNow.AddYears(1);
    public string Issuer { get; init; } = "BachMitID";
    public string? Signature { get; init; } // The simulated blockchain signature/hash
    public string? RawToken { get; init; } // Raw content for hashing
    public string? AnchorHashOnChain { get; init; } // The hash stored on chain

    // User data (claims)
    public Dictionary<string, string> Claims { get; init; } = new();

    public bool IsExpired => DateTime.UtcNow > ExpiresAt;
    public bool IsActive => !IsExpired;
    public bool IsAgeOver18 => Type == "AdultCredential" || (Claims.ContainsKey("Age") && int.TryParse(Claims["Age"], out int age) && age >= 18);

    /// <summary>
    /// Computes a SHA256 hash of the token's critical fields
    /// </summary>
    public string ComputeHash()
    {
        // Use RawToken if available, otherwise reconstruct
        var data = !string.IsNullOrEmpty(RawToken) ? RawToken : $"{TokenId}|{Type}|{IssuedAt:O}|{ExpiresAt:O}|{Issuer}";
        
        using var sha256 = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(data);
        var hash = sha256.ComputeHash(bytes);
        
        return Convert.ToHexString(hash);
    }
}
