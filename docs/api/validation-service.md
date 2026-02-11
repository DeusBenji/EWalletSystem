# ValidationService API Reference

## Overview

The ValidationService validates zero-knowledge proof envelopes submitted by websites. It enforces security invariants including anti-downgrade protection, origin binding, and replay prevention.

**Base URL:** `https://validation-service.zkp-wallet.dev/api`  
**Version:** v1.0  
**Protocol:** REST (JSON)

---

## Core Concepts

### Proof Envelope
Container for ZKP proof, public signals, and metadata required for validation.

**Structure:**
```typescript
interface ProofEnvelope {
  version: string;           // Protocol version (e.g., "1.0")
  policyId: string;          // Policy identifier
  policyVersion: string;     // Policy semantic version
  circuitVersion: string;    // Circuit version
  proof: ZKProof;            // The actual ZK proof
  publicSignals: PublicSignals;
  metadata: ProofMetadata;
}
```

### Validation Rules
1. **Anti-downgrade:** Policy/circuit versions ≥ minimum required
2. **Origin binding:** Proof bound to requesting website
3. **Replay prevention:** Challenge nonce must be unique and recent
4. **Clock skew:** ±5 minutes tolerance
5. **Signature verification:** Proof signature must be valid

---

## API Endpoints

### POST /api/validate

Validates a zero-knowledge proof envelope.

**Request:**
```http
POST /api/validate HTTP/1.1
Content-Type: application/json

{
  "version": "1.0",
  "policyId": "age_over_18",
  "policyVersion": "1.0.0",
  "circuitVersion": "1.0.0",
  "proof": {
    "pi_a": ["0x123...", "0x456..."],
    "pi_b": [["0x789...", "0xabc..."], ["0xdef...", "0x012..."]],
    "pi_c": ["0x345...", "0x678..."],
    "protocol": "groth16",
    "curve": "bn128"
  },
  "publicSignals": {
    "claimResult": true,
    "policyHash": "0x9abc...",
    "credentialHash": "0xdef0...",
    "timestamp": 1704067200
  },
  "metadata": {
    "origin": "https://example.com",
    "challenge": {
      "nonce": "random-nonce-123",
      "timestamp": 1704067200,
      "origin": "https://example.com"
    },
    "signature": "0x signature...",
    "timestamp": 1704067200
  }
}
```

**Request Model:**
```typescript
interface ProofEnvelope {
  version: string;
  policyId: string;
  policyVersion: string;
  circuitVersion: string;
  proof: ZKProof;
  publicSignals: PublicSignals;
  metadata: ProofMetadata;
}

interface ZKProof {
  pi_a: string[];          // Groth16 proof component A
  pi_b: string[][];        // Groth16 proof component B
  pi_c: string[];          // Groth16 proof component C
  protocol: 'groth16';     // ZK protocol
  curve: 'bn128';          // Elliptic curve
}

interface PublicSignals {
  claimResult: boolean;    // The claim result (e.g., isOver18: true)
  policyHash: string;      // Hash of policy definition
  credentialHash: string;  // Hash of credential (prevents tampering)
  timestamp: number;       // Proof generation timestamp (Unix)
}

interface ProofMetadata {
  origin: string;          // Website origin (for binding)
  challenge: Challenge;    // Anti-replay challenge
  signature: string;       // Proof signature (device-bound)
  timestamp: number;       // Envelope creation timestamp
}

interface Challenge {
  nonce: string;           // Random nonce from website
  timestamp: number;       // Challenge timestamp
  origin: string;          // Challenge origin (must match metadata.origin)
}
```

**Response (200 OK):**
```json
{
  "valid": true,
  "policyId": "age_over_18",
  "claimResult": true,
  "validatedAt": "2026-02-11T16:00:00Z",
  "origin": "https://example.com"
}
```

**Response Model:**
```typescript
interface ValidationResponse {
  valid: boolean;          // Overall validation result
  policyId: string;        // Validated policy
  claimResult: boolean;    // The claim result
  validatedAt: string;     // ISO 8601 validation timestamp
  origin: string;          // Validated origin
  errorCode?: string;      // Error code (if valid: false)
  errorMessage?: string;   // Human-readable error
}
```

**Errors:**
- `400 Bad Request` - Malformed proof envelope
- `403 Forbidden` - Validation failed (anti-downgrade, origin mismatch, etc.)
- `500 Internal Server Error` - Server error

