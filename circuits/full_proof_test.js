const { buildPoseidon } = require("circomlibjs");
const wc = require("./age_verification_js/witness_calculator.js");
const { readFileSync, writeFileSync } = require("fs");

async function generateProof() {
    console.log("=== Circom ZKP Proof Generation Test ===\n");

    // 1. Generate correct input
    console.log("1. Generating input with correct Poseidon hashes...");
    const poseidon = await buildPoseidon();
    const F = poseidon.F;

    const birthYear = 1995;
    const salt = 12345678901234567890123456789012n; // Use BigInt literal
    const currentYear = 2024;
    const challenge = 99999n;

    const commitment = poseidon([birthYear, salt]);
    const challengeHash = poseidon([challenge]);

    const input = {
        birthYear: birthYear.toString(),
        salt: salt.toString(),
        commitment: F.toString(commitment),
        currentYear: currentYear.toString(),
        challenge: challenge.toString(),
        challengeHash: F.toString(challengeHash)
    };

    console.log("   Input:", JSON.stringify(input, null, 2));
    writeFileSync("test_input_final.json", JSON.stringify(input, null, 2));
    console.log("   ✅ Saved to test_input_final.json\n");

    // 2. Generate witness
    console.log("2. Generating witness...");
    const buffer = readFileSync("age_verification_js/age_verification.wasm");
    const witnessCalculator = await wc(buffer);
    const witness = await witnessCalculator.calculateWitness(input, 0);
    console.log(`   ✅ Witness generated (length: ${witness.length})`);
    console.log(`   Output signal isAdult: ${witness[1]}\n`);

    // 3. Save witness to file
    console.log("3. Saving witness to file...");
    const witnessBin = await witnessCalculator.calculateWTNSBin(input, 0);
    writeFileSync("witness.wtns", witnessBin);
    console.log("   ✅ Saved to witness.wtns\n");

    console.log("=== Ready for proof generation ===");
    console.log("Run: npx snarkjs groth16 prove age_verification_final.zkey witness.wtns proof.json public.json");
    console.log("Then: npx snarkjs groth16 verify verification_key.json public.json proof.json");
}

generateProof().catch(err => {
    console.error("ERROR:", err.message);
    console.error(err.stack);
});
