package api

import (
	"encoding/json"
	"math/big"
	"net/http"

	"github.com/consensys/gnark-crypto/ecc/bn254/fr"
	fr_mimc "github.com/consensys/gnark-crypto/ecc/bn254/fr/mimc"
)

type HashRequest struct {
	Input string `json:"input"` // Decimal string of the challenge (big int)
}

type HashResponse struct {
	Hash string `json:"hash"` // Decimal string of the hash
}

// HashHandler computes MiMC hash of a single input (as field element)
// This matches how we hash the Challenge in the circuit.
func HashHandler(w http.ResponseWriter, r *http.Request) {
	var req HashRequest
	if err := json.NewDecoder(r.Body).Decode(&req); err != nil {
		http.Error(w, "Invalid request body", http.StatusBadRequest)
		return
	}

	// Parse input as BigInt
	val, ok := new(big.Int).SetString(req.Input, 10)
	if !ok {
		http.Error(w, "Invalid input number", http.StatusBadRequest)
		return
	}

	// Compute MiMC Hash
	// Matches mimcHashBN254 helper in tests:
	// h.Write(element.Bytes())

	h := fr_mimc.NewMiMC()
	var e fr.Element
	e.SetBigInt(val)
	b := e.Bytes()
	h.Write(b[:])

	sum := h.Sum(nil)
	hashInt := new(big.Int).SetBytes(sum)

	resp := HashResponse{
		Hash: hashInt.String(),
	}

	w.Header().Set("Content-Type", "application/json")
	json.NewEncoder(w).Encode(resp)
}
