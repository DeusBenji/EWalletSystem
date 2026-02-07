using Wallet.Wasm.Models;

namespace Wallet.Wasm.Services;

public interface IZkpProverService
{
    Task<string> GeneratePolicyProofAsync(string policyId, string challenge);
    Task<string> GenerateAgeProofAsync(LocalWalletToken token, string challenge);
}
