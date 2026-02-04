using Microsoft.JSInterop;

namespace Wallet.Wasm.Services;

/// <summary>
/// Manages the wallet's cryptographic secret used for zero-knowledge proofs.
/// The secret is stored securely in IndexedDB and never exposed in localStorage.
/// </summary>
/// <remarks>
/// Security considerations:
/// - Secret is 256-bit random value generated using crypto.getRandomValues
/// - Stored in IndexedDB (session-based, not persisted to disk by default)
/// - Never logged or exposed in plaintext
/// - Used only for computing commitments and generating ZKP proofs
/// </remarks>
public class SecretManager
{
    private readonly IJSRuntime _js;
    private string? _cachedSecret;

    public SecretManager(IJSRuntime js)
    {
        _js = js;
    }

    /// <summary>
    /// Get or create the wallet secret.
    /// If no secret exists, generates a new 256-bit random secret and stores it.
    /// </summary>
    /// <returns>Hex-encoded wallet secret (64 characters)</returns>
    public async Task<string> GetOrCreateSecretAsync()
    {
        if (_cachedSecret != null)
        {
            return _cachedSecret;
        }

        try
        {
            _cachedSecret = await _js.InvokeAsync<string>("secretManager.getOrCreateSecret");
            
            if (string.IsNullOrWhiteSpace(_cachedSecret))
            {
                throw new InvalidOperationException("Failed to generate wallet secret");
            }

            return _cachedSecret;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Failed to get or create wallet secret", ex);
        }
    }

    /// <summary>
    /// Get the existing wallet secret without creating a new one.
    /// </summary>
    /// <returns>Hex-encoded wallet secret, or null if not exists</returns>
    public async Task<string?> GetSecretAsync()
    {
        if (_cachedSecret != null)
        {
            return _cachedSecret;
        }

        try
        {
            _cachedSecret = await _js.InvokeAsync<string?>("secretManager.getSecret");
            return _cachedSecret;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SecretManager] Error getting secret: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Compute the subject commitment: Poseidon(walletSecret)
    /// This commitment binds credentials to the wallet without revealing the secret.
    /// </summary>
    /// <returns>Commitment as decimal string</returns>
    public async Task<string> ComputeCommitmentAsync()
    {
        var secret = await GetOrCreateSecretAsync();
        
        try
        {
            var commitment = await _js.InvokeAsync<string>("poseidon.computeCommitment", secret);
            
            if (string.IsNullOrWhiteSpace(commitment))
            {
                throw new InvalidOperationException("Failed to compute commitment");
            }

            return commitment;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Failed to compute commitment from secret", ex);
        }
    }

    /// <summary>
    /// Compute challenge hash: Poseidon(challenge)
    /// Used for replay protection in ZKP proofs.
    /// </summary>
    /// <param name="challenge">Challenge string from verifier</param>
    /// <returns>Challenge hash as decimal string</returns>
    public async Task<string> ComputeChallengeHashAsync(string challenge)
    {
        try
        {
            var challengeHash = await _js.InvokeAsync<string>("poseidon.computeChallengeHash", challenge);
            
            if (string.IsNullOrWhiteSpace(challengeHash))
            {
                throw new InvalidOperationException("Failed to compute challenge hash");
            }

            return challengeHash;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to compute challenge hash for: {challenge}", ex);
        }
    }

    /// <summary>
    /// Compute policy hash: Poseidon(policyId)
    /// Used to bind proofs to specific policies.
    /// </summary>
    /// <param name="policyId">Policy identifier (e.g., "age_over_18")</param>
    /// <returns>Policy hash as decimal string</returns>
    public async Task<string> ComputePolicyHashAsync(string policyId)
    {
        try
        {
            var policyHash = await _js.InvokeAsync<string>("poseidon.computePolicyHash", policyId);
            
            if (string.IsNullOrWhiteSpace(policyHash))
            {
                throw new InvalidOperationException("Failed to compute policy hash");
            }

            return policyHash;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to compute policy hash for: {policyId}", ex);
        }
    }

    /// <summary>
    /// Delete the wallet secret (for testing/reset purposes only).
    /// WARNING: This will invalidate all credentials bound to this secret.
    /// </summary>
    public async Task DeleteSecretAsync()
    {
        try
        {
            await _js.InvokeVoidAsync("secretManager.deleteSecret");
            _cachedSecret = null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SecretManager] Error deleting secret: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Clear the cached secret (does not delete from IndexedDB).
    /// </summary>
    public void ClearCache()
    {
        _cachedSecret = null;
    }
}
