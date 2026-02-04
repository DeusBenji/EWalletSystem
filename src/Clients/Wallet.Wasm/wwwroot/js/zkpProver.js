// zkpProver.js - SnarkJS integration for browser-based ZKP proof generation

import * as snarkjs from 'snarkjs';

window.zkpProver = {
    /**
     * Generate a zero-knowledge proof for age verification
     * @param {object} input - Circuit inputs (birthYear, salt, commitment, currentYear, challenge, challengeHash)
     * @returns {Promise<object>} - { proof, publicSignals }
     */
    async generateAgeProof(input) {
        try {
            console.log('[ZKP] Loading circuit WASM and zkey...');

            // Load circuit files from wwwroot
            const wasmResponse = await fetch('/circuits/age_verification.wasm');
            const wasmBuffer = await wasmResponse.arrayBuffer();

            const zkeyResponse = await fetch('/circuits/age_verification_final.zkey');
            const zkeyBuffer = await zkeyResponse.arrayBuffer();

            console.log('[ZKP] Generating proof...');
            const startTime = performance.now();

            // Generate proof using SnarkJS
            const { proof, publicSignals } = await snarkjs.groth16.fullProve(
                input,
                new Uint8Array(wasmBuffer),
                new Uint8Array(zkeyBuffer)
            );

            const endTime = performance.now();
            console.log(`[ZKP] Proof generated in ${(endTime - startTime).toFixed(0)}ms`);
            console.log('[ZKP] Public signals:', publicSignals);

            return {
                proof: proof,
                publicSignals: publicSignals
            };

        } catch (error) {
            console.error('[ZKP] Proof generation failed:', error);
            throw new Error(`ZKP proof generation failed: ${error.message}`);
        }
    },

    /**
     * Verify a proof (client-side verification, optional)
     * @param {object} proof - The proof object
     * @param {array} publicSignals - Public signals array
     * @returns {Promise<boolean>} - True if valid
     */
    async verifyProof(proof, publicSignals) {
        try {
            console.log('[ZKP] Loading verification key...');
            const vkeyResponse = await fetch('/circuits/verification_key.json');
            const vkey = await vkeyResponse.json();

            console.log('[ZKP] Verifying proof...');
            const isValid = await snarkjs.groth16.verify(vkey, publicSignals, proof);

            console.log('[ZKP] Proof valid:', isValid);
            return isValid;

        } catch (error) {
            console.error('[ZKP] Proof verification failed:', error);
            return false;
        }
    }
};

console.log('[ZKP] SnarkJS prover module loaded');
