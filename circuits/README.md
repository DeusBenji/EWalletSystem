# Age Verification Circuit

This directory contains the Circom circuit for zero-knowledge age verification.

## Circuit: `age_verification.circom`

### Purpose
Prove that a user is 18+ without revealing their exact birth year.

### Inputs

**Private (Witness):**
- `birthYear`: User's actual birth year (e.g., 1995)
- `salt`: Random salt from credential

**Public:**
- `commitment`: Hash(birthYear || salt) - stored in credential
- `currentYear`: Current year (e.g., 2024)
- `challenge`: Server nonce (replay protection)
- `challengeHash`: Hash(challenge)

### Output
- `isAdult`: 1 if age >= 18, 0 otherwise

### Constraints
1. **Commitment Binding**: `Hash(birthYear || salt) == commitment`
2. **Age Check**: `currentYear - birthYear >= 18`
3. **Challenge Binding**: `Hash(challenge) == challengeHash`
4. **Sanity Checks**: `1900 <= birthYear <= currentYear`

## Compilation

```bash
# Install circomlib (Poseidon hash library)
npm install circomlib

# Compile circuit
circom age_verification.circom --r1cs --wasm --sym --c

# This generates:
# - age_verification.r1cs (constraint system)
# - age_verification_js/age_verification.wasm (witness calculator)
# - age_verification.sym (symbol table for debugging)
```

## Trusted Setup

```bash
# Download Powers of Tau (universal setup)
wget https://hermez.s3-eu-west-1.amazonaws.com/powersOfTau28_hez_final_15.ptau

# Generate zkey (proving key)
snarkjs groth16 setup age_verification.r1cs powersOfTau28_hez_final_15.ptau age_verification_0000.zkey

# Contribute to ceremony (optional, for production)
snarkjs zkey contribute age_verification_0000.zkey age_verification_final.zkey --name="First contribution"

# Export verification key
snarkjs zkey export verificationkey age_verification_final.zkey verification_key.json
```

## Testing

```bash
# Create test input
echo '{
  "birthYear": "1995",
  "salt": "12345678901234567890123456789012",
  "commitment": "...",
  "currentYear": "2024",
  "challenge": "test-challenge-123",
  "challengeHash": "..."
}' > input.json

# Calculate witness
node age_verification_js/generate_witness.js age_verification_js/age_verification.wasm input.json witness.wtns

# Generate proof
snarkjs groth16 prove age_verification_final.zkey witness.wtns proof.json public.json

# Verify proof
snarkjs groth16 verify verification_key.json public.json proof.json
```

## Circuit Stats
- **Constraints**: ~500 (estimated)
- **Proof Generation Time**: <1s (browser)
- **Proof Size**: ~200 bytes
- **Verification Time**: ~10ms
