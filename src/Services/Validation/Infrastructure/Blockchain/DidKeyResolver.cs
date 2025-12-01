using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.IdentityModel.Tokens;
using ValidationService.Application.Interfaces;
using Application.Interfaces; // For IFabricLookupClient and ICacheService
using Domain.Models;

namespace Infrastructure.Blockchain
{
    public class DidKeyResolver : IDidKeyResolver
    {
        private readonly IFabricLookupClient _fabricClient;
        private readonly ICacheService _cache;
        
        public DidKeyResolver(IFabricLookupClient fabricClient, ICacheService cache)
        {
            _fabricClient = fabricClient;
            _cache = cache;
        }
        
        public async Task<SecurityKey?> ResolvePublicKeyAsync(string issuerDid, CancellationToken ct = default)
        {
            // Check cache first
            var cacheKey = $"did-key:{issuerDid}";
            var cached = await _cache.GetAsync<string>(cacheKey, ct);
            if (cached != null)
            {
                // return DeserializeKey(cached);
                // Simplified: if cached, we assume it's the one we know
                // In real impl, we would deserialize the JWK/PEM
            }
            
            // Resolve DID document
            var didDoc = await _fabricClient.ResolveDidAsync(issuerDid, ct);
            if (didDoc == null)
            {
                // Fallback for development: try to read from file if DID matches
                if (issuerDid.Contains("bachelor-token-service"))
                {
                    return await LoadDevKeyAsync(ct);
                }
                return null;
            }
            
            // In real implementation: fetch actual public key from verification method
            // For now: extract from DID document (assuming publicKeyJwk or publicKeyBase58)
            
            // TODO: Parse actual key from DID document
            // This is simplified - you'd need to handle publicKeyJwk/publicKeyBase58
            
            // Cache for 10 minutes
            // await _cache.SetAsync(cacheKey, "key-data", TimeSpan.FromMinutes(10));
            
            // Fallback for development
            return await LoadDevKeyAsync(ct);
        }
        
        private async Task<SecurityKey?> LoadDevKeyAsync(CancellationToken ct)
        {
             // Try to find the key file
             var paths = new[]
             {
                 "../../Token/TokenService/keys/issuer-public.pem",
                 "../../../Token/TokenService/keys/issuer-public.pem",
                 "../../../../Token/TokenService/keys/issuer-public.pem",
                 "e:/repos/bachelorprojekt/Token/TokenService/keys/issuer-public.pem"
             };

             foreach (var path in paths)
             {
                 if (File.Exists(path))
                 {
                     var pem = await File.ReadAllTextAsync(path, ct);
                     var rsa = RSA.Create();
                     rsa.ImportFromPem(pem);
                     return new RsaSecurityKey(rsa);
                 }
             }
             
             return null;
        }
    }
}
