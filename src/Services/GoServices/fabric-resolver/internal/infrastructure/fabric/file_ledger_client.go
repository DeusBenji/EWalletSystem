package fabric

import (
	"context"
	"encoding/json"
	"fmt"
	"log"
	"os"
	"path/filepath"
	"sync"
	"time"

	"fabric-resolver/internal/domain"
)

// Record represents a single immutable entry in the ledger
type Record struct {
	Commitment  string              `json:"commitment"` // The hash/commitment
	TxID        string              `json:"txId"`
	BlockNumber uint64              `json:"blockNumber"`
	Timestamp   time.Time           `json:"timestamp"`
	Metadata    string              `json:"metadata,omitempty"`
	DocType     string              `json:"docType"` // "anchor" or "did"
	DIDDoc      *domain.DIDDocument `json:"didDoc,omitempty"`
}

// LedgerState represents the persisted state of the ledger
type LedgerState struct {
	Records   map[string]Record `json:"records"` // Keyed by commitment/hash/DID
	NextBlock uint64            `json:"nextBlock"`
}

// FileLedgerClient is a local file-based implementation of LedgerClient.
// It uses atomic writes (write-tmp-sync-rename) to ensure data integrity.
type FileLedgerClient struct {
	mu     sync.RWMutex
	path   string
	state  LedgerState
	logger *log.Logger
}

// NewFileLedgerClient creates a new client backed by a local JSON file.
// It ensures the directory exists and loads existing state.
func NewFileLedgerClient(path string) (*FileLedgerClient, error) {
	if path == "" {
		path = "data/ledger.json"
	}

	// Ensure directory exists
	if err := os.MkdirAll(filepath.Dir(path), 0755); err != nil {
		return nil, fmt.Errorf("failed to create data directory: %w", err)
	}

	client := &FileLedgerClient{
		path:   path,
		logger: log.Default(),
		state: LedgerState{
			Records:   make(map[string]Record),
			NextBlock: 1,
		},
	}

	if err := client.load(); err != nil {
		return nil, err
	}

	client.logger.Printf("FileLedgerClient initialized at %s", path)
	return client, nil
}

// load reads the state from disk.
// If file does not exist, starts empty.
// If file exists but is corrupt, returns strict error (fail-fast).
func (c *FileLedgerClient) load() error {
	f, err := os.Open(c.path)
	if os.IsNotExist(err) {
		return nil // Start fresh
	}
	if err != nil {
		return fmt.Errorf("failed to open ledger file: %w", err)
	}
	defer f.Close()

	// check for empty file
	stat, err := f.Stat()
	if err != nil {
		return err
	}
	if stat.Size() == 0 {
		return nil // Start fresh
	}

	decoder := json.NewDecoder(f)
	if err := decoder.Decode(&c.state); err != nil {
		return fmt.Errorf("ledger file is corrupt: %w", err)
	}

	// Ensure map is initialized if nil in file
	if c.state.Records == nil {
		c.state.Records = make(map[string]Record)
	}
	if c.state.NextBlock == 0 {
		c.state.NextBlock = 1
	}

	return nil
}

// saveAtomic persists the state to disk atomically.
func saveAtomic(stateBytes []byte, path string) error {
	tmpPath := path + ".tmp"
	f, err := os.OpenFile(tmpPath, os.O_WRONLY|os.O_CREATE|os.O_TRUNC, 0644)
	if err != nil {
		return fmt.Errorf("failed to open tmp file: %w", err)
	}

	if _, err := f.Write(stateBytes); err != nil {
		f.Close()
		return fmt.Errorf("failed to write to tmp file: %w", err)
	}

	if err := f.Sync(); err != nil {
		f.Close()
		return fmt.Errorf("failed to sync tmp file: %w", err)
	}

	if err := f.Close(); err != nil {
		return fmt.Errorf("failed to close tmp file: %w", err)
	}

	// Atomic rename
	if err := os.Rename(tmpPath, path); err != nil {
		return fmt.Errorf("failed to rename tmp file: %w", err)
	}

	return nil
}

