package api

import (
	"encoding/json"
	"log"
	"net/http"
	"time"

	"fabric-resolver/internal/api/handlers"
	"fabric-resolver/internal/infrastructure/fabric"

	"github.com/gorilla/mux"
	"github.com/prometheus/client_golang/prometheus/promhttp"
)

// NewRouter creates and configures the HTTP router
func NewRouter(ledgerClient fabric.LedgerClient) *mux.Router {
	r := mux.NewRouter()

	// Middleware
	r.Use(loggingMiddleware)
	r.Use(corsMiddleware)

	// Health check
	r.HandleFunc("/health", healthHandler).Methods("GET")

	// Stats endpoint for debugging
	r.HandleFunc("/stats", statsHandler(ledgerClient)).Methods("GET")

	// Anchor handlers
	anchorHandler := handlers.NewAnchorHandler(ledgerClient)
	r.HandleFunc("/anchors", anchorHandler.CreateAnchor).Methods("POST")
	r.HandleFunc("/anchors/{hash}", anchorHandler.GetAnchor).Methods("GET")
	r.HandleFunc("/anchors/{hash}/verify", anchorHandler.VerifyAnchor).Methods("GET")

	// DID handlers
	didHandler := handlers.NewDidHandler(ledgerClient)
	r.HandleFunc("/dids", didHandler.CreateDid).Methods("POST")
	r.HandleFunc("/dids/{did:.*}", didHandler.ResolveDid).Methods("GET")

	// Metrics
	r.Handle("/metrics", promhttp.Handler()).Methods("GET")

	return r
}

func loggingMiddleware(next http.Handler) http.Handler {
	return http.HandlerFunc(func(w http.ResponseWriter, r *http.Request) {
		start := time.Now()
		next.ServeHTTP(w, r)
		log.Printf(
			"%s %s %s %v",
			r.Method,
			r.RequestURI,
			r.RemoteAddr,
			time.Since(start),
		)
	})
}

func corsMiddleware(next http.Handler) http.Handler {
	return http.HandlerFunc(func(w http.ResponseWriter, r *http.Request) {
		// TODO: Configure CORS properly for production
		// For now using wildcard for development
		w.Header().Set("Access-Control-Allow-Origin", "*")
		w.Header().Set("Access-Control-Allow-Methods", "GET, POST, OPTIONS")
		w.Header().Set("Access-Control-Allow-Headers", "Content-Type, Authorization")

		if r.Method == "OPTIONS" {
			w.WriteHeader(http.StatusOK)
			return
		}

		next.ServeHTTP(w, r)
	})
}

func healthHandler(w http.ResponseWriter, r *http.Request) {
	response := map[string]interface{}{
		"status":    "healthy",
		"timestamp": time.Now().UTC().Format(time.RFC3339),
		"service":   "fabric-resolver",
	}

	w.Header().Set("Content-Type", "application/json")
	if err := json.NewEncoder(w).Encode(response); err != nil {
		log.Printf("ERROR: Failed to encode health response: %v", err)
	}
}

// statsHandler returns statistics from the Fabric client (for debugging)
func statsHandler(ledgerClient fabric.LedgerClient) http.HandlerFunc {
	return func(w http.ResponseWriter, r *http.Request) {
		stats := ledgerClient.GetStats()
		stats["timestamp"] = time.Now().UTC().Format(time.RFC3339)

		w.Header().Set("Content-Type", "application/json")
		if err := json.NewEncoder(w).Encode(stats); err != nil {
			log.Printf("ERROR: Failed to encode stats response: %v", err)
		}
	}
}
