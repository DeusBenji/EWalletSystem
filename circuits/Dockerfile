FROM node:20-alpine AS builder

# Install circom compiler and snarkjs (pinned versions for reproducibility)
RUN npm install -g circom@2.1.6 snarkjs@0.7.0

# Set reproducible timestamps
ENV SOURCE_DATE_EPOCH=1234567890

WORKDIR /build

# Copy circuit source files
COPY age_verification.circom ./
COPY package.json package-lock.json ./

# Install dependencies (lockfile ensures determinism)
RUN npm ci --production

# Compile circuit
RUN circom age_verification.circom \
    --r1cs --wasm --sym \
    --output /build/output

# Copy trusted setup ceremony file (pre-generated)
# In production, this would come from a trusted ceremony (e.g., Hermez PTAU)
COPY ptau/powersOfTau28_hez_final_12.ptau /build/ptau/

# Generate verification key from r1cs
RUN snarkjs groth16 setup \
    /build/output/age_verification.r1cs \
    /build/ptau/powersOfTau28_hez_final_12.ptau \
    /build/output/age_verification_0000.zkey

# Export verification key (JSON format)
RUN snarkjs zkey export verificationkey \
    /build/output/age_verification_0000.zkey \
    /build/output/verification_key.json

# Copy WASM prover
RUN cp /build/output/age_verification_js/age_verification.wasm \
    /build/output/prover.wasm

# Create manifest with artifact hashes
COPY scripts/create-manifest.js ./scripts/
RUN node scripts/create-manifest.js

# Export stage - only artifacts, no build tools
FROM scratch AS export
COPY --from=builder /build/output/prover.wasm /
COPY --from=builder /build/output/verification_key.json /
COPY --from=builder /build/output/manifest.json /

# To build and extract artifacts:
# docker build -t age-verification-circuit -f Dockerfile .
# docker run --rm age-verification-circuit --output ./dist
