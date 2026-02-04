pragma circom 2.1.0;

include "node_modules/circomlib/circuits/poseidon.circom";
include "node_modules/circomlib/circuits/comparators.circom";

/*
 * Age Verification Circuit (ZKP)
 * 
 * Purpose: Prove that a user is 18+ without revealing their exact birth year.
 * 
 * Private Inputs (witness):
 *   - birthYear: User's actual birth year (e.g., 1995)
 *   - salt: Random salt used in commitment (from credential)
 * 
 * Public Inputs:
 *   - commitment: Hash(birthYear || salt) - stored in credential
 *   - currentYear: Current year (e.g., 2024)
 *   - challenge: Server-provided nonce (replay protection)
 *   - challengeHash: Hash(challenge) - for verification
 * 
 * Public Outputs:
 *   - isAdult: 1 if age >= 18, 0 otherwise
 * 
 * Constraints:
 *   1. Commitment binding: Hash(birthYear || salt) == commitment
 *   2. Age check: currentYear - birthYear >= 18
 *   3. Challenge binding: Hash(challenge) == challengeHash
 */

template AgeVerification() {
    // Private inputs (witness)
    signal input birthYear;
    signal input salt;
    
    // Public inputs
    signal input commitment;
    signal input currentYear;
    signal input challenge;
    signal input challengeHash;
    
    // Public output
    signal output isAdult;
    
    // ===== Constraint 1: Commitment Binding =====
    // Verify that the prover knows birthYear and salt that hash to the commitment
    component commitmentHasher = Poseidon(2);
    commitmentHasher.inputs[0] <== birthYear;
    commitmentHasher.inputs[1] <== salt;
    
    // Assert that computed hash matches the public commitment
    commitment === commitmentHasher.out;
    
    // ===== Constraint 2: Age Check =====
    // Calculate age = currentYear - birthYear
    signal age;
    age <== currentYear - birthYear;
    
    // Check if age >= 18
    component ageCheck = GreaterEqThan(8); // 8 bits = max age 255
    ageCheck.in[0] <== age;
    ageCheck.in[1] <== 18;
    
    // Output 1 if adult, 0 otherwise
    isAdult <== ageCheck.out;
    
    // ===== Constraint 3: Challenge Binding (Replay Protection) =====
    // Verify that the challenge matches the expected hash
    component challengeHasher = Poseidon(1);
    challengeHasher.inputs[0] <== challenge;
    
    // Assert that computed challenge hash matches the public challengeHash
    challengeHash === challengeHasher.out;
    
    // ===== Additional Constraints (Sanity Checks) =====
    // Ensure birthYear is reasonable (e.g., between 1900 and currentYear)
    component birthYearMin = GreaterEqThan(12); // 12 bits = max 4095
    birthYearMin.in[0] <== birthYear;
    birthYearMin.in[1] <== 1900;
    birthYearMin.out === 1;
    
    component birthYearMax = LessEqThan(12);
    birthYearMax.in[0] <== birthYear;
    birthYearMax.in[1] <== currentYear;
    birthYearMax.out === 1;
}

// Main component
component main {public [commitment, currentYear, challenge, challengeHash]} = AgeVerification();
