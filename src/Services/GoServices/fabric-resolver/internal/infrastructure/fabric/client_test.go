package fabric

import (
	"context"
	"sync"
	"testing"
	"time"

	"fabric-resolver/internal/domain"
)

func TestNewClient(t *testing.T) {
	client, err := NewClient()
	if err != nil {
		t.Fatalf("Failed to create client: %v", err)
	}
	defer client.Close()

	if client.anchors == nil {
		t.Error("anchors map not initialized")
	}
	if client.dids == nil {
		t.Error("dids map not initialized")
	}
	if client.nextBlock != 1 {
		t.Errorf("expected nextBlock=1, got %d", client.nextBlock)
	}
}

func TestCreateAnchor(t *testing.T) {
	client, _ := NewClient()
	defer client.Close()

	ctx := context.Background()
	anchor := &domain.Anchor{
		Hash:      "test-hash-123",
		IssuerDID: "did:example:issuer",
		Metadata:  "test metadata",
	}

	txID, blockNum, err := client.CreateAnchor(ctx, anchor)
	if err != nil {
		t.Fatalf("CreateAnchor failed: %v", err)
	}

	if txID == "" {
		t.Error("txID should not be empty")
	}
	if blockNum != 1 {
		t.Errorf("expected blockNum=1, got %d", blockNum)
	}
	if anchor.Timestamp.IsZero() {
		t.Error("timestamp should be set")
	}
}

func TestCreateAnchorDuplicate(t *testing.T) {
	client, _ := NewClient()
	defer client.Close()

	ctx := context.Background()
	anchor := &domain.Anchor{
		Hash:      "duplicate-hash",
		IssuerDID: "did:example:issuer",
	}

	// First creation should succeed
	_, _, err := client.CreateAnchor(ctx, anchor)
	if err != nil {
		t.Fatalf("First CreateAnchor failed: %v", err)
	}

	// Second creation should fail
	_, _, err = client.CreateAnchor(ctx, anchor)
	if err == nil {
		t.Error("Expected error for duplicate anchor, got nil")
	}
}

func TestGetAnchor(t *testing.T) {
	client, _ := NewClient()
	defer client.Close()

	ctx := context.Background()
	original := &domain.Anchor{
		Hash:      "test-hash",
		IssuerDID: "did:example:issuer",
	}

	_, _, err := client.CreateAnchor(ctx, original)
	if err != nil {
		t.Fatalf("CreateAnchor failed: %v", err)
	}

	retrieved, err := client.GetAnchor(ctx, "test-hash")
	if err != nil {
		t.Fatalf("GetAnchor failed: %v", err)
	}

	if retrieved.Hash != original.Hash {
		t.Errorf("expected hash %s, got %s", original.Hash, retrieved.Hash)
	}
	if retrieved.IssuerDID != original.IssuerDID {
		t.Errorf("expected issuer %s, got %s", original.IssuerDID, retrieved.IssuerDID)
	}
}

func TestGetAnchorNotFound(t *testing.T) {
	client, _ := NewClient()
	defer client.Close()

	ctx := context.Background()
	_, err := client.GetAnchor(ctx, "nonexistent-hash")
	if err == nil {
		t.Error("Expected error for nonexistent anchor, got nil")
	}
}

func TestVerifyAnchor(t *testing.T) {
	client, _ := NewClient()
	defer client.Close()

	ctx := context.Background()

	// Should not exist initially
	exists := client.VerifyAnchor(ctx, "test-hash")
	if exists {
		t.Error("Anchor should not exist initially")
	}

	// Create anchor
	anchor := &domain.Anchor{Hash: "test-hash"}
	client.CreateAnchor(ctx, anchor)

	// Should exist now
	exists = client.VerifyAnchor(ctx, "test-hash")
	if !exists {
		t.Error("Anchor should exist after creation")
	}
}