func (c *FileLedgerClient) CreateAnchor(ctx context.Context, anchor *domain.Anchor) (string, uint64, error) {
	if err := ctx.Err(); err != nil {
		return "", 0, fmt.Errorf("context cancelled: %w", err)
	}

	c.mu.Lock()

	// Idempotency check
	if record, exists := c.state.Records[anchor.Hash]; exists {
		c.mu.Unlock()
		return record.TxID, record.BlockNumber, nil
	}

	now := time.Now().UTC()
	txID := fmt.Sprintf("tx-%d", now.UnixNano())
	blockNum := c.state.NextBlock

	// Update domain object to match returned data
	anchor.TxID = txID
	anchor.BlockNumber = blockNum
	anchor.Timestamp = now

	record := Record{
		Commitment:  anchor.Hash,
		TxID:        txID,
		BlockNumber: blockNum,
		Timestamp:   now,
		Metadata:    anchor.Metadata,
		DocType:     "anchor",
	}

	c.state.Records[anchor.Hash] = record
	c.state.NextBlock++

	// Marshal state
	data, err := json.MarshalIndent(c.state, "", "  ")
	if err != nil {
		return "", 0, fmt.Errorf("failed to marshal ledger state: %w", err)
	}

	// Persist atomically (must hold lock to ensure sequential writes and avoid file contention)
	if err := saveAtomic(data, c.path); err != nil {
		c.mu.Unlock()
		return "", 0, fmt.Errorf("failed to persist anchor: %w", err)
	}

	c.mu.Unlock()

	c.logger.Printf("Anchor created: %s (block: %d)", anchor.Hash, blockNum)
	return txID, blockNum, nil
}

func (c *FileLedgerClient) GetAnchor(ctx context.Context, hash string) (*domain.Anchor, error) {
	c.mu.RLock()
	defer c.mu.RUnlock()

	record, exists := c.state.Records[hash]
	if !exists || record.DocType != "anchor" {
		return nil, fmt.Errorf("anchor not found: %s", hash)
	}

	return &domain.Anchor{
		Hash:        record.Commitment,
		TxID:        record.TxID,
		BlockNumber: record.BlockNumber,
		Timestamp:   record.Timestamp,
		Metadata:    record.Metadata,
		IssuerDID:   "",
	}, nil
}

func (c *FileLedgerClient) VerifyAnchor(ctx context.Context, hash string) bool {
	c.mu.RLock()
	defer c.mu.RUnlock()

	record, exists := c.state.Records[hash]
	return exists && record.DocType == "anchor"
}

func (c *FileLedgerClient) CreateDid(ctx context.Context, didDoc *domain.DIDDocument) error {
	if err := ctx.Err(); err != nil {
		return fmt.Errorf("context cancelled: %w", err)
	}

	c.mu.Lock()
	if _, exists := c.state.Records[didDoc.ID]; exists {
		c.mu.Unlock()
		return fmt.Errorf("DID already exists: %s", didDoc.ID)
	}

	now := time.Now().UTC()
	didDoc.Created = now
	didDoc.Updated = now

	record := Record{
		Commitment: didDoc.ID,
		Timestamp:  now,
		DocType:    "did",
		DIDDoc:     didDoc,
	}

	c.state.Records[didDoc.ID] = record

	data, err := json.MarshalIndent(c.state, "", "  ")
	if err != nil {
		c.mu.Unlock()
		return fmt.Errorf("failed to marshal ledger state: %w", err)
	}

	if err := saveAtomic(data, c.path); err != nil {
		c.mu.Unlock()
		return fmt.Errorf("failed to persist DID: %w", err)
	}

	c.mu.Unlock()

	c.logger.Printf("DID created: %s", didDoc.ID)
	return nil
}

func (c *FileLedgerClient) GetDid(ctx context.Context, did string) (*domain.DIDDocument, error) {
	c.mu.RLock()
	defer c.mu.RUnlock()

	record, exists := c.state.Records[did]
	if !exists || record.DocType != "did" {
		return nil, fmt.Errorf("DID not found: %s", did)
	}

	// Return copy
	doc := *record.DIDDoc
	return &doc, nil
}

func (c *FileLedgerClient) GetStats() map[string]interface{} {
	c.mu.RLock()
	defer c.mu.RUnlock()

	anchors := 0
	dids := 0
	for _, r := range c.state.Records {
		if r.DocType == "anchor" {
			anchors++
		} else if r.DocType == "did" {
			dids++
		}
	}

	return map[string]interface{}{
		"anchors":   anchors,
		"dids":      dids,
		"nextBlock": c.state.NextBlock,
		"mode":      "file-persistent",
		"path":      c.path,
	}
}

func (c *FileLedgerClient) Close() error {
	return nil
}
