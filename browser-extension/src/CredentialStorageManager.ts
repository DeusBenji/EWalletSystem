/**
 * Credential Storage Manager
 * 
 * Manages encrypted credential storage using chrome.storage.local.
 * Credentials are encrypted with device secret before storage.
 */

import { DeviceSecretManager, EncryptedCredential } from './DeviceSecretManager';

export interface StoredCredential {
    id: string;
    policyId: string;
    policyVersion: string;
    encryptedData: EncryptedCredential;
    issuedAt: number;
    expiresAt: number;
    issuer: string;
    status: 'active' | 'expired' | 'revoked';
}

export interface CredentialFilter {
    policyId?: string;
    status?: 'active' | 'expired' | 'revoked';
}

export class CredentialStorageManager {
    private static readonly STORAGE_KEY_PREFIX = 'credential_';
    private static readonly INDEX_KEY = 'credential_index';

    /**
     * Stores a credential (encrypted with device secret).
     */
    static async storeCredential(
        credential: any,
        policyId: string,
        policyVersion: string,
        issuer: string,
        expiresAt: number
    ): Promise<string> {
        console.log(`💾 Storing credential for policy: ${policyId}`);

        // 1. Get device secret
        const deviceSecret = await DeviceSecretManager.getOrGenerateDeviceSecret();

        // 2. Encrypt credential
        const encryptedData = await DeviceSecretManager.encryptCredential(credential, deviceSecret);

        // 3. Create stored credential
        const storedCredential: StoredCredential = {
            id: encryptedData.id,
            policyId,
            policyVersion,
            encryptedData,
            issuedAt: Date.now(),
            expiresAt,
            issuer,
            status: 'active'
        };

        // 4. Save to chrome.storage.local
        await this.saveToStorage(storedCredential);

        // 5. Update index
        await this.updateIndex(storedCredential.id, policyId);

        console.log(`✓ Credential stored: ${storedCredential.id}`);
        return storedCredential.id;
    }

    /**
     * Retrieves a credential by ID (decrypted).
     */
    static async getCredential(credentialId: string): Promise<any> {
        console.log(`🔍 Retrieving credential: ${credentialId}`);

        // 1. Load from storage
        const stored = await this.loadFromStorage(credentialId);
        if (!stored) {
            throw new Error(`Credential not found: ${credentialId}`);
        }

        // 2. Check status
        if (stored.status !== 'active') {
            throw new Error(`Credential is ${stored.status}: ${credentialId}`);
        }

        // 3. Check expiration
        if (Date.now() > stored.expiresAt) {
            await this.markExpired(credentialId);
            throw new Error(`Credential expired: ${credentialId}`);
        }

        // 4. Decrypt
        const deviceSecret = await DeviceSecretManager.getOrGenerateDeviceSecret();
        const decrypted = await DeviceSecretManager.decryptCredential(
            stored.encryptedData,
            deviceSecret
        );

        console.log(`✓ Credential retrieved: ${credentialId}`);
        return decrypted;
    }

    /**
     * Lists all credentials matching filter.
     */
    static async listCredentials(filter?: CredentialFilter): Promise<StoredCredential[]> {
        const index = await this.loadIndex();
        const credentials: StoredCredential[] = [];

        for (const credentialId of index.credentialIds) {
            const stored = await this.loadFromStorage(credentialId);
            if (!stored) continue;

            // Apply filters
            if (filter?.policyId && stored.policyId !== filter.policyId) {
                continue;
            }
            if (filter?.status && stored.status !== filter.status) {
                continue;
            }

            credentials.push(stored);
        }

        return credentials;
    }

    /**
     * Deletes a credential by ID.
     */
    static async deleteCredential(credentialId: string): Promise<void> {
        console.log(`🗑️ Deleting credential: ${credentialId}`);

        // 1. Remove from storage
        const chromeGlobal = (globalThis as any).chrome;
        if (chromeGlobal?.storage?.local) {
            await chromeGlobal.storage.local.remove(this.STORAGE_KEY_PREFIX + credentialId);
        }

        // 2. Update index
        await this.removeFromIndex(credentialId);

        console.log(`✓ Credential deleted: ${credentialId}`);
    }

    /**
     * Marks a credential as expired.
     */
    private static async markExpired(credentialId: string): Promise<void> {
        const stored = await this.loadFromStorage(credentialId);
        if (stored) {
            stored.status = 'expired';
            await this.saveToStorage(stored);
        }
    }

    /**
     * Saves credential to chrome.storage.local.
     */
    private static async saveToStorage(credential: StoredCredential): Promise<void> {
        const chromeGlobal = (globalThis as any).chrome;
        if (!chromeGlobal?.storage?.local) {
            throw new Error('chrome.storage.local not available');
        }

        const key = this.STORAGE_KEY_PREFIX + credential.id;
        await chromeGlobal.storage.local.set({ [key]: credential });
    }

    /**
     * Loads credential from chrome.storage.local.
     */
    private static async loadFromStorage(credentialId: string): Promise<StoredCredential | null> {
        const chromeGlobal = (globalThis as any).chrome;
        if (!chromeGlobal?.storage?.local) {
            throw new Error('chrome.storage.local not available');
        }

        const key = this.STORAGE_KEY_PREFIX + credentialId;
        const result = await chromeGlobal.storage.local.get(key);
        return result[key] || null;
    }

    /**
     * Updates the credential index.
     */
    private static async updateIndex(credentialId: string, policyId: string): Promise<void> {
        const index = await this.loadIndex();

        if (!index.credentialIds.includes(credentialId)) {
            index.credentialIds.push(credentialId);
        }

        if (!index.byPolicy[policyId]) {
            index.byPolicy[policyId] = [];
        }
        if (!index.byPolicy[policyId].includes(credentialId)) {
            index.byPolicy[policyId].push(credentialId);
        }

        await this.saveIndex(index);
    }

    /**
     * Removes credential from index.
     */
    private static async removeFromIndex(credentialId: string): Promise<void> {
        const index = await this.loadIndex();

        index.credentialIds = index.credentialIds.filter(id => id !== credentialId);

        for (const policyId in index.byPolicy) {
            index.byPolicy[policyId] = index.byPolicy[policyId].filter(id => id !== credentialId);
        }

        await this.saveIndex(index);
    }

    /**
     * Loads credential index.
     */
    private static async loadIndex(): Promise<CredentialIndex> {
        const chromeGlobal = (globalThis as any).chrome;
        if (!chromeGlobal?.storage?.local) {
            return { credentialIds: [], byPolicy: {} };
        }

        const result = await chromeGlobal.storage.local.get(this.INDEX_KEY);
        return result[this.INDEX_KEY] || { credentialIds: [], byPolicy: {} };
    }

    /**
     * Saves credential index.
     */
    private static async saveIndex(index: CredentialIndex): Promise<void> {
        const chromeGlobal = (globalThis as any).chrome;
        if (!chromeGlobal?.storage?.local) {
            throw new Error('chrome.storage.local not available');
        }

        await chromeGlobal.storage.local.set({ [this.INDEX_KEY]: index });
    }
}

interface CredentialIndex {
    credentialIds: string[];
    byPolicy: Record<string, string[]>;
}
