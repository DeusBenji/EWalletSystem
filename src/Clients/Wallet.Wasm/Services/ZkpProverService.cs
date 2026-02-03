using System.Text.Json;
using Wallet.Wasm.Models;

namespace Wallet.Wasm.Services;

public class ZkpProverService
{
    // In a real implementation, this would call a WASM module (e.g. snarkjs or a Rust provers).
    // For this MVP/Standardization steps, we mock the proof generation but follow the data structure.

    public async Task<string> GenerateAgeProofAsync(LocalWalletToken token, string challenge)
    {
        // 1. Simulate heavy computation
        await Task.Delay(50);

        // 2. Create the presentation payload
        // This must match AgeZkpPresentation.cs structure in ValidationService
        var presentation = new
        {
            verifiableCredential = token.Signature, // In a strict ZKP flow, we might not send the full VC, but for "binding" check we send the commitment-containing part.
                                                    // OR we send the raw VC so the verifier can check the signature + commitment match.
                                                    // For this "Strict ZKP" flow, let's assume we send the VC so the verifier can check:
                                                    // VC.Commitment == Proof.PublicInputs.Commitment
            
            proof = "mock-zkp-proof-data-" + Guid.NewGuid().ToString(),
            
            publicInputs = new 
            {
                commitment = "mock-commitment-from-token", // In real flow: token.Claims["commitment"]
                challengeHash = ComputeChallengeHash(challenge),
                currentYear = DateTime.UtcNow.Year.ToString()
            }
        };

        return JsonSerializer.Serialize(presentation);
    }

    private string ComputeChallengeHash(string challenge)
    {
        // Mock hash
        return $"hash({challenge})";
    }
}
