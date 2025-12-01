package handlers

import (
	"encoding/json"
	"log"
	"net/http"
)

type errorResponse struct {
	Error string `json:"error"`
}

// respondError sends a JSON error with given status code
func respondError(w http.ResponseWriter, status int, message string) {
	resp := errorResponse{
		Error: message,
	}
	respondJSON(w, status, resp)
}

// respondJSON sends a JSON response with given status code
func respondJSON(w http.ResponseWriter, status int, payload interface{}) {
	w.Header().Set("Content-Type", "application/json")
	w.WriteHeader(status)

	// Proper error handling for JSON encoding
	if err := json.NewEncoder(w).Encode(payload); err != nil {
		log.Printf("ERROR: Failed to encode JSON response: %v (payload: %+v)", err, payload)
		// At this point headers are already written, so we can't change the response
		// But at least we've logged the error
	}
}
