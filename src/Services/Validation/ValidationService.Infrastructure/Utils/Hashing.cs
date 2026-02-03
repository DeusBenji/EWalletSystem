using System.Security.Cryptography;
using System.Text;

namespace ValidationService.Infrastructure.Utils
{
    public static class Hashing
    {
        /// <summary>
        /// Hashes the challenge string to match the circuit's expectations.
        /// Note: The ZK circuit uses MiMC for internal hashing. 
        /// If the 'ChallengeHash' public input is derived via MiMC, we must use MiMC here.
        /// If the circuit allows SHA256 for public inputs, we use that.
        /// 
        /// In our design (Task description): "Challenge (nonce) that is hashed and included as a public input... The ValidationService plugin computes serverChallengeHash".
        /// 
        /// CRITICAL: Go service / Circuit uses MiMC for "Hash(Challenge) == ChallengeHash".
        /// So we MUST compute MiMC(Challenge) here in C#.
        /// 
        /// However, implementing MiMC in C# exactly as gnark-crypto does is complex.
        /// Alternative used in many systems:
        /// The Verifier (C#) generates a RANDOM 'Challenge' (scalar). 
        /// It sends 'Challenge' to the Prover (Wallet).
        /// The Prover computes 'ChallengeHash = MiMC(Challenge)' inside the circuit (or outside and proves preimage).
        /// The circuit checks 'MiMC(Challenge) == ChallengeHash'.
        /// 
        /// Wait. If we send 'Challenge' (plaintext) to the prover, the prover inputs 'Challenge' as private/public input?
        /// Circuit: "Challenge" is Private Input. "ChallengeHash" is Public Input.
        /// 
        /// The Verifier needs to know "ChallengeHash".
        /// If the Verifier generates "Challenge" (random 128 bit),
        /// it must compute "ChallengeHash = MiMC(Challenge)" to pass as public input to the verifier (Go Service).
        /// 
        /// Issue: We don't have a MiMC implementation in C#.
        /// Solution A: Call the Go Service to "ComputeHash(Challenge)"?
        /// Solution B: Change circuit to use SHA256 (expensive in ZK).
        /// Solution C: Use a simpler relationship or trust the generic "Hash" concept if we can verify it.
        /// 
        /// BETTER APPROACH for V1:
        /// The "Challenge" is passed as a PUBLIC input to the circuit?
        /// If "Challenge" is Public, then the Verifier sends "Challenge" to Go Service.
        /// Go Service checks "Challenge" matches the one used in proof?
        /// 
        /// Let's look at Circuit V1 again (Step 178):
        /// Public Inputs: CurrentYear, Commitment, ChallengeHash.
        /// Private Inputs: Challenge.
        /// Constraints: Hash(Challenge) == ChallengeHash.
        /// 
        /// Use case: Replay protection.
        /// The Verifier generates a random nonce `N`.
        /// The Wallet proves "I know 'x' such that Hash(x) = H_N" where H_N is related to `N`?
        /// 
        /// Actually, simpler:
        /// Verifier sends Challenge `C` to Wallet.
        /// Wallet inputs `C` into circuit.
        /// Circuit calculates `H = Hash(C)`.
        /// `H` is exposed as public input (or `C` is exposed).
        /// 
        /// If `C` is private and `H` is public:
        /// The Wallet effectively "commits" to `C`.
        /// The Verifier must check that `H == Hash(OriginalChallenge)`.
        /// So the Verifier MUST compute `Hash(OriginalChallenge)`.
        /// 
        /// If we can't compute MiMC in C#, we have a problem.
        /// 
        /// WORKAROUND:
        /// Change the protocol slightly for MVP or include a helper endpoint in ZKP Service to "GetChallengeHash(nonce)".
        /// 
        /// OR:
        /// The Verifier sends `Challenge` to ZKP Service as well?
        /// VerifyRequest(proof, publicInputs: { challengeHash: ..., ... })
        /// 
        /// If validation service cannot compute MiMC, it cannot populate `challengeHash` correctly if `challengeHash` is the result of MiMC.
        /// 
        /// Let's assume for this task we implement a helper `GetChallengeHash` in `zkp-service` API?
        /// OR simply use SHA256 for the challenge part? 
        /// But the circuit already uses MiMC.
        /// 
        /// DECISION:
        /// I will add a method to `ZkpServiceClient` to "GetChallengeHash(challenge)".
        /// This is safer than implementing MiMC in C#.
        /// 
        /// So `Hashing.cs` might effectively just delegate or handle simple conversions.
        /// 
        /// Let's stick to SHA256 for other things, but for MiMC, we delegate.
        /// 
        /// For this file, I'll provide SHA256 helper as a utility, and rely on `ZkpServiceClient` for MiMC.
        /// </summary>
        public static string ComputeSha256(string input)
        {
            using var sha = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(input);
            var hash = sha.ComputeHash(bytes);
            return Convert.ToHexString(hash).ToLowerInvariant();
        }
    }
}