func TestCreateDid(t *testing.T) {
	client, _ := NewClient()
	defer client.Close()

	ctx := context.Background()
	didDoc := &domain.DIDDocument{
		Context: []string{"https://www.w3.org/ns/did/v1"},
		ID:      "did:example:123",
		VerificationMethod: []domain.VerificationMethod{
			{
				ID:              "did:example:123#key-1",
				Type:            "Ed25519VerificationKey2020",
				Controller:      "did:example:123",
				PublicKeyBase58: "H3C2AVvLMv6gmMNam3uVAjZpfkcJCwDwnZn6z3wXmqPV",
			},
		},
	}

	err := client.CreateDid(ctx, didDoc)
	if err != nil {
		t.Fatalf("CreateDid failed: %v", err)
	}

	if didDoc.Created.IsZero() {
		t.Error("Created timestamp should be set")
	}
	if didDoc.Updated.IsZero() {
		t.Error("Updated timestamp should be set")
	}
}

func TestGetDid(t *testing.T) {
	client, _ := NewClient()
	defer client.Close()

	ctx := context.Background()
	original := &domain.DIDDocument{
		Context: []string{"https://www.w3.org/ns/did/v1"},
		ID:      "did:example:456",
	}

	err := client.CreateDid(ctx, original)
	if err != nil {
		t.Fatalf("CreateDid failed: %v", err)
	}

	retrieved, err := client.GetDid(ctx, "did:example:456")
	if err != nil {
		t.Fatalf("GetDid failed: %v", err)
	}

	if retrieved.ID != original.ID {
		t.Errorf("expected ID %s, got %s", original.ID, retrieved.ID)
	}
}

func TestConcurrentAnchors(t *testing.T) {
	client, _ := NewClient()
	defer client.Close()

	ctx := context.Background()
	numGoroutines := 100
	var wg sync.WaitGroup
	wg.Add(numGoroutines)

	// Create 100 anchors concurrently
	for i := 0; i < numGoroutines; i++ {
		go func(index int) {
			defer wg.Done()
			anchor := &domain.Anchor{
				Hash:      time.Now().Format("2006-01-02T15:04:05.999999999Z07:00") + "-" + string(rune(index)),
				IssuerDID: "did:example:concurrent",
			}
			_, _, err := client.CreateAnchor(ctx, anchor)
			if err != nil {
				t.Errorf("Concurrent CreateAnchor failed: %v", err)
			}
		}(i)
	}

	wg.Wait()

	// Check stats
	stats := client.GetStats()
	anchorsCount := stats["anchors"].(int)
	if anchorsCount != numGoroutines {
		t.Errorf("expected %d anchors, got %d", numGoroutines, anchorsCount)
	}
}

func TestContextCancellation(t *testing.T) {
	client, _ := NewClient()
	defer client.Close()

	ctx, cancel := context.WithCancel(context.Background())
	cancel() // Cancel immediately

	anchor := &domain.Anchor{Hash: "test-hash"}
	_, _, err := client.CreateAnchor(ctx, anchor)
	if err == nil {
		t.Error("Expected error for cancelled context, got nil")
	}
}

func TestGetStats(t *testing.T) {
	client, _ := NewClient()
	defer client.Close()

	ctx := context.Background()

	// Initial stats
	stats := client.GetStats()
	if stats["anchors"].(int) != 0 {
		t.Error("Expected 0 anchors initially")
	}
	if stats["dids"].(int) != 0 {
		t.Error("Expected 0 DIDs initially")
	}

	// Add some data
	client.CreateAnchor(ctx, &domain.Anchor{Hash: "hash1"})
	client.CreateDid(ctx, &domain.DIDDocument{ID: "did:example:1"})

	// Check updated stats
	stats = client.GetStats()
	if stats["anchors"].(int) != 1 {
		t.Errorf("Expected 1 anchor, got %d", stats["anchors"].(int))
	}
	if stats["dids"].(int) != 1 {
		t.Errorf("Expected 1 DID, got %d", stats["dids"].(int))
	}
}

func TestClose(t *testing.T) {
	client, _ := NewClient()
	
	ctx := context.Background()
	client.CreateAnchor(ctx, &domain.Anchor{Hash: "test"})

	err := client.Close()
	if err != nil {
		t.Fatalf("Close failed: %v", err)
	}

	// After close, maps should be nil
	if client.anchors != nil {
		t.Error("anchors map should be nil after Close")
	}
	if client.dids != nil {
		t.Error("dids map should be nil after Close")
	}
}
