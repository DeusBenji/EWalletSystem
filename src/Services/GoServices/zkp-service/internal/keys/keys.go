package keys

import (
	"log"

	"github.com/consensys/gnark-crypto/ecc"
	"github.com/consensys/gnark/backend/groth16"
	"github.com/consensys/gnark/constraint"
	"github.com/consensys/gnark/frontend"
	"github.com/consensys/gnark/frontend/cs/r1cs"

	"zkp-service/internal/circuits/age"
)

var (
	// In memory keys for now. In prod, load from disk.
	VerifyingKey     groth16.VerifyingKey
	ProvingKey       groth16.ProvingKey
	ConstraintSystem constraint.ConstraintSystem
)

func Init() {
	log.Println("Initializing Zero Knowledge Keys (Groth16 Setup)...")

	// 1. Compile the circuit
	var circuit age.AgeCircuitV1
	ccs, err := frontend.Compile(ecc.BN254.ScalarField(), r1cs.NewBuilder, &circuit)
	if err != nil {
		log.Fatalf("Failed to compile circuit: %v", err)
	}
	ConstraintSystem = ccs

	// 2. Setup (Generate Keys)
	// In production, use trusted setup keys. Here we generate dummy trusted setup.
	pk, vk, err := groth16.Setup(ccs)
	if err != nil {
		log.Fatalf("Failed to run setup: %v", err)
	}

	ProvingKey = pk
	VerifyingKey = vk

	log.Println("Keys initialized successfully.")
}
