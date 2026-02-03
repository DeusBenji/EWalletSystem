package api

// VerifyAgeV1Request matches the strict requirements: proof + inputs.
type VerifyAgeV1Request struct {
	Proof        []byte       `json:"proof"`        // Serialized Groth16 proof
	PublicInputs PublicInputs `json:"publicInputs"` // Public inputs needed for verification
}

type PublicInputs struct {
	CurrentYear   string `json:"currentYear"`   // As string to handle large field elements if needed, or int
	Commitment    string `json:"commitment"`    // Hex or Base64 string of the commitment
	ChallengeHash string `json:"challengeHash"` // Hex or Base64 string of the challenge hash
}

type VerifyResponse struct {
	Valid bool   `json:"valid"`
	Error string `json:"error,omitempty"`
}
