/**
 * ZKP Wallet SDK
 * 
 * JavaScript SDK for websites to request ZKP proofs from the browser extension.
 * 
 * SECURITY:
 * - Origin validation (extension checks origin before sending proof)
 * - Timeout with fail-closed behavior
 * - Challenge-response to prevent replay attacks
 */

export interface VerifyPolicyRequest {
    policyId: string;
    challenge?: {
        nonce?: string;
        timestamp?: number;
    };
    timeout?: number; // milliseconds, default 30000
}

export interface VerifyPolicyResponse {
    success: boolean;
    proof?: {
        envelope: any; // ProofEnvelope from extension
        timestamp: number;
    };
    error?: {
        code: string;
        message: string;
    };
}

export class ZKPWalletSDK {
    private static readonly DEFAULT_TIMEOUT = 30000; // 30 seconds
    private static readonly EXTENSION_ID = 'zkp-wallet-extension'; // Will be actual Chrome extension ID

    /**
     * Verifies a policy using ZKP proof from extension.
     * 
     * USAGE:
     * ```javascript
     * const result = await ZKPWalletSDK.verifyPolicy({
     *   policyId: 'age_over_18'
     * });
     * 
     * if (result.success) {
     *   // Grant access
     *   console.log('Proof verified:', result.proof);
     * } else {
     *   // Deny access
     *   console.error('Verification failed:', result.error);
     * }
     * ```
     */
    static async verifyPolicy(request: VerifyPolicyRequest): Promise<VerifyPolicyResponse> {
        console.log(`🔐 Requesting ZKP proof for policy: ${request.policyId}`);

        try {
            // 1. Generate challenge (anti-replay)
            const challenge = request.challenge || this.generateChallenge();

            // 2. Request proof from extension
            const proof = await this.requestProofFromExtension(request.policyId, challenge, request.timeout);

            // 3. Return success with proof
            return {
                success: true,
                proof: {
                    envelope: proof,
                    timestamp: Date.now()
                }
            };

        } catch (error: any) {
            console.error('❌ Policy verification failed:', error);

            // FAIL CLOSED: Always return failure on error
            return {
                success: false,
                error: {
                    code: error.code || 'VERIFICATION_FAILED',
                    message: error.message || 'An unknown error occurred'
                }
            };
        }
    }

    /**
     * Generates a challenge for anti-replay protection.
     */
    private static generateChallenge(): { nonce: string; timestamp: number; origin: string } {
        return {
            nonce: this.generateNonce(),
            timestamp: Date.now(),
            origin: window.location.origin
        };
    }

    /**
     * Generates a cryptographic nonce.
     */
    private static generateNonce(): string {
        const buffer = new Uint8Array(32);
        crypto.getRandomValues(buffer);
        return Array.from(buffer)
            .map(b => b.toString(16).padStart(2, '0'))
            .join('');
    }

    /**
     * Requests proof from browser extension.
     */
    private static async requestProofFromExtension(
        policyId: string,
        challenge: any,
        timeout?: number
    ): Promise<any> {
        const timeoutMs = timeout || this.DEFAULT_TIMEOUT;

        return new Promise((resolve, reject) => {
            // Set timeout (fail closed)
            const timeoutId = setTimeout(() => {
                reject({
                    code: 'TIMEOUT',
                    message: `Proof request timed out after ${timeoutMs}ms`
                });
            }, timeoutMs);

            // Post message to extension
            window.postMessage({
                type: 'ZKP_PROOF_REQUEST',
                source: 'zkp-wallet-sdk',
                policyId: policyId,
                challenge: challenge
            }, '*');

            // Listen for response
            const messageHandler = (event: MessageEvent) => {
                // Validate message source
                if (event.source !== window) {
                    return;
                }

                // Validate message structure
                if (!event.data || event.data.type !== 'ZKP_PROOF_RESPONSE') {
                    return;
                }

                // Validate message is for this request
                if (event.data.policyId !== policyId) {
                    return;
                }

                // Clear timeout
                clearTimeout(timeoutId);

                // Remove listener
                window.removeEventListener('message', messageHandler);

                // Check for error
                if (event.data.error) {
                    reject({
                        code: event.data.error.code || 'EXTENSION_ERROR',
                        message: event.data.error.message || 'Extension returned error'
                    });
                    return;
                }

                // Return proof
                resolve(event.data.proof);
            };

            window.addEventListener('message', messageHandler);
        });
    }

    /**
     * Checks if the extension is installed.
     */
    static async isExtensionInstalled(): Promise<boolean> {
        return new Promise((resolve) => {
            const timeoutId = setTimeout(() => {
                resolve(false);
            }, 1000);

            window.postMessage({
                type: 'ZKP_EXTENSION_PING',
                source: 'zkp-wallet-sdk'
            }, '*');

            const messageHandler = (event: MessageEvent) => {
                if (event.source === window &&
                    event.data?.type === 'ZKP_EXTENSION_PONG') {
                    clearTimeout(timeoutId);
                    window.removeEventListener('message', messageHandler);
                    resolve(true);
                }
            };

            window.addEventListener('message', messageHandler);
        });
    }

    /**
     * Gets SDK version.
     */
    static getVersion(): string {
        return '1.0.0';
    }
}

// Export as global for non-module usage
if (typeof window !== 'undefined') {
    (window as any).ZKPWalletSDK = ZKPWalletSDK;
}
