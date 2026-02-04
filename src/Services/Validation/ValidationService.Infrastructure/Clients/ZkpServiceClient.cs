using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;

namespace ValidationService.Infrastructure.Clients
{
    public interface IZkpServiceClient
    {
        /// <summary>
        /// Verify age-specific ZKP proof (legacy)
        /// </summary>
        Task<bool> VerifyAgeV1Async(string proofHex, string currentYear, string commitment, string challengeHash, CancellationToken ct);
        
        /// <summary>
        /// Verify policy-based ZKP proof (universal)
        /// </summary>
        Task<bool> VerifyPolicyV1Async(string proofHex, string challengeHash, string policyHash, string subjectCommitment, string sessionTag, CancellationToken ct);
        
        /// <summary>
        /// Compute Poseidon hash of challenge (replay protection)
        /// </summary>
        Task<string?> GetChallengeHashAsync(string challenge, CancellationToken ct);
        
        /// <summary>
        /// Compute Poseidon hash of policy ID (policy binding)
        /// </summary>
        Task<string?> GetPolicyHashAsync(string policyId, CancellationToken ct);
    }

    public class ZkpServiceClient : IZkpServiceClient
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<ZkpServiceClient> _logger;

        public ZkpServiceClient(HttpClient httpClient, ILogger<ZkpServiceClient> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task<bool> VerifyAgeV1Async(string proofHex, string currentYear, string commitment, string challengeHash, CancellationToken ct)
        {
            var payload = new 
            {
                proof = ConvertHexToByteArray(proofHex), // Go service expects []byte for proof? Or hex string? 
                // The Go service model says: Proof []byte `json:"proof"`. 
                // Wait, typically we send Base64 in JSON for []byte.
                // If proofHex is hex, we should convert to byte array.
                // Go's encoding/json unmarshals base64 strings into []byte automatically.
                // So if we send a byte array in C# via System.Net.Http.Json (System.Text.Json), it serializes as Base64 string. Perfect.

                publicInputs = new 
                {
                    currentYear = currentYear,
                    commitment = commitment,
                    challengeHash = challengeHash
                }
            };
            
            // Log payload for debugging (redacted logic applies closer to logging sink, but here we log structure)
            _logger.LogDebug("Calling ZKP Service verify-age-v1");

            var response = await _httpClient.PostAsJsonAsync("/verify/age-v1", payload, ct);
            
            if (!response.IsSuccessStatusCode)
            {
                var err = await response.Content.ReadAsStringAsync(ct);
                _logger.LogError("ZKP Service failed: {StatusCode} {Error}", response.StatusCode, err);
                return false;
            }

            var result = await response.Content.ReadFromJsonAsync<VerifyResponse>(cancellationToken: ct);
            return result?.Valid ?? false;
        }

        public async Task<bool> VerifyPolicyV1Async(string proofHex, string challengeHash, string policyHash, string subjectCommitment, string sessionTag, CancellationToken ct)
        {
            var payload = new 
            {
                proof = ConvertHexToByteArray(proofHex),
                publicInputs = new 
                {
                    challengeHash = challengeHash,
                    policyHash = policyHash,
                    subjectCommitment = subjectCommitment,
                    sessionTag = sessionTag
                }
            };
            
            _logger.LogDebug("Calling ZKP Service verify-policy-v1");

            var response = await _httpClient.PostAsJsonAsync("/verify/policy-v1", payload, ct);
            
            if (!response.IsSuccessStatusCode)
            {
                var err = await response.Content.ReadAsStringAsync(ct);
                _logger.LogError("ZKP Service policy verification failed: {StatusCode} {Error}", response.StatusCode, err);
                return false;
            }

            var result = await response.Content.ReadFromJsonAsync<VerifyResponse>(cancellationToken: ct);
            return result?.Valid ?? false;
        }

        public async Task<string?> GetChallengeHashAsync(string challenge, CancellationToken ct)
        {
            // Challenge is assumed to be a decimal string of a BigInt (e.g. from a random 128-bit number)
            // If it's a GUID or random string, we need to convert it to BigInt decimal string first?
            // For now, assume the Challenge string passed here IS the BigInt string representation 
            // OR we convert it here.
            
            // To be safe: If challenge is hex or random chars, we should treat it as BigInt.
            // Let's assume the input 'challenge' from VerificationRequest is the nonce.
            // Converting arbitrary string to BigInt: BigInteger.Parse or from bytes.
            
            // Simplified: Assume strictly numeric string for now, or HEX.
            // Let's pass it as is, assuming the caller has formatted it or it's a numeric nonce.
            
            var payload = new { input = challenge };
            var response = await _httpClient.PostAsJsonAsync("/utils/hash", payload, ct);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("ZKP Hash failed: {StatusCode}", response.StatusCode);
                return null;
            }

            var result = await response.Content.ReadFromJsonAsync<HashResponse>(cancellationToken: ct);
            return result?.Hash;
        }

        public async Task<string?> GetPolicyHashAsync(string policyId, CancellationToken ct)
        {
            // Convert policyId string to numeric representation for hashing
            // For now, use simple character code sum (same as in JS)
            var policyIdNumeric = policyId.Sum(c => (long)c).ToString();
            
            var payload = new { input = policyIdNumeric };
            var response = await _httpClient.PostAsJsonAsync("/utils/hash", payload, ct);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("ZKP Policy Hash failed: {StatusCode}", response.StatusCode);
                return null;
            }

            var result = await response.Content.ReadFromJsonAsync<HashResponse>(cancellationToken: ct);
            return result?.Hash;
        }

        private byte[] ConvertHexToByteArray(string hex)
        {
           if (string.IsNullOrEmpty(hex)) return Array.Empty<byte>();
           return Convert.FromHexString(hex);
        }

        private class VerifyResponse 
        {
            [JsonPropertyName("valid")]
            public bool Valid { get; set; }
            
            [JsonPropertyName("error")]
            public string? Error { get; set; }
        }

        private class HashResponse
        {
            [JsonPropertyName("hash")]
            public string Hash { get; set; } = string.Empty;
        }
    }
}
