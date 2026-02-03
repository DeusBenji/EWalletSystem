package fabric

import (
	"context"
	"fabric-resolver/internal/domain"
)

// LedgerClient defines the interface for interactions with the ledger (blockchain or local persistence).
type LedgerClient interface {
	CreateAnchor(ctx context.Context, anchor *domain.Anchor) (string, uint64, error)
	GetAnchor(ctx context.Context, hash string) (*domain.Anchor, error)
	VerifyAnchor(ctx context.Context, hash string) bool

	CreateDid(ctx context.Context, didDoc *domain.DIDDocument) error
	GetDid(ctx context.Context, did string) (*domain.DIDDocument, error)

	GetStats() map[string]interface{}
	Close() error
}
