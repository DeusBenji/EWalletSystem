//go:build fabric

package fabric

import (
	"context"
	"errors"
	"fabric-resolver/internal/domain"
)

// RealFabricClient implementation (skeleton)
type RealFabricClient struct {
	// Fabric SDK client fields would go here
}

func NewRealClient(cfg Config) (LedgerClient, error) {
	// Here we would initialize the real Fabric SDK key
	return nil, errors.New("Real Fabric client is not yet configured (missing connection profile/crypto)")
}

func (c *RealFabricClient) CreateAnchor(ctx context.Context, anchor *domain.Anchor) (string, uint64, error) {
	return "", 0, errors.New("not implemented")
}

func (c *RealFabricClient) GetAnchor(ctx context.Context, hash string) (*domain.Anchor, error) {
	return nil, errors.New("not implemented")
}

func (c *RealFabricClient) VerifyAnchor(ctx context.Context, hash string) bool {
	return false
}

func (c *RealFabricClient) CreateDid(ctx context.Context, didDoc *domain.DIDDocument) error {
	return errors.New("not implemented")
}

func (c *RealFabricClient) GetDid(ctx context.Context, did string) (*domain.DIDDocument, error) {
	return nil, errors.New("not implemented")
}

func (c *RealFabricClient) GetStats() map[string]interface{} {
	return map[string]interface{}{"mode": "fabric-real"}
}

func (c *RealFabricClient) Close() error {
	return nil
}
