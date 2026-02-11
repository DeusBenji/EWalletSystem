/**
 * Circuit Manifest Verifier (Browser Version)
 * 
 * Verifies circuit manifest signatures using embedded public key.
 * This is the TypeScript version for browser extension.
 */

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

export class CircuitManifestVerifier {
    // Embedded public key for circuit signature verification
    // This is the same key from CircuitSigningPublicKey.cs
    private static readonly PUBLIC_KEY_PEM = `-----BEGIN PUBLIC KEY-----
MFkwEwYHKoZIzj0CAQYIKoZIzj0DAQcDQgAEabc123def456...PLACEHOLDER...
-----END PUBLIC KEY-----`;

    /**
     * Verifies manifest signature.
     * 
     * CRITICAL: Returns false if signature is missing or invalid.
     */
    static async verify(manifest: CircuitManifest): Promise<boolean> {
        // 1. Check signature exists
        if (!manifest.signature || manifest.signature.length === 0) {
            console.error("❌ Manifest signature missing");
            return false;
        }

        try {
            // 2. Create canonical JSON (without signature)
            const canonicalJson = this.createCanonicalJson(manifest);

            // 3. Decode signature from base64
            const signatureBytes = this.base64ToArrayBuffer(manifest.signature);

            // 4. Import public key
            const publicKey = await this.importPublicKey();

            // 5. Verify signature
            const isValid = await crypto.subtle.verify(
                {
                    name: "ECDSA",
                    hash: { name: "SHA-256" }
                },
                publicKey,
                signatureBytes,
                new TextEncoder().encode(canonicalJson)
            );

            if (!isValid) {
                console.error("❌ Manifest signature verification failed");
            }

            return isValid;

        } catch (error) {
            console.error("❌ Signature verification error:", error);
            return false;
        }
    }

    /**
     * Creates canonical JSON (sorted keys, no signature).
     */
    private static createCanonicalJson(manifest: CircuitManifest): string {
        const { signature, ...manifestWithoutSignature } = manifest;

        // Sort keys recursively and stringify without whitespace
        const sortedManifest = this.sortKeysRecursive(manifestWithoutSignature);
        return JSON.stringify(sortedManifest);
    }

    /**
     * Recursively sorts object keys alphabetically.
     */
    private static sortKeysRecursive(obj: any): any {
        if (Array.isArray(obj)) {
            return obj.map(item => this.sortKeysRecursive(item));
        }

        if (obj !== null && typeof obj === 'object') {
            const sorted: any = {};
            Object.keys(obj).sort().forEach(key => {
                sorted[key] = this.sortKeysRecursive(obj[key]);
            });
            return sorted;
        }

        return obj;
    }

    /**
     * Imports the embedded public key for verification.
     */
    private static async importPublicKey(): Promise<CryptoKey> {
        // Remove PEM headers/footers
        const pemContents = this.PUBLIC_KEY_PEM
            .replace(/-----BEGIN PUBLIC KEY-----/, '')
            .replace(/-----END PUBLIC KEY-----/, '')
            .replace(/\s/g, '');

        // Decode base64 to ArrayBuffer
        const binaryDer = atob(pemContents);
        const bytes = new Uint8Array(binaryDer.length);
        for (let i = 0; i < binaryDer.length; i++) {
            bytes[i] = binaryDer.charCodeAt(i);
        }

        // Import as CryptoKey
        return await crypto.subtle.importKey(
            "spki",
            bytes.buffer,
            {
                name: "ECDSA",
                namedCurve: "P-256"
            },
            false,
            ["verify"]
        );
    }

    /**
     * Converts base64 string to ArrayBuffer.
     */
    private static base64ToArrayBuffer(base64: string): ArrayBuffer {
        const binaryString = atob(base64);
        const bytes = new Uint8Array(binaryString.length);
        for (let i = 0; i < binaryString.length; i++) {
            bytes[i] = binaryString.charCodeAt(i);
        }
        return bytes.buffer;
    }
}
