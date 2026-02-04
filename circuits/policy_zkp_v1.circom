pragma circom 2.2.0;

include "node_modules/circomlib/circuits/poseidon.circom";

/*
 * Policy ZKP v1 - Universal Policy Verification Circuit (Super Robust Version)
 * 
 * Purpose: Prove knowledge of a wallet secret with replay and policy protection.
 * 
 * Design Philosophy:
 * - Circuit COMPUTES commitment and sessionTag as public outputs
 * - No risk of Poseidon mismatch between JS and circuit
 * - Binding checks done in ValidationService plugin
 * - Witness generation is idiot-proof
 * 
 * Private Inputs (witness):
 *   - walletSecret: User's wallet secret (field element)
 * 
 * Public Inputs:
 *   - challengeHash: Poseidon(challenge) - computed in JS
 *   - policyHash: Poseidon(policyId) - computed in JS
 * 
 * Public Outputs (computed by circuit):
 *   - subjectCommitment: Poseidon(walletSecret)
 *   - sessionTag: Poseidon(walletSecret, challengeHash, policyHash)
 * 
 * Security Properties:
 *   - Zero-knowledge: walletSecret never revealed
 *   - Binding: ValidationService checks vc.commitment == proof.subjectCommitment
 *   - Replay protection: sessionTag changes with challengeHash
 *   - Policy isolation: sessionTag changes with policyHash
 */

template PolicyZkpV1() {
    // ===== Private Input (Witness) =====
    signal input walletSecret;
    
    // ===== Public Inputs =====
    signal input challengeHash;
    signal input policyHash;
    
    // ===== Public Outputs (Computed by Circuit) =====
    signal output subjectCommitment;
    signal output sessionTag;
    
    // ===== Compute subjectCommitment =====
    // subjectCommitment = Poseidon(walletSecret)
    component commitmentHasher = Poseidon(1);
    commitmentHasher.inputs[0] <== walletSecret;
    subjectCommitment <== commitmentHasher.out;
    
    // ===== Compute sessionTag =====
    // sessionTag = Poseidon(walletSecret, challengeHash, policyHash)
    component sessionHasher = Poseidon(3);
    sessionHasher.inputs[0] <== walletSecret;
    sessionHasher.inputs[1] <== challengeHash;
    sessionHasher.inputs[2] <== policyHash;
    sessionTag <== sessionHasher.out;
}

// Main component
// Public inputs: challengeHash, policyHash
// Public outputs: subjectCommitment, sessionTag
// Private inputs: walletSecret
component main {public [challengeHash, policyHash]} = PolicyZkpV1();
