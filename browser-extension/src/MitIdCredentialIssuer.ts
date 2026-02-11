/**
 * MitID Credential Issuer
 * 
 * Handles credential issuance flow with MitID authentication.
 * Integrates with IdentityService and TokenService.
 */

import { CredentialStorageManager } from './CredentialStorageManager';

export interface MitIdAuthResult {
    accessToken: string;
    idToken: string;
    expiresIn: number;
}

export interface IssuedCredential {
    credentialId: string;
    policyId: string;
    policyVersion: string;
    credential: any;
    jwt: string;
    expiresAt: number;
}

export class MitIdCredentialIssuer {
    private static readonly IDENTITY_SERVICE_URL = process.env.IDENTITY_SERVICE_URL || 'https://localhost:5001';
    private static readonly TOKEN_SERVICE_URL = process.env.TOKEN_SERVICE_URL || 'https://localhost:5002';

    /**
     * Initiates MitID authentication flow.
     * Opens popup window for MitID login.
     */
    static async initiateMitIdAuth(): Promise<MitIdAuthResult> {
        console.log('🔐 Initiating MitID authentication');

        // 1. Get authorization URL from IdentityService
        const authUrl = await this.getAuthorizationUrl();

        // 2. Open popup for MitID login
        const result = await this.openAuthPopup(authUrl);

        console.log('✓ MitID authentication successful');
        return result;
    }

    /**
     * Issues a credential for a specific policy.
     * Requires valid MitID authentication.
     */
    static async issueCredential(
        authResult: MitIdAuthResult,
        policyId: string
    ): Promise<string> {
        console.log(`📜 Issuing credential for policy: ${policyId}`);

        // 1. Request credential from TokenService
        const issued = await this.requestCredential(authResult.accessToken, policyId);

        // 2. Store credential (encrypted)
        const credentialId = await CredentialStorageManager.storeCredential(
            issued.credential,
            issued.policyId,
            issued.policyVersion,
            'TokenService',
            issued.expiresAt
        );

        console.log(`✓ Credential issued and stored: ${credentialId}`);
        return credentialId;
    }

    /**
     * Gets authorization URL from IdentityService.
     */
    private static async getAuthorizationUrl(): Promise<string> {
        const response = await fetch(`${this.IDENTITY_SERVICE_URL}/api/auth/authorize`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({
                clientId: 'zkp-wallet-extension',
                redirectUri: chrome.identity.getRedirectURL(),
                scope: 'openid profile',
                responseType: 'code'
            })
        });

        if (!response.ok) {
            throw new Error(`Failed to get authorization URL: ${response.status}`);
        }

        const data = await response.json();
        return data.authorizationUrl;
    }

    /**
     * Opens authentication popup and waits for result.
     */
    private static async openAuthPopup(authUrl: string): Promise<MitIdAuthResult> {
        return new Promise((resolve, reject) => {
            const chromeGlobal = (globalThis as any).chrome;

            if (!chromeGlobal?.identity?.launchWebAuthFlow) {
                reject(new Error('chrome.identity API not available'));
                return;
            }

            chromeGlobal.identity.launchWebAuthFlow(
                {
                    url: authUrl,
                    interactive: true
                },
                async (responseUrl: string) => {
                    if (chromeGlobal.runtime.lastError) {
                        reject(new Error(chromeGlobal.runtime.lastError.message));
                        return;
                    }

                    try {
                        // Parse authorization code from response URL
                        const url = new URL(responseUrl);
                        const code = url.searchParams.get('code');

                        if (!code) {
                            reject(new Error('No authorization code received'));
                            return;
                        }

                        // Exchange code for tokens
                        const tokens = await this.exchangeCodeForTokens(code);
                        resolve(tokens);

                    } catch (error) {
                        reject(error);
                    }
                }
            );
        });
    }

    /**
     * Exchanges authorization code for access/ID tokens.
     */
    private static async exchangeCodeForTokens(code: string): Promise<MitIdAuthResult> {
        const response = await fetch(`${this.IDENTITY_SERVICE_URL}/api/auth/token`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({
                code: code,
                redirectUri: chrome.identity.getRedirectURL(),
                clientId: 'zkp-wallet-extension'
            })
        });

        if (!response.ok) {
            throw new Error(`Token exchange failed: ${response.status}`);
        }

        const data = await response.json();
        return {
            accessToken: data.access_token,
            idToken: data.id_token,
            expiresIn: data.expires_in
        };
    }

    /**
     * Requests credential from TokenService.
     */
    private static async requestCredential(
        accessToken: string,
        policyId: string
    ): Promise<IssuedCredential> {
        const response = await fetch(`${this.TOKEN_SERVICE_URL}/api/credentials/issue`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'Authorization': `Bearer ${accessToken}`
            },
            body: JSON.stringify({
                policyId: policyId
            })
        });

        if (!response.ok) {
            throw new Error(`Credential issuance failed: ${response.status}`);
        }

        const data = await response.json();
        return {
            credentialId: data.credentialId,
            policyId: data.policyId,
            policyVersion: data.policyVersion,
            credential: data.credential,
            jwt: data.jwt,
            expiresAt: data.expiresAt
        };
    }
}
