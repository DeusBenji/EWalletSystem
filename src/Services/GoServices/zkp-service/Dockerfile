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
FROM alpine:latest
WORKDIR /app

# Copy binary from builder
COPY --from=builder /app/zkp-server .

# Expose port (Gorilla mux listens on :8080)
EXPOSE 8080

# Run the binary
CMD ["./zkp-server"]
