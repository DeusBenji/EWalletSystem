// poseidon.js - Poseidon hash wrapper using circomlibjs
// Used for computing commitments and challenge hashes in ZKP circuits

import { buildPoseidon } from 'circomlibjs';

let poseidonInstance = null;

/**
 * Initialize Poseidon hash function
 * @returns {Promise<void>}
 */
async function initPoseidon() {
    if (!poseidonInstance) {
        console.log('[Poseidon] Initializing Poseidon hash...');
        poseidonInstance = await buildPoseidon();
        console.log('[Poseidon] Initialized successfully');
    }
}

/**
 * Compute Poseidon hash of a single input
 * @param {string} input - Hex-encoded input value
 * @returns {Promise<string>} Hex-encoded hash output
 */
async function hashSingle(input) {
    await initPoseidon();

    try {
        // Convert hex string to BigInt
        const inputBigInt = BigInt('0x' + input);

        // Compute hash
        const hash = poseidonInstance([inputBigInt]);

        // Convert to string (decimal)
        const hashStr = poseidonInstance.F.toString(hash);

        console.log('[Poseidon] Hash computed:', {
            input: input.substring(0, 16) + '...',
            output: hashStr.substring(0, 16) + '...'
        });

        return hashStr;
    } catch (error) {
        console.error('[Poseidon] Error computing hash:', error);
        throw error;
    }
}

/**
 * Compute Poseidon hash of multiple inputs
 * @param {string[]} inputs - Array of hex-encoded input values
 * @returns {Promise<string>} Hex-encoded hash output
 */
async function hashMultiple(inputs) {
    await initPoseidon();

    try {
        // Convert hex strings to BigInts
        const inputBigInts = inputs.map(input => BigInt('0x' + input));

        // Compute hash
        const hash = poseidonInstance(inputBigInts);

        // Convert to string (decimal)
        const hashStr = poseidonInstance.F.toString(hash);

        console.log('[Poseidon] Multi-input hash computed:', {
            inputCount: inputs.length,
            output: hashStr.substring(0, 16) + '...'
        });

        return hashStr;
    } catch (error) {
        console.error('[Poseidon] Error computing multi-input hash:', error);
        throw error;
    }
}

/**
 * Compute commitment: Poseidon(walletSecret)
 * @param {string} secret - Hex-encoded wallet secret
 * @returns {Promise<string>} Commitment (decimal string)
 */
async function computeCommitment(secret) {
    console.log('[Poseidon] Computing commitment from secret');
    return await hashSingle(secret);
}

/**
 * Compute challenge hash: Poseidon(challenge)
 * @param {string} challenge - Challenge string (will be converted to numeric)
 * @returns {Promise<string>} Challenge hash (decimal string)
 */
async function computeChallengeHash(challenge) {
    console.log('[Poseidon] Computing challenge hash');

    // Convert challenge string to numeric value (simple hash)
    const challengeNumeric = Math.abs(challenge.split('').reduce((acc, char) =>
        acc + char.charCodeAt(0), 0
    )).toString(16).padStart(64, '0');

    return await hashSingle(challengeNumeric);
}

/**
 * Compute policy hash: Poseidon(policyId)
 * @param {string} policyId - Policy identifier (e.g., "age_over_18")
 * @returns {Promise<string>} Policy hash (decimal string)
 */
async function computePolicyHash(policyId) {
    console.log('[Poseidon] Computing policy hash for:', policyId);

    // Convert policyId string to numeric value
    const policyNumeric = policyId.split('').reduce((acc, char) =>
        acc + char.charCodeAt(0), 0
    ).toString(16).padStart(64, '0');

    return await hashSingle(policyNumeric);
}

// Export to global window object for C# JSInterop
window.poseidon = {
    hashSingle,
    hashMultiple,
    computeCommitment,
    computeChallengeHash,
    computePolicyHash
};

console.log('[Poseidon] Module loaded');
