package handlers

import (
	"encoding/json"
	"net/http"

	"fabric-resolver/internal/domain"
	"fabric-resolver/internal/infrastructure/fabric"

	"github.com/gorilla/mux"
)

type AnchorHandler struct {
	fabricClient fabric.FabricClient // Brug interface i stedet for konkret type
}

func NewAnchorHandler(fabricClient fabric.FabricClient) *AnchorHandler {
	return &AnchorHandler{
		fabricClient: fabricClient,
	}
}

type CreateAnchorRequest struct {
	Hash      string `json:"hash"`
	IssuerDID string `json:"issuerDid,omitempty"`
	Metadata  string `json:"metadata,omitempty"`
}

type AnchorResponse struct {
	Hash        string `json:"hash"`
	IssuerDID   string `json:"issuerDid"`
	Timestamp   string `json:"timestamp"`
	BlockNumber uint64 `json:"blockNumber"`
	TxID        string `json:"txId"`
	Metadata    string `json:"metadata,omitempty"`
}

// POST /anchors
func (h *AnchorHandler) CreateAnchor(w http.ResponseWriter, r *http.Request) {
	var req CreateAnchorRequest
	if err := json.NewDecoder(r.Body).Decode(&req); err != nil {
		respondError(w, http.StatusBadRequest, "Invalid request body")
		return
	}

	if req.Hash == "" {
		respondError(w, http.StatusBadRequest, "Hash is required")
		return
	}

	anchor := &domain.Anchor{
		Hash:      req.Hash,
		IssuerDID: req.IssuerDID,
		Metadata:  req.Metadata,
	}

	txID, blockNumber, err := h.fabricClient.CreateAnchor(r.Context(), anchor)
	if err != nil {
		respondError(w, http.StatusInternalServerError, "Failed to create anchor: "+err.Error())
		return
	}

	resp := AnchorResponse{
		Hash:        anchor.Hash,
		IssuerDID:   anchor.IssuerDID,
		Timestamp:   anchor.Timestamp.UTC().Format("2006-01-02T15:04:05Z"),
		BlockNumber: blockNumber,
		TxID:        txID,
		Metadata:    anchor.Metadata,
	}

	respondJSON(w, http.StatusCreated, resp)
}

// GET /anchors/{hash}
func (h *AnchorHandler) GetAnchor(w http.ResponseWriter, r *http.Request) {
	vars := mux.Vars(r)
	hash := vars["hash"]

	if hash == "" {
		respondError(w, http.StatusBadRequest, "Hash is required")
		return
	}

	anchor, err := h.fabricClient.GetAnchor(r.Context(), hash)
	if err != nil {
		respondError(w, http.StatusNotFound, "Anchor not found")
		return
	}

	resp := AnchorResponse{
		Hash:        anchor.Hash,
		IssuerDID:   anchor.IssuerDID,
		Timestamp:   anchor.Timestamp.UTC().Format("2006-01-02T15:04:05Z"),
		BlockNumber: anchor.BlockNumber,
		TxID:        anchor.TxID,
		Metadata:    anchor.Metadata,
	}

	respondJSON(w, http.StatusOK, resp)
}

// GET /anchors/{hash}/verify
func (h *AnchorHandler) VerifyAnchor(w http.ResponseWriter, r *http.Request) {
	vars := mux.Vars(r)
	hash := vars["hash"]

	if hash == "" {
		respondError(w, http.StatusBadRequest, "Hash is required")
		return
	}

	// VerifyAnchor returnerer nu bare bool (ikke error)
	exists := h.fabricClient.VerifyAnchor(r.Context(), hash)

	resp := map[string]interface{}{
		"hash":   hash,
		"exists": exists,
		"valid":  exists, // For kompatibilitet med .NET client forventning
	}

	respondJSON(w, http.StatusOK, resp)
}
