using System.Text.Json;
using Microsoft.JSInterop;
using Wallet.Wasm.Models;

namespace Wallet.Wasm.Services;

/// <summary>
/// Service for generating zero-knowledge proofs using Circom circuits and SnarkJS.
/// Supports both legacy age-specific proofs and universal policy-based proofs.
/// </summary>
public class ZkpProverService
{
    private readonly IJSRuntime _js;
    private readonly SecretManager _secretManager;

    public ZkpProverService(IJSRuntime js, SecretManager secretManager)
    {
        _js = js;
        _secretManager = secretManager;
    }

    /// <summary>
    /// Generate a policy-based zero-knowledge proof (universal approach).
    /// Uses wallet secret + policy circuit for any policy type.
    /// </summary>
    /// <param name="policyId">Policy identifier (e.g., "age_over_18", "lawyer")</param>
    /// <param name="challenge">Challenge from verifier for replay protection</param>
    /// <returns>JSON presentation payload for ValidationService</returns>
    public async Task<string> GeneratePolicyProofAsync(string policyId, string challenge)
    {
        try
        {
            // 1. Get wallet secret
            var walletSecret = await _secretManager.GetOrCreateSecretAsync();
            
            // 2. Compute hashes in C# (will be passed to circuit)
            var challengeHash = await _secretManager.ComputeChallengeHashAsync(challenge);
            var policyHash = await _secretManager.ComputePolicyHashAsync(policyId);
            
            // 3. Prepare circuit inputs
            // Note: Circuit computes subjectCommitment and sessionTag as outputs
            var circuitInput = new
            {
                walletSecret = walletSecret,
                challengeHash = challengeHash,
                policyHash = policyHash
            };
            
            Console.WriteLine($"[ZKP] Generating policy proof for: {policyId}");
            
            // 4. Call SnarkJS via JavaScript interop to generate proof
            var proofResult = await _js.InvokeAsync<JsonElement>(
                "zkpProver.generatePolicyProof",
                circuitInput
            );
            
            // 5. Extract proof and public signals
            var proof = proofResult.GetProperty("proof");
            var publicSignals = proofResult.GetProperty("publicSignals");
            
            // Public signals order (from circuit):
            // [0] = challengeHash (input)
            // [1] = policyHash (input)
            // [2] = subjectCommitment (output)
            // [3] = sessionTag (output)
            
            // 6. Create presentation payload matching PolicyZkpPresentation.cs
            var presentation = new
            {
                presentationType = "policy-zkp-v1",
                vcJwt = "", // Will be populated when we have credential issuance
                proof = JsonSerializer.Serialize(proof),
                publicInputs = new
                {
                    challengeHash = publicSignals[0].GetString(),
                    policyHash = publicSignals[1].GetString(),
                    subjectCommitment = publicSignals[2].GetString(),
                    sessionTag = publicSignals[3].GetString()
                }
            };
            
            Console.WriteLine($"[ZKP] Policy proof generated successfully");
            return JsonSerializer.Serialize(presentation);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ZKP] Policy proof generation failed: {ex.Message}");
            throw new InvalidOperationException($"ZKP policy proof generation failed: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Generate age-specific zero-knowledge proof (legacy approach).
    /// This method is kept for backward compatibility with existing age verification demos.
    /// </summary>
    /// <param name="token">Wallet token containing age credentials</param>
    /// <param name="challenge">Challenge from verifier</param>
    /// <returns>JSON presentation payload</returns>
    public async Task<string> GenerateAgeProofAsync(LocalWalletToken token, string challenge)
    {
        try
        {
            // 1. Extract credential data
            if (!token.Claims.TryGetValue("birthYear", out var birthYearStr) ||
                !token.Claims.TryGetValue("salt", out var saltStr) ||
                !token.Claims.TryGetValue("commitment", out var commitmentStr))
            {
                throw new InvalidOperationException("Token missing required claims for ZKP proof");
            }

            // 2. Compute challenge hash (using Poseidon hash via JS)
            // For now, we'll use a simple numeric challenge
            var challengeNumeric = Math.Abs(challenge.GetHashCode()).ToString();
            
            // In production, compute Poseidon hash of challenge via JS
            var challengeHashStr = await ComputePoseidonHashAsync(challengeNumeric);

            // 3. Prepare circuit inputs
            var circuitInput = new
            {
                birthYear = birthYearStr,
                salt = saltStr,
                commitment = commitmentStr,
                currentYear = DateTime.UtcNow.Year.ToString(),
                challenge = challengeNumeric,
                challengeHash = challengeHashStr
            };

            // 4. Call SnarkJS via JavaScript interop to generate proof
            var proofResult = await _js.InvokeAsync<JsonElement>(
                "zkpProver.generateAgeProof",
                circuitInput
            );

            // 5. Extract proof and public signals
            var proof = proofResult.GetProperty("proof");
            var publicSignals = proofResult.GetProperty("publicSignals");

            // 6. Create presentation payload matching AgeZkpPresentation.cs
            var presentation = new
            {
                verifiableCredential = token.Signature, // VC signature for binding check
                proof = JsonSerializer.Serialize(proof),
                publicInputs = new
                {
                    isAdult = publicSignals[0].GetString(), // First public signal is isAdult (1 or 0)
                    commitment = publicSignals[1].GetString(),
                    currentYear = publicSignals[2].GetString(),
                    challenge = publicSignals[3].GetString(),
                    challengeHash = publicSignals[4].GetString()
                }
            };

            return JsonSerializer.Serialize(presentation);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ZKP] Age proof generation failed: {ex.Message}");
            throw new InvalidOperationException($"ZKP age proof generation failed: {ex.Message}", ex);
        }
    }

    private async Task<string> ComputePoseidonHashAsync(string input)
    {
        // TODO: Implement Poseidon hash via circomlibjs
        // For now, return a mock hash (this will cause constraint violations in production)
        // In production, call: await _js.InvokeAsync<string>("poseidon.hash", input);
        return "21888242871839275222246405745257275088548364400416034343698204186575808495617";
    }
}
