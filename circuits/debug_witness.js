const wc = require("./age_verification_js/witness_calculator.js");
const { readFileSync } = require("fs");

async function test() {
    try {
        console.log("Loading WASM...");
        const buffer = readFileSync("age_verification_js/age_verification.wasm");

        console.log("Creating witness calculator...");
        const witnessCalculator = await wc(buffer);

        console.log("Preparing input...");
        const input = JSON.parse(readFileSync("input.json", "utf8"));
        console.log("Input:", JSON.stringify(input, null, 2));

        console.log("Calculating witness...");
        const w = await witnessCalculator.calculateWitness(input, 0);

        console.log("Success! Witness length:", w.length);
        console.log("First few witness values:", w.slice(0, 5));

    } catch (error) {
        console.error("ERROR:", error.message);
        console.error("Stack:", error.stack);
    }
}

test();
