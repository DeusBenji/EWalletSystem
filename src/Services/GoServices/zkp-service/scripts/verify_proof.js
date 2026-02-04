const snarkjs = require("snarkjs");
const fs = require("fs");

/**
 * Verify a Groth16 proof using snarkjs
 * Usage: node verify_proof.js <proof_json> <public_signals_json> <vkey_path>
 */
async function verify() {
    try {
        // Parse command line arguments
        if (process.argv.length !== 5) {
            console.error("Usage: node verify_proof.js <proof_json> <public_signals_json> <vkey_path>");
            process.exit(1);
        }

        const proofJSON = process.argv[2];
        const publicSignalsJSON = process.argv[3];
        const vkeyPath = process.argv[4];

        // Parse inputs
        const proof = JSON.parse(proofJSON);
        const publicSignals = JSON.parse(publicSignalsJSON);
        const vkey = JSON.parse(fs.readFileSync(vkeyPath, "utf8"));

        // Verify proof
        const res = await snarkjs.groth16.verify(vkey, publicSignals, proof);

        // Output result
        if (res) {
            console.log("OK");
            process.exit(0);
        } else {
            console.log("INVALID");
            process.exit(1);
        }
    } catch (error) {
        console.error("ERROR:", error.message);
        process.exit(2);
    }
}

verify();
