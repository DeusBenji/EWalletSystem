package fabric

import (
	"context"
	"fmt"
	"path/filepath"
	"testing"
	"time"

	"fabric-resolver/internal/domain"

	"golang.org/x/sync/errgroup"
)

func TestNewFileLedgerClient(t *testing.T) {
	tmpDir := t.TempDir()
	ledgerPath := filepath.Join(tmpDir, "ledger.json")

	client, err := NewFileLedgerClient(ledgerPath)
	if err != nil {
		t.Fatalf("Failed to create client: %v", err)
	}
	// Close is no-op but good practice
	client.Close()

	if client.path != ledgerPath {
		t.Errorf("expected path %s, got %s", ledgerPath, client.path)
	}
}

func TestPersistenceAcrossRestart(t *testing.T) {
	tmpDir := t.TempDir()
	ledgerPath := filepath.Join(tmpDir, "ledger.json")
	ctx := context.Background()

	// 1. Create client and data
	client1, err := NewFileLedgerClient(ledgerPath)
	if err != nil {
		t.Fatalf("NewFileLedgerClient 1 failed: %v", err)
	}

	anchor := &domain.Anchor{Hash: "persist-hash"}
	txID, _, err := client1.CreateAnchor(ctx, anchor)
	if err != nil {
		t.Fatalf("CreateAnchor failed: %v", err)
	}

	// 2. "Restart" (create new client instance pointing to same file)
	client2, err := NewFileLedgerClient(ledgerPath)
	if err != nil {
		t.Fatalf("NewFileLedgerClient 2 failed: %v", err)
	}

	// 3. Verify data exists
	retrieved, err := client2.GetAnchor(ctx, "persist-hash")
	if err != nil {
		t.Fatalf("GetAnchor failed after restart: %v", err)
	}

	if retrieved.TxID != txID {
		t.Errorf("Persistence failed: expected TxID %s, got %s", txID, retrieved.TxID)
	}
}

func TestConcurrency(t *testing.T) {
	tmpDir := t.TempDir()
	ledgerPath := filepath.Join(tmpDir, "ledger.json")

	client, err := NewFileLedgerClient(ledgerPath)
	if err != nil {
		t.Fatalf("Setup failed: %v", err)
	}

	ctx := context.Background()
	count := 100
	var g errgroup.Group

	// 1. Write 100 unique anchors concurrently
	for i := 0; i < count; i++ {
		i := i
		g.Go(func() error {
			anchor := &domain.Anchor{
				Hash: fmt.Sprintf("hash-%d", i),
			}
			_, _, err := client.CreateAnchor(ctx, anchor)
			return err
		})
	}

	if err := g.Wait(); err != nil {
		t.Fatalf("Concurrency writes failed: %v", err)
	}

	// 2. Reopen and verify ALL exist
	client2, err := NewFileLedgerClient(ledgerPath)
	if err != nil {
		t.Fatalf("Reopen failed: %v", err)
	}

	for i := 0; i < count; i++ {
		hash := fmt.Sprintf("hash-%d", i)
		if !client2.VerifyAnchor(ctx, hash) {
			t.Errorf("Missing anchor %s after concurrent write", hash)
		}
	}

	stats := client2.GetStats()
	if stats["anchors"].(int) != count {
		t.Errorf("Stats mismatch: expected %d anchors, got %d", count, stats["anchors"])
	}
}

func TestIdempotency(t *testing.T) {
	tmpDir := t.TempDir()
	ledgerPath := filepath.Join(tmpDir, "ledger.json")
	client, _ := NewFileLedgerClient(ledgerPath)
	ctx := context.Background()

	anchor := &domain.Anchor{Hash: "idempotent-hash", Metadata: "original"}

	// 1. First Write
	txID1, _, err := client.CreateAnchor(ctx, anchor)
	if err != nil {
		t.Fatalf("First write failed: %v", err)
	}

	timestamp1 := anchor.Timestamp

	// 2. Delay to ensure clock moves (if it were allowed to update)
	time.Sleep(10 * time.Millisecond)

	// 3. Duplicate Write using same Hash but different Metadata (attempted mutation)
	anchorDup := &domain.Anchor{Hash: "idempotent-hash", Metadata: "mutated"}
	txID2, _, err := client.CreateAnchor(ctx, anchorDup)
	if err != nil {
		t.Fatalf("Duplicate write failed: %v", err)
	}

	// 4. Verify Same TxID returned
	if txID1 != txID2 {
		t.Errorf("Idempotency violation: TxID changed from %s to %s", txID1, txID2)
	}

	// 5. Verify Record NOT mutated
	retrieved, _ := client.GetAnchor(ctx, "idempotent-hash")
	if retrieved.Metadata != "original" {
		t.Error("Idempotency violation: Metadata was mutated")
	}
	if !retrieved.Timestamp.Equal(timestamp1) {
		t.Error("Idempotency violation: Timestamp changed")
	}
}

func TestGetAnchorNotFound(t *testing.T) {
	tmpDir := t.TempDir()
	client, _ := NewFileLedgerClient(filepath.Join(tmpDir, "ledger.json"))

	_, err := client.GetAnchor(context.Background(), "missing")
	if err == nil {
		t.Error("Expected error for missing anchor")
	}
}
