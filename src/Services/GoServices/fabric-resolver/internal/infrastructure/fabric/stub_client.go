//go:build !fabric

package fabric

import (
	"errors"
)

// NewRealClient stub for non-fabric builds
func NewRealClient(cfg Config) (LedgerClient, error) {
	return nil, errors.New("binary not built with 'fabric' tag; use LEDGER_MODE=file or rebuild with -tags fabric")
}
