# Build Stage
FROM golang:1.23-alpine AS builder
WORKDIR /app

# Copy generic go.mod and go.sum (if any) or just init later
COPY go.mod go.sum ./
RUN go mod download

# Copy source code
COPY . .

# Build the binary
RUN go build -o zkp-server ./cmd/server/main.go

# Runtime Stage
FROM node:20-alpine
WORKDIR /app

# Install snarkjs globally
RUN npm install -g snarkjs@0.7.0

# Copy binary from builder
COPY --from=builder /app/zkp-server .

# Copy Node.js verification scripts
COPY scripts/ /app/scripts/

# Install script dependencies
WORKDIR /app/scripts
RUN npm install
WORKDIR /app

# Create circuits directory for verification keys
RUN mkdir -p /app/circuits

# Expose port (Gorilla mux listens on :8080)
EXPOSE 8080

# Run the binary
CMD ["./zkp-server"]