**Example (curl):**
```bash
curl -X POST https://validation-service.zkp-wallet.dev/api/validate \
  -H "Content-Type: application/json" \
  -d @proof-envelope.json
```

**Example (JavaScript):**
```javascript
// On website backend
const response = await fetch('https://validation-service.zkp-wallet.dev/api/validate', {
  method: 'POST',
  headers: { 'Content-Type': 'application/json' },
  body: JSON.stringify(proofEnvelope)
});

const result = await response.json();

if (result.valid && result.claimResult) {
  // User proved they are over 18
  grantAccess();
} else {
  // Validation failed or user doesn't meet criteria
  denyAccess();
}
```

---

### GET /api/policies/{policyId}/versions

Gets version information for a specific policy.

**Request:**
```http
GET /api/policies/age_over_18/versions HTTP/1.1
```

**Response (200 OK):**
```json
{
  "policyId": "age_over_18",
  "latestVersion": "1.2.0",
  "minimumVersion": "1.0.0",
  "versions": [
    {
      "version": "1.0.0",
      "status": "active",
      "publishedAt": "2026-01-01T00:00:00Z"
    },
    {
      "version": "1.1.0",
      "status": "active",
      "publishedAt": "2026-01-15T00:00:00Z"
    },
    {
      "version": "1.2.0",
      "status": "active",
      "publishedAt": "2026-02-01T00:00:00Z"
    }
  ]
}
```

**Response Model:**
```typescript
interface PolicyVersionsResponse {
  policyId: string;
  latestVersion: string;
  minimumVersion: string;   // Minimum accepted version (anti-downgrade)
  versions: PolicyVersion[];
}

interface PolicyVersion {
  version: string;
  status: 'active' | 'deprecated' | 'sunset';
  publishedAt: string;
}
```

---

### GET /api/health

Health check endpoint.

**Request:**
```http
GET /api/health HTTP/1.1
```

**Response (200 OK):**
```json
{
  "status": "healthy",
  "version": "1.0.0",
  "timestamp": "2026-02-11T16:00:00Z"
}
```

---

## Validation Logic

### 1. Anti-Downgrade Protection

```csharp
// Pseudo-code
if (envelope.policyVersion < policy.MinimumVersion) {
    return ValidationError(
        code: "ANTI_DOWNGRADE_VIOLATION",
        message: $"Policy version {envelope.policyVersion} < minimum {policy.MinimumVersion}"
    );
}

if (envelope.circuitVersion < circuit.MinimumVersion) {
    return ValidationError(
        code: "ANTI_DOWNGRADE_VIOLATION",
        message: $"Circuit version {envelope.circuitVersion} < minimum {circuit.MinimumVersion}"
    );
}
```

### 2. Origin Binding

```csharp
if (envelope.metadata.origin != envelope.metadata.challenge.origin) {
    return ValidationError(
        code: "ORIGIN_MISMATCH",
        message: "Envelope origin doesn't match challenge origin"
    );
}

// Also validated by website (check X-Forwarded-Host or similar)
```

### 3. Replay Prevention

```csharp
// Check nonce hasn't been used
if (await nonceCache.ExistsAsync(envelope.metadata.challenge.nonce)) {
    return ValidationError(
        code: "NONCE_ALREADY_USED",
        message: "Challenge nonce has already been used"
    );
}

// Store nonce for 10 minutes
await nonceCache.SetAsync(
    envelope.metadata.challenge.nonce, 
    "used", 
    expiry: TimeSpan.FromMinutes(10)
);
```

### 4. Clock Skew Tolerance

```csharp
var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
var skew = Math.Abs(now - envelope.metadata.timestamp);

if (skew > 300) { // 5 minutes
    return ValidationError(
        code: "TIMESTAMP_OUT_OF_RANGE",
        message: $"Clock skew {skew}s exceeds maximum 300s"
    );
}
```

### 5. ZK Proof Verification

```csharp
var verificationKey = await circuitRegistry.GetVerificationKeyAsync(
    envelope.policyId, 
    envelope.circuitVersion
);

var isValid = await zkVerifier.VerifyProofAsync(
    proof: envelope.proof,
    publicSignals: envelope.publicSignals,
    verificationKey: verificationKey
);

if (!isValid) {
    return ValidationError(
        code: "PROOF_VERIFICATION_FAILED",
        message: "ZK proof verification failed"
    );
}
```

---

## Error Codes

