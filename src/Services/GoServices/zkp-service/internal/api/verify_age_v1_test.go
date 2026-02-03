package api

import (
	"bytes"
	"encoding/json"
	"net/http"
	"net/http/httptest"
	"testing"
)

func TestVerifyAgeV1Handler_Structure(t *testing.T) {
	// Create request payload
	reqBody := VerifyAgeV1Request{
		Proof: []byte("fake-proof"),
		PublicInputs: PublicInputs{
			CurrentYear:   "2024",
			Commitment:    "12345",
			ChallengeHash: "abcde",
		},
	}
	body, _ := json.Marshal(reqBody)

	// Create request
	req, err := http.NewRequest("POST", "/verify/age-v1", bytes.NewBuffer(body))
	if err != nil {
		t.Fatal(err)
	}

	// Recorder
	rr := httptest.NewRecorder()
	handler := http.HandlerFunc(VerifyAgeV1Handler)

	// Call
	handler.ServeHTTP(rr, req)

	// Check status
	if status := rr.Code; status != http.StatusOK {
		t.Errorf("handler returned wrong status code: got %v want %v",
			status, http.StatusOK)
	}

	// Check response (currently stubbed to valid: false)
	var resp VerifyResponse
	json.NewDecoder(rr.Body).Decode(&resp)

	if resp.Valid != false {
		t.Errorf("handler returned valid=true, expected false (stub)")
	}
}
