package policy

import (
	"bytes"
	"encoding/json"
	"fmt"
	"os/exec"
	"strings"
)

// VerifyProofWithSnarkJS verifies a Groth16 proof using Node.js subprocess with snarkjs.
// This is an interim solution until rapidsnark C++ integration is complete.
func VerifyProofWithSnarkJS(proofJSON, publicSignalsJSON, vkeyPath string) (bool, error) {
	// Call Node.js verification script
	cmd := exec.Command("node", "/app/scripts/verify_proof.js", proofJSON, publicSignalsJSON, vkeyPath)

	var stdout, stderr bytes.Buffer
	cmd.Stdout = &stdout
	cmd.Stderr = &stderr

	err := cmd.Run()
	output := strings.TrimSpace(stdout.String())

	if err != nil {
		// Check exit code
		if exitErr, ok := err.(*exec.ExitError); ok {
			switch exitErr.ExitCode() {
			case 1:
				// Invalid proof
				return false, nil
			case 2:
				// Verification error
				return false, fmt.Errorf("verification error: %s", stderr.String())
			default:
				return false, fmt.Errorf("unexpected exit code %d: %s", exitErr.ExitCode(), stderr.String())
			}
		}
		return false, fmt.Errorf("failed to run verification: %w", err)
	}

	// Check output
	if output == "OK" {
		return true, nil
	} else if output == "INVALID" {
		return false, nil
	}

	return false, fmt.Errorf("unexpected output: %s", output)
}

// ConvertProofToSnarkJSFormat converts proof bytes to snarkjs JSON format.
// For now, we assume the proof is already in JSON format (from browser).
func ConvertProofToSnarkJSFormat(proofBytes []byte) (string, error) {
	// Validate JSON
	var proof map[string]interface{}
	if err := json.Unmarshal(proofBytes, &proof); err != nil {
		return "", fmt.Errorf("invalid proof JSON: %w", err)
	}

	// Return as string
	return string(proofBytes), nil
}

// BuildPublicSignalsJSON builds the public signals array in snarkjs format.
// Order must match circuit public inputs: [challengeHash, policyHash, subjectCommitment, sessionTag]
func BuildPublicSignalsJSON(challengeHash, policyHash, subjectCommitment, sessionTag string) (string, error) {
	publicSignals := []string{
		challengeHash,
		policyHash,
		subjectCommitment,
		sessionTag,
	}

	jsonBytes, err := json.Marshal(publicSignals)
	if err != nil {
		return "", fmt.Errorf("failed to marshal public signals: %w", err)
	}

	return string(jsonBytes), nil
}
