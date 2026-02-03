package canonicalizer

import (
	"bytes"
	"crypto/hmac"
	"crypto/sha256"
	"encoding/hex"
	"encoding/json"
	"errors"
	"io"
)

const MinHMACKeyLen = 32

// CanonicalizeAndHashJSON takes raw JSON bytes, canonicalizes them, and returns a SHA-256 hash.
// Policy:
// - Uses json.Decoder.UseNumber() to preserve number representation (1 vs 1.0).
// - Re-encodes using SetEscapeHTML(false) to preserve < and > as characters.
// - Trims trailing newline added by encoder.
// - Ensures no trailing garbage tokens exist after the first valid JSON value.
func CanonicalizeAndHashJSON(raw []byte) (string, error) {
	canonicalBytes, err := canonicalizeJSON(raw)
	if err != nil {
		return "", err
	}
	return hash(canonicalBytes), nil
}

// CanonicalizeAndHash takes a Go value, canonicalizes it, and returns a SHA-256 hash.
// Note on Numbers:
// If the input 'v' already contains float64 (standard Go unmarshal), then 1.0 and 1
// may have been normalized to the same value by Go before calling this.
// Use CanonicalizeAndHashJSON if you need strict preservation of raw number formatting.
func CanonicalizeAndHash(v interface{}) (string, error) {
	canonicalBytes, err := canonicalize(v)
	if err != nil {
		return "", err
	}
	return hash(canonicalBytes), nil
}

// CanonicalizeAndCommitJSON canonicalizes raw JSON bytes and returns an HMAC-SHA256 commitment.
// Requires a key of at least 32 bytes.
func CanonicalizeAndCommitJSON(raw []byte, key []byte) (string, error) {
	if len(key) < MinHMACKeyLen {
		return "", errors.New("HMAC key size too short (min 32 bytes)")
	}

	canonicalBytes, err := canonicalizeJSON(raw)
	if err != nil {
		return "", err
	}

	return commit(canonicalBytes, key), nil
}

// CanonicalizeAndCommit canonicalizes a Go value and returns an HMAC-SHA256 commitment.
// Requires a key of at least 32 bytes.
func CanonicalizeAndCommit(v interface{}, key []byte) (string, error) {
	if len(key) < MinHMACKeyLen {
		return "", errors.New("HMAC key size too short (min 32 bytes)")
	}

	canonicalBytes, err := canonicalize(v)
	if err != nil {
		return "", err
	}

	return commit(canonicalBytes, key), nil
}

// ---------------------------------------------------------------------
// Internal Helpers
// ---------------------------------------------------------------------

func canonicalizeJSON(raw []byte) ([]byte, error) {
	dec := json.NewDecoder(bytes.NewReader(raw))
	dec.UseNumber()

	var v interface{}
	if err := dec.Decode(&v); err != nil {
		return nil, err
	}

	// Check for trailing garbage
	var extra interface{}
	err := dec.Decode(&extra)
	if err != io.EOF {
		return nil, errors.New("input contains extra data after JSON value")
	}

	return canonicalize(v)
}

func canonicalize(v interface{}) ([]byte, error) {
	var buf bytes.Buffer
	enc := json.NewEncoder(&buf)
	enc.SetEscapeHTML(false) // Crucial: do not escape <, >, &

	if err := enc.Encode(v); err != nil {
		return nil, err
	}

	b := buf.Bytes()
	// json.Encoder.Encode always appends a newline. We must remove it for canonicalization stability.
	if len(b) > 0 && b[len(b)-1] == '\n' {
		b = b[:len(b)-1]
	}

	return b, nil
}

func hash(data []byte) string {
	sum := sha256.Sum256(data)
	return hex.EncodeToString(sum[:])
}

func commit(data []byte, key []byte) string {
	mac := hmac.New(sha256.New, key)
	mac.Write(data)
	return hex.EncodeToString(mac.Sum(nil))
}
