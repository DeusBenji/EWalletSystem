package api

import (
	"encoding/json"
	"net/http"
	"zkp-service/internal/circuits/policy"
)

// VerifyPolicyV1Handler handles the /verify/policy-v1 endpoint.
// Verifies Groth16 proofs for the universal policy circuit.
func VerifyPolicyV1Handler(w http.ResponseWriter, r *http.Request) {
	var req VerifyPolicyV1Request
	if err := json.NewDecoder(r.Body).Decode(&req); err != nil {
		http.Error(w, "Invalid request body", http.StatusBadRequest)
		return
	}

	// Verify the proof using the policy circuit verifier
	valid, err := policy.VerifyProof(
		req.Proof,
		req.PublicInputs.ChallengeHash,
		req.PublicInputs.PolicyHash,
		req.PublicInputs.SubjectCommitment,
		req.PublicInputs.SessionTag,
	)

	if err != nil {
		resp := VerifyResponse{
			Valid: false,
			Error: err.Error(),
		}
		w.Header().Set("Content-Type", "application/json")
		w.WriteHeader(http.StatusOK)
		json.NewEncoder(w).Encode(resp)
		return
	}

	resp := VerifyResponse{
		Valid: valid,
	}

	w.Header().Set("Content-Type", "application/json")
	json.NewEncoder(w).Encode(resp)
}
