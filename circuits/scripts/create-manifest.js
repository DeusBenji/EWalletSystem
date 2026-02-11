const crypto = require('crypto');
const fs = require('fs');
const path = require('path');

/**
 * Creates a signed manifest for circuit artifacts.
 * This manifest is later signed offline by the circuit signing key.
 */

function computeSHA256(filepath) {
  const buffer = fs.readFileSync(filepath);
  return crypto.createHash('sha256').update(buffer).digest('hex');
}

function getFileSize(filepath) {
  return fs.statSync(filepath).size;
}

// Paths to artifacts
const OUTPUT_DIR = '/build/output';
const PROVER_WASM = path.join(OUTPUT_DIR, 'prover.wasm');
const VERIFICATION_KEY = path.join(OUTPUT_DIR, 'verification_key.json');
const MANIFEST_PATH = path.join(OUTPUT_DIR, 'manifest.json');

// Create manifest
const manifest = {
  circuitId: "age_verification_v1",
  version: process.env.CIRCUIT_VERSION || "1.2.0",
  buildTimestamp: new Date().toISOString(),
  
  artifacts: {
    prover: {
      filename: "prover.wasm",
      sha256: computeSHA256(PROVER_WASM),
      size: getFileSize(PROVER_WASM)
    },
    verificationKey: {
      filename: "verification_key.json",
      sha256: computeSHA256(VERIFICATION_KEY),
      size: getFileSize(VERIFICATION_KEY)
    }
  },
  
  builder: {
    circomVersion: "2.1.6",
    snarkjsVersion: "0.7.0",
    dockerImage: "node:20-alpine",
    sourceDateEpoch: process.env.SOURCE_DATE_EPOCH || "1234567890"
  },
  
  // Signature added by offline signing ceremony
  signature: null
};

// Write manifest (canonical JSON format for signing)
fs.writeFileSync(MANIFEST_PATH, JSON.stringify(manifest, null, 2));

console.log('✅ Circuit manifest created successfully');
console.log(`  Circuit ID: ${manifest.circuitId}`);
console.log(`  Version: ${manifest.version}`);
console.log(`  Prover hash: ${manifest.artifacts.prover.sha256.substring(0, 16)}...`);
console.log(`  VKey hash: ${manifest.artifacts.verificationKey.sha256.substring(0, 16)}...`);
console.log('');
console.log('⚠️  Next step: Transfer manifest.json to air-gapped machine for signing');
