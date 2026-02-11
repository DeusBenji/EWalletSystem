# Circuit Build & Signing Guide

**Version:** 1.0  
**Last Updated:** 2026-02-11  
**Status:** Active

---

## 1. Overview

This document specifies the **reproducible build pipeline** and **offline signing ceremony** for ZKP circuit artifacts. This establishes the **root of trust** for the ZKP credential platform.

**Security Goals:**
- ✅ Reproducible builds (deterministic output)
- ✅ Offline signing (CI cannot sign, only verify)
- ✅ Public key embedded at compile-time (validation service)
- ✅ Tamper detection (manifest signature verification)
- ✅ Downgrade protection (minimum circuit version enforcement)

---

## 2. Circuit Build Pipeline

### Build Environment

**Requirement:** Fully deterministic, dockerized build

**Dockerfile:**
```dockerfile
FROM node:20-alpine AS builder

# Install circom compiler (pinned version)
RUN npm install -g circom@2.1.6 snarkjs@0.7.0

# Set reproducible timestamps
ENV SOURCE_DATE_EPOCH=1234567890

WORKDIR /build

# Copy circuit source
COPY circuits/ ./circuits/
COPY package.json package-lock.json ./

# Install dependencies (lockfile ensures determinism)
RUN npm ci --production

# Compile circuit
RUN circom circuits/age_verification.circom \
    --r1cs --wasm --sym \
    --output /build/output

# Generate verification key (trusted setup ceremony output)
RUN snarkjs groth16 setup \
    /build/output/age_verification.r1cs \
    /build/ptau/powersOfTau28_hez_final_12.ptau \
    /build/output/age_verification_0000.zkey

# Export verification key
RUN snarkjs zkey export verificationkey \
    /build/output/age_verification_0000.zkey \
    /build/output/verification_key.json

# Export WASM prover
RUN cp /build/output/age_verification_js/age_verification.wasm \
    /build/output/prover.wasm

# Create manifest
RUN node scripts/create-manifest.js

FROM scratch AS export
COPY --from=builder /build/output/prover.wasm /
COPY --from=builder /build/output/verification_key.json /
COPY --from=builder /build/output/manifest.json /
```

**Key Points:**
- Pinned dependencies (circom, snarkjs versions)
- `SOURCE_DATE_EPOCH` for reproducible timestamps
- `npm ci` (not `npm install`) for lockfile reproducibility
- Output: `prover.wasm`, `verification_key.json`, `manifest.json`

---

## 3. Circuit Manifest

### Structure

**manifest.json:**
```json
{
  "circuitId": "age_verification_v1",
  "version": "1.2.0",
  "buildTimestamp": "2026-02-11T14:00:00Z",
  "artifacts": {
    "prover": {
      "filename": "prover.wasm",
      "sha256": "abc123def456...",
      "size": 123456
    },
    "verificationKey": {
      "filename": "verification_key.json",
      "sha256": "def456abc123...",
      "size": 7890
    }
  },
  "builder": {
    "circomVersion": "2.1.6",
    "snarkjsVersion": "0.7.0",
    "dockerImage": "node:20-alpine"
  },
  "signature": null  // Added by offline signing ceremony
}
```

### Manifest Creation Script

**scripts/create-manifest.js:**
```javascript
const crypto = require('crypto');
const fs = require('fs');

function computeSHA256(filepath) {
  const buffer = fs.readFileSync(filepath);
  return crypto.createHash('sha256').update(buffer).digest('hex');
}

const manifest = {
  circuitId: "age_verification_v1",
  version: "1.2.0",
  buildTimestamp: new Date().toISOString(),
  artifacts: {
    prover: {
      filename: "prover.wasm",
      sha256: computeSHA256('/build/output/prover.wasm'),
      size: fs.statSync('/build/output/prover.wasm').size
    },
    verificationKey: {
      filename: "verification_key.json",
      sha256: computeSHA256('/build/output/verification_key.json'),
      size: fs.statSync('/build/output/verification_key.json').size
    }
  },
  builder: {
    circomVersion: "2.1.6",
    snarkjsVersion: "0.7.0",
    dockerImage: "node:20-alpine"
  },
  signature: null
};

fs.writeFileSync('/build/output/manifest.json', JSON.stringify(manifest, null, 2));
```

---

## 4. Offline Signing Ceremony

### Security Model

**Critical Principle:** CI can VERIFY signatures, but CANNOT SIGN

**Offline Signing Key:**
- Stored on air-gapped machine (USB key in safe)
- NEVER committed to git
- NEVER accessible from CI/CD
- Only held by 2-3 authorized signers (multi-sig optional)

### Key Generation (One-Time)

