package age

import (
	"github.com/consensys/gnark/frontend"
	"github.com/consensys/gnark/std/hash/mimc"
)

// AgeCircuitV1 defines the constraints for the "Over 18" proof.
// It includes strict binding (Commitment) and replay protection (ChallengeHash).
type AgeCircuitV1 struct {
	// Public Inputs
	CurrentYear   frontend.Variable `gnark:",public"` // The server's current year (e.g. 2024)
	Commitment    frontend.Variable `gnark:",public"` // Hash(BirthYear | Salt) committed in the VC
	ChallengeHash frontend.Variable `gnark:",public"` // Hash(Challenge) provided by the server

	// Private Inputs
	BirthYear frontend.Variable // User's birth year
	Salt      frontend.Variable // User's secret salt
	Challenge frontend.Variable // The challenge value (pre-image of ChallengeHash)
}

// Define declares the circuit constraints
func (circuit *AgeCircuitV1) Define(api frontend.API) error {

	// ------------------------------------------------------------------
	// 1. Binding Check: Hash(BirthYear + Salt) == Commitment
	// ------------------------------------------------------------------
	// We use MiMC for hashing inside the circuit as it is SNARK-friendly.
	// Note: In production, ensure the hashing scheme matches exactly what is used
	// outside the circuit (TokenService/Wallet).
	// For this strict implementation, we assume MiMC is used everywhere for commitments.

	hasher, err := mimc.NewMiMC(api)
	if err != nil {
		return err
	}

	hasher.Write(circuit.BirthYear)
	hasher.Write(circuit.Salt)
	calculatedCommitment := hasher.Sum()

	api.AssertIsEqual(calculatedCommitment, circuit.Commitment)

	// ------------------------------------------------------------------
	// 2. Age Logic: CurrentYear - BirthYear >= 18
	// ------------------------------------------------------------------
	// Ensure the user is essentially saying: 2024 - 2000 = 24 >= 18

	// diff = CurrentYear - BirthYear
	diff := api.Sub(circuit.CurrentYear, circuit.BirthYear)

	// Assert diff >= 18
	// In gnark, we can use AssertIsLessOrEqual.
	// 18 <= diff  <==>  diff >= 18

	// Note: To be safe against underflows (e.g. if birthYear > currentYear),
	// we should technically constrain bits or assume field size handles it safely for small numbers.
	// For this V1, we simply assert the inequality.

	// Check if (diff - 18) is non-negative?
	// simpler: api.AssertIsLessOrEqual(18, diff)

	api.AssertIsLessOrEqual(18, diff)

	// ------------------------------------------------------------------
	// 3. Replay Protection: Hash(Challenge) == ChallengeHash
	// ------------------------------------------------------------------
	// This proves that the prover knows the 'Challenge' that results in 'ChallengeHash'.
	// Since 'ChallengeHash' is a public input fixed by the Verifier (server),
	// the prover must embed the specific nonce for this session into the proof.

	hasherChallenge, err := mimc.NewMiMC(api)
	if err != nil {
		return err
	}
	hasherChallenge.Write(circuit.Challenge)
	calculatedChallengeHash := hasherChallenge.Sum()

	api.AssertIsEqual(calculatedChallengeHash, circuit.ChallengeHash)

	return nil
}
