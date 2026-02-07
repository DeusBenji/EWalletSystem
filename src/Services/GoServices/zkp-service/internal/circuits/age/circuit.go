package age

import (
	"github.com/consensys/gnark/frontend"
	"github.com/consensys/gnark/std/hash/mimc"
	// "github.com/consensys/gnark/std/math/cmp"
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
	// diff = CurrentYear - BirthYear
	diff := api.Sub(circuit.CurrentYear, circuit.BirthYear)

	// Safe >= 18 Check:
	// We want diff >= 18.
	// So (diff - 18) must be >= 0.
	// We compute val = diff - 18.
	// Then we constrain val to be small (e.g. 64 bits).
	// If diff < 18, val will be negative (huge in field), and ToBinary(val, 64) will fail.

	val := api.Sub(diff, 18)
	api.ToBinary(val, 64)

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
