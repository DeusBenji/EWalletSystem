.PHONY: help build run test clean docker-build docker-run deps lint

help: ## Vis denne hjælp
	@grep -E '^[a-zA-Z_-]+:.*?## .*$$' $(MAKEFILE_LIST) | sort | awk 'BEGIN {FS = ":.*?## "}; {printf "\033[36m%-20s\033[0m %s\n", $$1, $$2}'

deps: ## Installer dependencies
	go mod download
	go mod tidy

build: ## Build applikationen
	go build -o bin/fabric-resolver.exe ./cmd/server

run: ## Kør applikationen lokalt
	go run ./cmd/server

test: ## Kør alle tests
	go test -v ./...

test-cover: ## Kør tests med coverage
	go test -cover -coverprofile=coverage.out ./...
	go tool cover -html=coverage.out -o coverage.html

lint: ## Kør linter
	golangci-lint run

clean: ## Ryd op
	rm -rf bin/
	rm -f coverage.out coverage.html

docker-build: ## Build Docker image
	docker build -t fabric-resolver:latest .

docker-run: ## Kør Docker container
	docker run -p 8080:8080 --env-file .env fabric-resolver:latest

docker-compose-up: ## Start hele systemet
	docker-compose up -d

docker-compose-down: ## Stop hele systemet
	docker-compose down

docker-compose-logs: ## Vis logs
	docker-compose logs -f fabric-resolver

watch: ## Auto-reload under udvikling (kræver air)
	air

dev: ## Start development environment
	docker-compose up -d sqlserver redis kafka zookeeper
	@echo "Waiting for services to be ready..."
	@sleep 10
	$(MAKE) run
