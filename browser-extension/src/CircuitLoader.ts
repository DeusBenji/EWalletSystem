/**
 * Circuit Loader
 * 
 * Loads and verifies ZKP circuits (WASM + verification key).
 * CRITICAL: All circuits must have valid signatures before loading.
 */

import { CircuitManifestVerifier } from './CircuitManifestVerifier';

export interface CircuitManifest {
    circuitId: string;
    version: string;
    artifacts: {
        prover: {
            path: string;
            sha256: string;
            size: number;
        };
        verificationKey: {
            path: string;
            sha256: string;
            size: number;
        };
    };
    signature?: string;
    timestamp: number;
}

export interface LoadedCircuit {
    circuitId: string;
    version: string;
    wasm: Uint8Array;
    verificationKey: any;
    manifest: CircuitManifest;
}

export class CircuitLoader {
    private static readonly CIRCUITS_BASE_URL = "/circuits";
    private static readonly MINIMUM_VERSIONS: Record<string, string> = {
        "age_verification_v1": "1.2.0",
        "drivers_license_v1": "1.0.0"
    };

    /**
     * Loads a circuit with full security verification.
     * 
     * SECURITY CHECKS:
     * 1. Version >= minimum (downgrade protection)
     * 2. Manifest signature valid
     * 3. WASM hash matches manifest
     * 4. Verification key hash matches manifest
     */
    static async loadCircuit(circuitId: string, version: string): Promise<LoadedCircuit> {
        console.log(`🔧 Loading circuit: ${circuitId}@${version}`);

        // 1. CHECK: Downgrade protection
        if (!this.satisfiesMinimum(circuitId, version)) {
            const minimum = this.MINIMUM_VERSIONS[circuitId];
            throw new Error(
                `⛔ DOWNGRADE BLOCKED: ${circuitId}@${version} < minimum v${minimum}`
            );
        }

        // 2. FETCH: Manifest
        const manifestUrl = `${this.CIRCUITS_BASE_URL}/${circuitId}/${version}/manifest.json`;
        const manifestResponse = await fetch(manifestUrl);

        if (!manifestResponse.ok) {
            throw new Error(`Failed to fetch manifest: ${manifestResponse.status}`);
        }

        const manifest: CircuitManifest = await manifestResponse.json();

        // 3. VERIFY: Manifest signature
        const isSignatureValid = await CircuitManifestVerifier.verify(manifest);
        if (!isSignatureValid) {
            throw new Error(
                `⛔ SIGNATURE INVALID: Circuit ${circuitId}@${version} manifest signature verification failed`
            );
        }

        console.log(`✓ Manifest signature verified for ${circuitId}@${version}`);

        // 4. FETCH: WASM prover
        const wasmUrl = `${this.CIRCUITS_BASE_URL}/${circuitId}/${version}/${manifest.artifacts.prover.path}`;
        const wasmResponse = await fetch(wasmUrl);

        if (!wasmResponse.ok) {
            throw new Error(`Failed to fetch WASM: ${wasmResponse.status}`);
        }

        const wasmBytes = new Uint8Array(await wasmResponse.arrayBuffer());

        // 5. VERIFY: WASM hash
        const wasmHash = await this.sha256(wasmBytes);
        if (wasmHash !== manifest.artifacts.prover.sha256) {
            throw new Error(
                `⛔ WASM TAMPERED: Hash mismatch for ${circuitId}@${version}\n` +
                `Expected: ${manifest.artifacts.prover.sha256}\n` +
                `Actual: ${wasmHash}`
            );
        }

        console.log(`✓ WASM hash verified for ${circuitId}@${version}`);

        // 6. FETCH: Verification key
        const vkeyUrl = `${this.CIRCUITS_BASE_URL}/${circuitId}/${version}/${manifest.artifacts.verificationKey.path}`;
        const vkeyResponse = await fetch(vkeyUrl);

        if (!vkeyResponse.ok) {
            throw new Error(`Failed to fetch verification key: ${vkeyResponse.status}`);
        }

        const verificationKey = await vkeyResponse.json();

        // 7. VERIFY: Verification key hash
        const vkeyBytes = new TextEncoder().encode(JSON.stringify(verificationKey));
        const vkeyHash = await this.sha256(vkeyBytes);

        if (vkeyHash !== manifest.artifacts.verificationKey.sha256) {
            throw new Error(
                `⛔ VKEY TAMPERED: Hash mismatch for ${circuitId}@${version}`
            );
        }

        console.log(`✓ Verification key hash verified for ${circuitId}@${version}`);

        console.log(`🎉 Circuit ${circuitId}@${version} loaded successfully (ALL CHECKS PASSED)`);

        return {
            circuitId,
            version,
            wasm: wasmBytes,
            verificationKey,
            manifest
        };
    }

    /**
     * Checks if version satisfies minimum requirement (downgrade protection).
     */
    private static satisfiesMinimum(circuitId: string, version: string): boolean {
        const minimum = this.MINIMUM_VERSIONS[circuitId];
        if (!minimum) {
            throw new Error(`Unknown circuit: ${circuitId}`);
        }

        return this.compareVersions(version, minimum) >= 0;
    }

    /**
     * Compares semantic versions.
     * Returns: -1 if v1 < v2, 0 if equal, 1 if v1 > v2
     */
    private static compareVersions(v1: string, v2: string): number {
        const parse = (v: string) => v.split('.').map(Number);
        const parts1 = parse(v1);
        const parts2 = parse(v2);

        for (let i = 0; i < 3; i++) {
            if (parts1[i] !== parts2[i]) {
                return parts1[i] - parts2[i];
            }
        }

        return 0;
    }

    /**
     * Computes SHA-256 hash of data.
     */
    private static async sha256(data: Uint8Array): Promise<string> {
        // Convert Uint8Array to proper ArrayBuffer (not SharedArrayBuffer)
        const buffer = data.buffer.slice(data.byteOffset, data.byteOffset + data.byteLength) as ArrayBuffer;
        const hashBuffer = await crypto.subtle.digest('SHA-256', buffer);
        const hashArray = Array.from(new Uint8Array(hashBuffer));
        return hashArray.map(b => b.toString(16).padStart(2, '0')).join('');
    }
}
