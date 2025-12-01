package domain

import "time"

// Anchor represents a hash anchored on the blockchain
type Anchor struct {
	Hash        string    `json:"hash"`
	IssuerDID   string    `json:"issuerDid"`
	Timestamp   time.Time `json:"timestamp"`
	BlockNumber uint64    `json:"blockNumber"`
	TxID        string    `json:"txId"`
	Metadata    string    `json:"metadata,omitempty"`
}

// DIDDocument represents a DID document (for future use)
type DIDDocument struct {
	Context            []string             `json:"@context"`
	ID                 string               `json:"id"`
	Controller         string               `json:"controller,omitempty"`
	VerificationMethod []VerificationMethod `json:"verificationMethod"`
	Authentication     []string             `json:"authentication,omitempty"`
	AssertionMethod    []string             `json:"assertionMethod,omitempty"`
	Service            []Service            `json:"service,omitempty"`
	Created            time.Time            `json:"created"`
	Updated            time.Time            `json:"updated"`
}

// VerificationMethod represents a public key for verification
type VerificationMethod struct {
	ID              string `json:"id"`
	Type            string `json:"type"`
	Controller      string `json:"controller"`
	PublicKeyJwk    string `json:"publicKeyJwk,omitempty"`
	PublicKeyBase58 string `json:"publicKeyBase58,omitempty"`
}

// Service represents a service endpoint in a DID document
type Service struct {
	ID              string `json:"id"`
	Type            string `json:"type"`
	ServiceEndpoint string `json:"serviceEndpoint"`
}
