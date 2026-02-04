package api

// VerifyPolicyV1Request matches the policy circuit verification requirements.
type VerifyPolicyV1Request struct {
	Proof        []byte             `json:"proof"`        // Serialized Groth16 proof
	PublicInputs PolicyPublicInputs `json:"publicInputs"` // Public inputs for policy circuit
}

type PolicyPublicInputs struct {
	ChallengeHash     string `json:"challengeHash"`     // Poseidon(challenge)
	PolicyHash        string `json:"policyHash"`        // Poseidon(policyId)
	SubjectCommitment string `json:"subjectCommitment"` // Poseidon(walletSecret) - circuit output
	SessionTag        string `json:"sessionTag"`        // Poseidon(secret, challengeHash, policyHash) - circuit output
}

// Note: HashRequest and HashResponse are defined in hash.go
