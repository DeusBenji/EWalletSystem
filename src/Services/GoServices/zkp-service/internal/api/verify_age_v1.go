package api

import (
	"encoding/json"
	"net/http"
)

// VerifyAgeV1Handler handles the /verify/age-v1 endpoint.
// In a real implementation, this would load the Verifying Key (VK)
// and call gnark.Verify().
func VerifyAgeV1Handler(w http.ResponseWriter, r *http.Request) {
	var req VerifyAgeV1Request
	if err := json.NewDecoder(r.Body).Decode(&req); err != nil {
		http.Error(w, "Invalid request body", http.StatusBadRequest)
		return
	}

	// TODO: Load Key, Deserialize Proof, Deserialize Witness, Verify
	// For now, return false as we haven't implemented the zkp backend integration yet.

	resp := VerifyResponse{
		Valid: false,
		Error: "Not implemented",
	}

	w.Header().Set("Content-Type", "application/json")
	json.NewEncoder(w).Encode(resp)
}