**On air-gapped machine:**
```bash
# Generate ECDSA key (ES256)
openssl ecparam -genkey -name prime256v1 -out circuit_signing_private.pem

# Extract public key
openssl ec -in circuit_signing_private.pem -pubout -out circuit_signing_public.pem

# Compute public key fingerprint
openssl dgst -sha256 circuit_signing_public.pem | awk '{print $2}'
# Output: abc123def456... (this goes into ValidationService)
```

**Public key embedded in ValidationService:**
```csharp
public static class CircuitSigningPublicKey
{
    // Public key fingerprint (SHA256 of public key file)
    public const string Fingerprint = "abc123def456789...";
    
    // Public key (PEM format)
    public const string PublicKeyPem = @"
-----BEGIN PUBLIC KEY-----
MFkwEwYHKoZIzj0CAQYIKoZIzj0DAQcDQgAE...
-----END PUBLIC KEY-----
";
}
```

### Signing Workflow

**Step 1: Build circuit (on developer machine or CI)**
```bash
docker build -t circuit-builder -f circuits/Dockerfile .
docker run --rm circuit-builder --output ./output
```

**Step 2: Transfer manifest to air-gapped machine**
- Copy `manifest.json` to USB drive
- Physically transport to signing machine

**Step 3: Sign manifest (on air-gapped machine)**
```bash
# Canonical JSON encoding (deterministic order)
jq -S -c . manifest.json > manifest.canonical.json

# Sign
openssl dgst -sha256 -sign circuit_signing_private.pem \
  manifest.canonical.json | base64 -w 0 > manifest.signature

# Add signature to manifest
jq '.signature = "'$(cat manifest.signature)'"' manifest.json > manifest.signed.json
```

**Step 4: Transfer signed manifest back**
- Copy `manifest.signed.json` to USB drive
- Return to developer machine
- Commit signed manifest to git

**Step 5: CI verifies signature (automatic)**
```bash
# Extract signature from manifest
jq -r .signature manifest.signed.json | base64 -d > /tmp/signature

# Remove signature field for canonical verification
jq 'del(.signature)' manifest.signed.json | jq -S -c . > /tmp/canonical.json

# Verify signature
openssl dgst -sha256 -verify circuit_signing_public.pem \
  -signature /tmp/signature /tmp/canonical.json
```

---

## 5. CI Verification Pipeline

**`.github/workflows/circuit-verification.yml`:**
```yaml
name: Circuit Integrity Verification

on:
  pull_request:
    paths:
      - 'circuits/**'
      - 'src/circuits/**'

jobs:
  verify-circuit-signature:
    runs-on: ubuntu-latest
    
    steps:
      - name: Checkout
        uses: actions/checkout@v4
      
      - name: Verify circuit manifest signature
        run: |
          # Extract signature
          jq -r .signature circuits/manifest.signed.json | base64 -d > /tmp/signature
          
          # Create canonical JSON (without signature)
          jq 'del(.signature)' circuits/manifest.signed.json | jq -S -c . > /tmp/canonical.json
          
          # Verify signature with embedded public key
          openssl dgst -sha256 -verify circuits/circuit_signing_public.pem \
            -signature /tmp/signature /tmp/canonical.json
          
          if [ $? -ne 0 ]; then
            echo "❌ CRITICAL: Circuit manifest signature verification FAILED"
            echo "Circuit artifacts may be tampered or unsigned"
            exit 1
          fi
          
          echo "✅ Circuit manifest signature verified"
      
      - name: Verify artifact hashes
        run: |
          # Extract expected hashes from manifest
          EXPECTED_PROVER_HASH=$(jq -r .artifacts.prover.sha256 circuits/manifest.signed.json)
          EXPECTED_VKEY_HASH=$(jq -r .artifacts.verificationKey.sha256 circuits/manifest.signed.json)
          
          # Compute actual hashes
          ACTUAL_PROVER_HASH=$(sha256sum circuits/prover.wasm | awk '{print $1}')
          ACTUAL_VKEY_HASH=$(sha256sum circuits/verification_key.json | awk '{print $1}')
          
          # Compare
          if [ "$EXPECTED_PROVER_HASH" != "$ACTUAL_PROVER_HASH" ]; then
            echo "❌ Prover WASM hash mismatch"
            exit 1
          fi
          
          if [ "$EXPECTED_VKEY_HASH" != "$ACTUAL_VKEY_HASH" ]; then
            echo "❌ Verification key hash mismatch"
            exit 1
          fi
          
          echo "✅ All artifact hashes match manifest"
      
      - name: Block PR if verification fails
        if: failure()
        run: |
          echo "::error::Circuit integrity verification failed. PR BLOCKED."
          exit 1
```

