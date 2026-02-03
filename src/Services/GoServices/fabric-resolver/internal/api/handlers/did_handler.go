package handlers

import (
	"encoding/json"
	"net/http"
	"strconv"

	"fabric-resolver/internal/domain"
	"fabric-resolver/internal/infrastructure/fabric"

	"github.com/gorilla/mux"
)

type DidHandler struct {
	ledgerClient fabric.LedgerClient // Brug interface
}

func NewDidHandler(ledgerClient fabric.LedgerClient) *DidHandler {
	return &DidHandler{ledgerClient: ledgerClient}

}

type CreateDidRequest struct {
	Did                string                      `json:"did"`
	Controller         string                      `json:"controller,omitempty"`
	VerificationMethod []VerificationMethodRequest `json:"verificationMethod"`
}

type VerificationMethodRequest struct {
	Type            string `json:"type"`
	PublicKeyJwk    string `json:"publicKeyJwk,omitempty"`
	PublicKeyBase58 string `json:"publicKeyBase58,omitempty"`
}

type DidDocumentResponse struct {
	Context            []string                `json:"@context"`
	ID                 string                  `json:"id"`
	Controller         string                  `json:"controller,omitempty"`
	VerificationMethod []VerificationMethodDto `json:"verificationMethod"`
	Authentication     []string                `json:"authentication,omitempty"`
	AssertionMethod    []string                `json:"assertionMethod,omitempty"`
	Created            string                  `json:"created"`
	Updated            string                  `json:"updated"`
}

type VerificationMethodDto struct {
	ID              string `json:"id"`
	Type            string `json:"type"`
	Controller      string `json:"controller"`
	PublicKeyJwk    string `json:"publicKeyJwk,omitempty"`
	PublicKeyBase58 string `json:"publicKeyBase58,omitempty"`
}

// CreateDid registers a new DID on the blockchain
func (h *DidHandler) CreateDid(w http.ResponseWriter, r *http.Request) {
	var req CreateDidRequest
	if err := json.NewDecoder(r.Body).Decode(&req); err != nil {
		respondError(w, http.StatusBadRequest, "Invalid request body")
		return
	}

	if req.Did == "" {
		respondError(w, http.StatusBadRequest, "DID is required")
		return
	}

	// Convert to domain model
	didDoc := &domain.DIDDocument{
		Context:            []string{"https://www.w3.org/ns/did/v1"},
		ID:                 req.Did,
		Controller:         req.Controller,
		VerificationMethod: make([]domain.VerificationMethod, len(req.VerificationMethod)),
	}

	for i, vm := range req.VerificationMethod {
		didDoc.VerificationMethod[i] = domain.VerificationMethod{
			ID:              req.Did + "#key-" + strconv.Itoa(i+1),
			Type:            vm.Type,
			Controller:      req.Did,
			PublicKeyJwk:    vm.PublicKeyJwk,
			PublicKeyBase58: vm.PublicKeyBase58,
		}
	}

	// Store on Fabric
	if err := h.ledgerClient.CreateDid(r.Context(), didDoc); err != nil {
		respondError(w, http.StatusInternalServerError, "Failed to create DID: "+err.Error())
		return
	}

	response := map[string]interface{}{
		"did":     req.Did,
		"status":  "created",
		"message": "DID successfully registered on blockchain",
	}

	respondJSON(w, http.StatusCreated, response)
}

// ResolveDid retrieves a DID Document from the blockchain
func (h *DidHandler) ResolveDid(w http.ResponseWriter, r *http.Request) {
	vars := mux.Vars(r)
	did := vars["did"]

	if did == "" {
		respondError(w, http.StatusBadRequest, "DID is required")
		return
	}

	// Query from Fabric
	didDoc, err := h.ledgerClient.GetDid(r.Context(), did)
	if err != nil {
		respondError(w, http.StatusNotFound, "DID not found")
		return
	}

	// Convert to response DTO
	response := DidDocumentResponse{
		Context:            didDoc.Context,
		ID:                 didDoc.ID,
		Controller:         didDoc.Controller,
		VerificationMethod: make([]VerificationMethodDto, len(didDoc.VerificationMethod)),
		Created:            didDoc.Created.Format("2006-01-02T15:04:05Z"),
		Updated:            didDoc.Updated.Format("2006-01-02T15:04:05Z"),
	}

	// Build authentication and assertion method lists
	authMethods := make([]string, 0, len(didDoc.VerificationMethod))
	assertionMethods := make([]string, 0, len(didDoc.VerificationMethod))

	for i, vm := range didDoc.VerificationMethod {
		response.VerificationMethod[i] = VerificationMethodDto{
			ID:              vm.ID,
			Type:            vm.Type,
			Controller:      vm.Controller,
			PublicKeyJwk:    vm.PublicKeyJwk,
			PublicKeyBase58: vm.PublicKeyBase58,
		}

		// Add to authentication and assertion methods
		authMethods = append(authMethods, vm.ID)
		assertionMethods = append(assertionMethods, vm.ID)
	}

	response.Authentication = authMethods
	response.AssertionMethod = assertionMethods

	respondJSON(w, http.StatusOK, response)
}
