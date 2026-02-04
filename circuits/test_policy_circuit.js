const { buildPoseidon } = require("circomlibjs");
const { writeFileSync } = require("fs");

async function testPolicyCircuit() {
    console.log("=== Policy ZKP v1 Circuit Test (Robust Version) ===\n");

    // 1. Initialize Poseidon
    console.log("1. Initializing Poseidon hash...");
    const poseidon = await buildPoseidon();
    const F = poseidon.F;

    // 2. Generate test inputs (start with small numbers for debugging)
    console.log("2. Generating test inputs...");

    // Use small numbers first to ensure circuit works
    const walletSecret = BigInt(1);
    const challengeNumeric = BigInt(2);
    const policyIdNumeric = BigInt(3);

    // Compute public inputs using Poseidon
    const subjectCommitment = poseidon([walletSecret]);
    const challengeHash = poseidon([challengeNumeric]);
    const policyHash = poseidon([policyIdNumeric]);
    const sessionTag = poseidon([walletSecret, challengeHash, policyHash]);

    const input = {
        walletSecret: F.toString(walletSecret),
        subjectCommitment: F.toString(subjectCommitment),
        challengeHash: F.toString(challengeHash),
        policyHash: F.toString(policyHash),
        sessionTag: F.toString(sessionTag)
    };

    console.log("   Test Input (small numbers for debugging):");
    console.log("   - walletSecret:", F.toString(walletSecret));
    console.log("   - subjectCommitment:", F.toString(subjectCommitment).substring(0, 20) + "...");
    console.log("   - challengeHash:", F.toString(challengeHash).substring(0, 20) + "...");
    console.log("   - policyHash:", F.toString(policyHash).substring(0, 20) + "...");
    console.log("   - sessionTag:", F.toString(sessionTag).substring(0, 20) + "...");
    console.log();

    // Save input
    writeFileSync("policy_test_input.json", JSON.stringify(input, null, 2));
    console.log("   ✅ Saved to policy_test_input.json\n");

    console.log("=== Test Complete ===");
    console.log("Next steps:");
    console.log("1. Generate witness: npx snarkjs wtns calculate policy_zkp_v1_js/policy_zkp_v1.wasm policy_test_input.json policy_witness.wtns");
    console.log("2. Generate proof: npx snarkjs groth16 prove policy_zkp_v1_final.zkey policy_witness.wtns policy_proof.json policy_public.json");
    console.log("3. Verify proof: npx snarkjs groth16 verify policy_v1_verification_key.json policy_public.json policy_proof.json");
}

testPolicyCircuit().catch(console.error);