**Result:** PR cannot merge if:
- Manifest signature is invalid
- Manifest is missing
- Artifact hashes don't match manifest

---

## 6. Downgrade Protection

### Minimum Circuit Version

**Extension enforces minimum circuit version:**

```typescript
const MINIMUM_CIRCUIT_VERSIONS: Record<string, string> = {
  "age_verification_v1": "1.2.0",  // Must be >= 1.2.0
  "drivers_license_v1": "1.0.0"
};

async function loadCircuit(circuitId: string, version: string): Promise<Circuit> {
  const minimumVersion = MINIMUM_CIRCUIT_VERSIONS[circuitId];
  
  if (!minimumVersion) {
    throw new Error(`Unknown circuit: ${circuitId}`);
  }
  
  // Semver comparison
  if (!satisfies(version, `>=${minimumVersion}`)) {
    throw new Error(
      `Circuit downgrade rejected: ${circuitId} v${version} < minimum v${minimumVersion}`
    );
  }
  
  // Load circuit...
}
```

### Attack Scenario: Downgrade to Vulnerable Circuit

**Attack:**
1. Attacker finds vulnerability in circuit v1.0.0
2. Attacker serves old v1.0.0 circuit to victim's extension
3. Attacker exploits vulnerability

**Defense:**
1. Extension checks circuit version against minimum
2. v1.0.0 < v1.2.0 (minimum) → **Reject**
3. Extension refuses to load old circuit

**Test:**
```typescript
test("loadCircuit rejects downgrade to v1.0.0", async () => {
  await expect(
    loadCircuit("age_verification_v1", "1.0.0")
  ).rejects.toThrow("Circuit downgrade rejected");
});
```

---

## 7. Key Ceremony Documentation

### Participants

**Signers (2 of 3 multi-sig):**
- Security Lead
- CTO
- External Auditor (optional)

**Frequency:** Annual or on compromise

### Procedure

1. **Generate key (air-gapped machine)**
2. **Split key using Shamir's Secret Sharing (optional)**
   - 3 shares created
   - 2 shares required to reconstruct
3. **Store shares:**
   - Share 1: Physical safe (office)
   - Share 2: Bank vault
   - Share 3: External auditor
4. **Document public key fingerprint**
   - Add to `CircuitSigningPublicKey.cs`
   - Commit to git
5. **Rotate old key:**
   - Mark old public key as deprecated
   - Grace period: 30 days (allow old circuit signatures)
   - After grace: Block old signatures

---

## 8. Operational Playbook

### Scenario: New Circuit Version

1. Developer builds circuit locally
2. Developer transfers manifest to signing machine (USB)
3. Signer verifies build matches review
4. Signer signs manifest (offline)
5. Developer commits signed manifest
6. CI verifies signature automatically
7. PR merged after signature verification

**RTO:** 1-2 hours (includes physical USB transfer)

### Scenario: Compromise of Signing Key

**RTO:** 4 hours  
**RPO:** 0

**Steps:**
1. **Detect:** Unauthorized signature discovered
2. **Rotate:** Generate new signing key (emergency ceremony)
3. **Update:** Embed new public key in ValidationService
4. **Deploy:** Emergency deployment (hotfix)
5. **Invalidate:** Old public key marked as compromised
6. **Audit:** Review all signatures from old key

---

## 9. Testing Strategy

### Unit Tests

**CircuitSignatureVerifier:**
- Valid signature → Accept
- Invalid signature → Reject
- Missing signature → Reject
- Tampered manifest (hash mismatch) → Reject

### Integration Tests

**CI Pipeline:**
- Unsigned manifest → Block PR
- Wrong signature → Block PR
- Tampered WASM (hash mismatch) → Block PR
- Valid signature → Pass

### Security Tests

**Downgrade Protection:**
- Load v1.0.0 when minimum v1.2.0 → Reject
- Load v1.3.0 when minimum v1.2.0 → Accept
- Unknown circuit ID → Reject

---

## 10. Compliance & Audit Trail

**Audit Log Requirements:**
- Every circuit signature event logged
- Signer identity recorded
- Timestamp (ISO 8601)
- Circuit version + fingerprint
- Public key used

**Retention:** 7 years (compliance requirement)

---

## References

- [Threat Model](file:///e:/Ny%20mappe%20%282%29/EWalletSystem/docs/threat-model.md)
- [Security Invariants](file:///e:/Ny%20mappe%20%282%29/EWalletSystem/docs/security-invariants.md)
- [Revocation Model](file:///e:/Ny%20mappe%20%282%29/EWalletSystem/docs/revocation-model.md)

---

**Approval Status:** Ready for implementation  
**Reviewed By:** Security team, CTO  
**Version:** 1.0 (2026-02-11)
