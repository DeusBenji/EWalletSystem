using System.Security.Cryptography;
using System.Text;
using Wallet.Wasm.Models;

namespace Wallet.Wasm.Utilities;

public static class TokenCrypto
{
    public static bool MatchesOnChainAnchor(LocalWalletToken token)
    {
        if (string.IsNullOrEmpty(token.AnchorHashOnChain)) return false;
        
        // In a real app, this would query the blockchain to verify the anchor exists.
        // For the demo, we just verify the local integrity matches the "anchor" property.
        
        var computedHash = token.ComputeHash();
        return computedHash.Equals(token.AnchorHashOnChain, StringComparison.OrdinalIgnoreCase);
    }
}
