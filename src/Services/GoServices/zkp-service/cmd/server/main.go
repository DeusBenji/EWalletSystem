package main

import (
	"encoding/json"
	"log"
	"net/http"
	"time"

	"zkp-service/internal/api"
	"zkp-service/internal/keys"

	"github.com/gorilla/mux"
)

func main() {
	// Initialize ZK Keys (Setup phase)
	keys.Init()

	r := mux.NewRouter()

	// Middleware
	r.Use(loggingMiddleware)

	// Routes
	r.HandleFunc("/health", healthHandler).Methods("GET")

	// API V1
	// We will inject dependencies (like loaded keys) into the handler later
	r.HandleFunc("/verify/age-v1", api.VerifyAgeV1Handler).Methods("POST")
	r.HandleFunc("/utils/hash", api.HashHandler).Methods("POST")

	srv := &http.Server{
		Handler:      r,
		Addr:         "0.0.0.0:8080",
		WriteTimeout: 15 * time.Second,
		ReadTimeout:  15 * time.Second,
	}

	log.Println("ZKP Service running on port 8080...")
	log.Fatal(srv.ListenAndServe())
}

func healthHandler(w http.ResponseWriter, r *http.Request) {
	w.Header().Set("Content-Type", "application/json")
	json.NewEncoder(w).Encode(map[string]string{"status": "ok", "service": "zkp-service"})
}

func loggingMiddleware(next http.Handler) http.Handler {
	return http.HandlerFunc(func(w http.ResponseWriter, r *http.Request) {
		start := time.Now()
		next.ServeHTTP(w, r)
		log.Printf("%s %s %s", r.Method, r.RequestURI, time.Since(start))
	})
}
