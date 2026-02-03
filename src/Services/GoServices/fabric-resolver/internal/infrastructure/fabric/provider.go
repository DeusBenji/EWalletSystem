package fabric

import (
	"fmt"
	"os"
)

// Config holds configuration for the ledger client
type Config struct {
	Mode     string // "file" or "fabric"
	FilePath string // For file mode (default: data/ledger.json)
	// Add Fabric specific config fields here later (CCP, MSP, etc.)
}

// NewLedgerClient creates a new LedgerClient based on configuration.
// Defaults to FileLedgerClient if Mode is empty or "file".
func NewLedgerClient(cfg Config) (LedgerClient, error) {
	if cfg.Mode == "" {
		cfg.Mode = "file"
	}

	switch cfg.Mode {
	case "file":
		if cfg.FilePath == "" {
			cfg.FilePath = "data/ledger.json"
		}
		return NewFileLedgerClient(cfg.FilePath)
	case "fabric":
		return NewRealClient(cfg)
	default:
		return nil, fmt.Errorf("invalid ledger mode: %s (supported: file, fabric)", cfg.Mode)
	}
}

// LoadConfigFromEnv helper to load common env vars
func LoadConfigFromEnv() Config {
	return Config{
		Mode:     os.Getenv("LEDGER_MODE"),
		FilePath: os.Getenv("LEDGER_FILE_PATH"),
	}
}