| Code | Description | Action |
|------|-------------|--------|
| `ANTI_DOWNGRADE_VIOLATION` | Version below minimum | Update extension |
| `ORIGIN_MISMATCH` | Origin binding failed | Reject (possible attack) |
| `NONCE_ALREADY_USED` | Replay attack detected | Reject |
| `TIMESTAMP_OUT_OF_RANGE` | Clock skew too large | Check system time |
| `PROOF_VERIFICATION_FAILED` | Invalid ZK proof | Reject (corrupted/fake proof) |
| `POLICY_NOT_FOUND` | Unknown policy | Check policy ID |
| `INVALID_PUBLIC_SIGNALS` | Public signals malformed | Reject |
| `SIGNATURE_INVALID` | Metadata signature invalid | Reject (tampered envelope) |

---

## Performance

### Benchmarks

| Operation | Target | Actual |
|-----------|--------|--------|
| Proof validation | < 100ms | ~80ms |
| Origin check | < 1ms | ~0.5ms |
| Anti-downgrade check | < 1ms | ~0.3ms |
| Full validation | < 150ms | ~120ms |

### Optimization

```csharp
// Cache verification keys (frequently used)
var cachedVKey = await cache.GetOrSetAsync(
    $"vkey:{policyId}:{circuitVersion}",
    async () => await circuitRegistry.GetVerificationKeyAsync(...),
    expiry: TimeSpan.FromHours(1)
);
```

---

## Configuration

### Environment Variables

```bash
# Database
DATABASE_CONNECTION_STRING=Server=localhost;Database=Validation;

# Circuit Registry
CIRCUIT_REGISTRY_URL=https://circuit-registry.zkp-wallet.dev

# Policy Registry
POLICY_REGISTRY_URL=https://policy-registry.zkp-wallet.dev

# Redis (for nonce cache)
REDIS_CONNECTION_STRING=localhost:6379

# Monitoring
PROMETHEUS_ENDPOINT=:9090
```

---

## Monitoring & Observability

### Metrics

```yaml
# Prometheus metrics
zkp_validation_total{result="success|failure",policy_id="age_over_18"}
zkp_validation_duration_seconds{policy_id="age_over_18"}
zkp_anti_downgrade_violations_total{policy_id="age_over_18"}
zkp_replay_attacks_total
zkp_origin_mismatches_total
```

### Logging

```json
{
  "timestamp": "2026-02-11T16:00:00Z",
  "level": "INFO",
  "message": "Proof validated successfully",
  "policyId": "age_over_18",
  "origin": "https://example.com",
  "validationDuration": "85ms",
  "claimResult": true
}
```

**NO PII IN LOGS** - Never log user identifiers, credentials, or personal data.

---

## Security Considerations

### HTTPS Only

Service must only be accessed via HTTPS. HTTP requests should be rejected.

### Rate Limiting

```
- Per origin: 100 validations/minute
- Per IP: 1000 validations/minute
- Global: 100,000 validations/minute
```

### DDoS Protection

- Request size limit: 50KB
- Timeout: 5 seconds
- Circuit breaker for dependent services

---

## Testing

### Test Proof Envelope

```json
{
  "version": "1.0",
  "policyId": "age_over_18",
  "policyVersion": "1.0.0",
  "circuitVersion": "1.0.0",
  "proof": {
    "pi_a": ["0x123...", "0x456..."],
    "pi_b": [["0x789...", "0xabc..."], ["0xdef...", "0x012..."]],
    "pi_c": ["0x345...", "0x678..."],
    "protocol": "groth16",
    "curve": "bn128"
  },
  "publicSignals": {
    "claimResult": true,
    "policyHash": "0x9abc...",
    "credentialHash": "0xdef0...",
    "timestamp": 1704067200
  },
  "metadata": {
    "origin": "https://test.example.com",
    "challenge": {
      "nonce": "test-nonce-456",
      "timestamp": 1704067200,
      "origin": "https://test.example.com"
    },
    "signature": "0xtest_signature",
    "timestamp": 1704067200
  }
}
```

---

## Support

- **Documentation:** https://docs.zkp-wallet.dev/validation-service
- **Issues:** https://github.com/zkp-wallet/validation-service/issues
- **Email:** api-support@zkp-wallet.dev

---

## Changelog

### v1.0.0 (2026-02-11)
- Initial release
- Anti-downgrade protection
- Origin binding
- Replay prevention
- Groth16 proof verification
