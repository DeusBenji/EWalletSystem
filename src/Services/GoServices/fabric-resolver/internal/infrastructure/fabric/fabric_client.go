package fabric

import (
	"context"
	"fmt"
	"log"
	"sync"
	"time"

	"fabric-resolver/internal/domain"
)

// FabricClient defines the interface for blockchain interactions
type FabricClient interface {
	CreateAnchor(ctx context.Context, anchor *domain.Anchor) (string, uint64, error)
	GetAnchor(ctx context.Context, hash string) (*domain.Anchor, error)
	VerifyAnchor(ctx context.Context, hash string) bool

	CreateDid(ctx context.Context, didDoc *domain.DIDDocument) error
	GetDid(ctx context.Context, did string) (*domain.DIDDocument, error)

	GetStats() map[string]interface{}
	Close() error
}

// Client handles communication with Hyperledger Fabric
// This is a mock implementation for MVP - stores data in memory
type Client struct {
	anchors   map[string]*domain.Anchor
	dids      map[string]*domain.DIDDocument
	nextBlock uint64
	mu        sync.RWMutex // Protects all maps and counters
	logger    *log.Logger
}

// NewClient creates a new Fabric client
func NewClient() (*Client, error) {
	client := &Client{
		anchors:   make(map[string]*domain.Anchor),
		dids:      make(map[string]*domain.DIDDocument),
		nextBlock: 1,
		logger:    log.Default(),
	}

	client.logger.Println("Fabric client initialized (mock mode)")
	return client, nil
}

// CreateAnchor stores an anchor on the blockchain
func (c *Client) CreateAnchor(ctx context.Context, anchor *domain.Anchor) (string, uint64, error) {
	// Check if context is cancelled
	if err := ctx.Err(); err != nil {
		return "", 0, fmt.Errorf("context cancelled: %w", err)
	}

	c.mu.Lock()
	defer c.mu.Unlock()

	// Check for duplicate
	if _, exists := c.anchors[anchor.Hash]; exists {
		return "", 0, fmt.Errorf("anchor already exists: %s", anchor.Hash)
	}

	// Use single timestamp for consistency
	now := time.Now().UTC()

	anchor.TxID = fmt.Sprintf("tx-%d", now.Unix())
	anchor.BlockNumber = c.nextBlock
	anchor.Timestamp = now

	c.anchors[anchor.Hash] = anchor
	c.nextBlock++

	c.logger.Printf("Anchor created: %s (block: %d, tx: %s)",
		anchor.Hash, anchor.BlockNumber, anchor.TxID)

	return anchor.TxID, anchor.BlockNumber, nil
}

// GetAnchor retrieves an anchor from the blockchain
func (c *Client) GetAnchor(ctx context.Context, hash string) (*domain.Anchor, error) {
	// Check if context is cancelled
	if err := ctx.Err(); err != nil {
		return nil, fmt.Errorf("context cancelled: %w", err)
	}

	c.mu.RLock()
	defer c.mu.RUnlock()

	anchor, exists := c.anchors[hash]
	if !exists {
		return nil, fmt.Errorf("anchor not found: %s", hash)
	}

	// Return a copy to prevent external modifications
	anchorCopy := *anchor
	return &anchorCopy, nil
}

// VerifyAnchor checks if an anchor exists
// Returns true if exists, false otherwise
// Note: Simplified signature - removed error return as mock never fails
func (c *Client) VerifyAnchor(ctx context.Context, hash string) bool {
	// Check if context is cancelled
	if ctx.Err() != nil {
		return false
	}

	c.mu.RLock()
	defer c.mu.RUnlock()

	_, exists := c.anchors[hash]
	return exists
}

// CreateDid stores a DID document on the blockchain
func (c *Client) CreateDid(ctx context.Context, didDoc *domain.DIDDocument) error {
	// Check if context is cancelled
	if err := ctx.Err(); err != nil {
		return fmt.Errorf("context cancelled: %w", err)
	}

	c.mu.Lock()
	defer c.mu.Unlock()

	// Check for duplicate
	if _, exists := c.dids[didDoc.ID]; exists {
		return fmt.Errorf("DID already exists: %s", didDoc.ID)
	}

	// Use single timestamp for consistency
	now := time.Now().UTC()
	didDoc.Created = now
	didDoc.Updated = now

	c.dids[didDoc.ID] = didDoc

	c.logger.Printf("DID created: %s", didDoc.ID)
	return nil
}

// GetDid retrieves a DID document from the blockchain
func (c *Client) GetDid(ctx context.Context, did string) (*domain.DIDDocument, error) {
	// Check if context is cancelled
	if err := ctx.Err(); err != nil {
		return nil, fmt.Errorf("context cancelled: %w", err)
	}

	c.mu.RLock()
	defer c.mu.RUnlock()

	didDoc, exists := c.dids[did]
	if !exists {
		return nil, fmt.Errorf("DID not found: %s", did)
	}

	// Return a copy to prevent external modifications
	didCopy := *didDoc
	return &didCopy, nil
}

// GetStats returns statistics about stored data (for debugging/monitoring)
func (c *Client) GetStats() map[string]interface{} {
	c.mu.RLock()
	defer c.mu.RUnlock()

	return map[string]interface{}{
		"anchors":   len(c.anchors),
		"dids":      len(c.dids),
		"nextBlock": c.nextBlock,
		"mode":      "mock",
	}
}

// Close closes the Fabric client connection
func (c *Client) Close() error {
	c.mu.Lock()
	defer c.mu.Unlock()

	// Clear maps to free memory
	c.anchors = nil
	c.dids = nil

	c.logger.Println("Fabric client closed")
	return nil
}
