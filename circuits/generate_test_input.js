const { buildPoseidon } = require("circomlibjs");

async function main() {
    const poseidon = await buildPoseidon();
    const F = poseidon.F;

    // Test inputs
    const birthYear = 1995;
    const salt = BigInt("12345678901234567890123456789012");
    const currentYear = 2024;
    const challenge = BigInt("99999");

    // Calculate commitment = Poseidon(birthYear, salt)
    const commitment = poseidon([birthYear, salt]);
    const commitmentStr = F.toString(commitment);

    // Calculate challengeHash = Poseidon(challenge)
    const challengeHash = poseidon([challenge]);
    const challengeHashStr = F.toString(challengeHash);

    // Create input JSON
    const input = {
        birthYear: birthYear.toString(),
        salt: salt.toString(),
        commitment: commitmentStr,
        currentYear: currentYear.toString(),
        challenge: challenge.toString(),
        challengeHash: challengeHashStr
    };

    console.log("Generated input:");
    console.log(JSON.stringify(input, null, 2));

    // Test witness generation
    console.log("\nTesting witness generation...");
    const wc = require("./age_verification_js/witness_calculator.js");
    const { readFileSync } = require("fs");

    const buffer = readFileSync("age_verification_js/age_verification.wasm");
    const witnessCalculator = await wc(buffer);

    try {
        const w = await witnessCalculator.calculateWitness(input, 0);
        console.log("✅ SUCCESS! Witness generated, length:", w.length);
        console.log("Output signal (isAdult):", F.toString(w[1]));

        // Save input to file
        const fs = require("fs");
        fs.writeFileSync("input_working.json", JSON.stringify(input, null, 2));
        console.log("\n✅ Saved working input to input_working.json");

    } catch (error) {
        console.error("❌ ERROR:", error.message);
    }
}

main();
