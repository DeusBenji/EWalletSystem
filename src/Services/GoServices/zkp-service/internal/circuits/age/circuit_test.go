package age

import (
	"crypto/rand"
	"math/big"
	"testing"

	"github.com/consensys/gnark-crypto/ecc"
	"github.com/consensys/gnark/backend"
	"github.com/consensys/gnark/frontend"
	"github.com/consensys/gnark/test"

	// Correct native MiMC for BN254
	"github.com/consensys/gnark-crypto/ecc/bn254/fr"
	fr_mimc "github.com/consensys/gnark-crypto/ecc/bn254/fr/mimc"
)

// mimcHashBN254 computes the MiMC hash of the given inputs (as field elements)
// to match the circuit's behavior: mimc.Write(circuit.Var)...
func mimcHashBN254(inputs ...*big.Int) *big.Int {
	h := fr_mimc.NewMiMC()

	for _, inp := range inputs {
		var e fr.Element
		e.SetBigInt(inp)
		b := e.Bytes()
		h.Write(b[:])
	}

	sum := h.Sum(nil)
	return new(big.Int).SetBytes(sum)
}

func TestAgeCircuit(t *testing.T) {
	assert := test.NewAssert(t)

	// 1. Prepare Valid Data
	currentYear := big.NewInt(2024)
	birthYear := big.NewInt(2000) // Age 24

	// Salt
	salt, _ := rand.Int(rand.Reader, new(big.Int).Lsh(big.NewInt(1), 128))

	// Commitment = Hash(BirthYear, Salt)
	commitment := mimcHashBN254(birthYear, salt)

	// Challenge & Hash
	challenge, _ := rand.Int(rand.Reader, new(big.Int).Lsh(big.NewInt(1), 128))
	challengeHash := mimcHashBN254(challenge)

	// 2. Define Circuit with Assignment
	var circuit AgeCircuitV1

	validAssignment := AgeCircuitV1{
		CurrentYear:   frontend.Variable(currentYear),
		Commitment:    frontend.Variable(commitment),
		ChallengeHash: frontend.Variable(challengeHash),
		BirthYear:     frontend.Variable(birthYear),
		Salt:          frontend.Variable(salt),
		Challenge:     frontend.Variable(challenge),
	}

	// 3. Test Happy Path
	assert.ProverSucceeded(&circuit, &validAssignment, test.WithCurves(ecc.BN254), test.WithBackends(backend.GROTH16))

	// 4. Test Underage
	birthYearUnder := big.NewInt(2010) // Age 14 in 2024
	commitmentUnder := mimcHashBN254(birthYearUnder, salt)

	underageAssignment := AgeCircuitV1{
		CurrentYear:   frontend.Variable(currentYear),
		Commitment:    frontend.Variable(commitmentUnder),
		ChallengeHash: frontend.Variable(challengeHash), // Valid challenge
		BirthYear:     frontend.Variable(birthYearUnder),
		Salt:          frontend.Variable(salt),
		Challenge:     frontend.Variable(challenge),
	}
	// Logic fails (age < 18)
	assert.ProverFailed(&circuit, &underageAssignment, test.WithCurves(ecc.BN254), test.WithBackends(backend.GROTH16))

	// 5. Test Binding Mismatch (Lying about birth year)
	// Prover claims birthYear=1990 (Age 34) but uses commitment for 2000
	fakeBirthYear := big.NewInt(1990)

	bindingFailAssignment := AgeCircuitV1{
		CurrentYear:   frontend.Variable(currentYear),
		Commitment:    frontend.Variable(commitment), // Matches 2000
		ChallengeHash: frontend.Variable(challengeHash),
		BirthYear:     frontend.Variable(fakeBirthYear), // Trying 1990
		Salt:          frontend.Variable(salt),
		Challenge:     frontend.Variable(challenge),
	}
	// Binding check fails
	assert.ProverFailed(&circuit, &bindingFailAssignment, test.WithCurves(ecc.BN254), test.WithBackends(backend.GROTH16))

	// 6. Test Replay / Challenge Mismatch
	fakeChallenge, _ := rand.Int(rand.Reader, new(big.Int).Lsh(big.NewInt(1), 128))

	replayFailAssignment := AgeCircuitV1{
		CurrentYear:   frontend.Variable(currentYear),
		Commitment:    frontend.Variable(commitment),
		ChallengeHash: frontend.Variable(challengeHash), // Expects valid challenge
		BirthYear:     frontend.Variable(birthYear),
		Salt:          frontend.Variable(salt),
		Challenge:     frontend.Variable(fakeChallenge), // Wrong challenge
	}
	// Replay check fails
	assert.ProverFailed(&circuit, &replayFailAssignment, test.WithCurves(ecc.BN254), test.WithBackends(backend.GROTH16))
}
