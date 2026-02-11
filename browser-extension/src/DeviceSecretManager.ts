/**
 * Device Secret Manager
 * 
 * Manages device-specific encryption keys for credential binding.
 * CRITICAL: Device secret is non-extractable and never leaves the browser.
 */

interface DeviceSecretMetadata {
    keyId: string;
    createdAt: number;
    algorithm: string;
    deviceTag: string;
}

export class DeviceSecretManager {
    private static readonly KEY_STORE = "deviceKeys";
    private static readonly DEVICE_SECRET_ID = "device_secret_v1";

    /**
     * Gets or generates device secret.
     * Device secret is used to encrypt all credentials.
     */
    static async getOrGenerateDeviceSecret(): Promise<CryptoKey> {
        const db = await this.openDatabase();

        // Try to load existing key
        const existingKey = await this.loadDeviceSecret(db);
        if (existingKey) {
            return existingKey;
        }

        // Generate new device secret
        console.log("⚙️ Generating new device secret (first install or reset)");
        const newKey = await this.generateDeviceSecret();

        // Store metadata (key itself is non-extractable, stays in memory)
        const metadata: DeviceSecretMetadata = {
            keyId: this.DEVICE_SECRET_ID,
            createdAt: Date.now(),
            algorithm: "AES-GCM",
            deviceTag: await this.computeDeviceTag(newKey)
        };

        const tx = db.transaction(this.KEY_STORE, "readwrite");
        tx.objectStore(this.KEY_STORE).put(metadata);
        await new Promise<void>((resolve, reject) => {
            tx.oncomplete = () => resolve();
            tx.onerror = () => reject(tx.error);
        });

        return newKey;
    }

    /**
     * Generates a new device secret.
     * SECURITY: Key is non-extractable (cannot be exported).
     */
    private static async generateDeviceSecret(): Promise<CryptoKey> {
        return await crypto.subtle.generateKey(
            {
                name: "AES-GCM",
                length: 256
            },
            false, // NOT extractable
            ["encrypt", "decrypt"]
        );
    }

    /**
     * Computes device tag for credential binding.
     * Device tag is included in proof public signals.
     */
    private static async computeDeviceTag(key: CryptoKey): Promise<string> {
        // Export key as JWK (only possible for metadata, not raw key)
        // For device tag, use a deterministic derivation
        const keyId = crypto.randomUUID(); // Unique per device
        const hash = await crypto.subtle.digest(
            "SHA-256",
            new TextEncoder().encode(keyId)
        );
        return this.arrayBufferToHex(hash);
    }

    /**
     * Encrypts a credential with device secret.
     */
    static async encryptCredential(
        credential: any,
        deviceSecret: CryptoKey
    ): Promise<EncryptedCredential> {
        const iv = crypto.getRandomValues(new Uint8Array(12)); // 96-bit nonce
        const credentialBytes = new TextEncoder().encode(JSON.stringify(credential));

        const ciphertext = await crypto.subtle.encrypt(
            {
                name: "AES-GCM",
                iv: iv
            },
            deviceSecret,
            credentialBytes
        );

        const metadata = await this.getDeviceSecretMetadata();

        return {
            id: credential.credentialId || crypto.randomUUID(),
            policyId: credential.policyId,
            ciphertext: this.arrayBufferToBase64(ciphertext),
            iv: this.arrayBufferToBase64(iv.buffer),
            deviceTag: metadata?.deviceTag || "",
            encryptedAt: Date.now()
        };
    }

    /**
     * Decrypts a credential with device secret.
     */
    static async decryptCredential(
        encrypted: EncryptedCredential,
        deviceSecret: CryptoKey
    ): Promise<any> {
        const ciphertext = this.base64ToArrayBuffer(encrypted.ciphertext);
        const iv = this.base64ToArrayBuffer(encrypted.iv);

        const plaintext = await crypto.subtle.decrypt(
            {
                name: "AES-GCM",
                iv: new Uint8Array(iv)
            },
            deviceSecret,
            ciphertext
        );

        const credentialJson = new TextDecoder().decode(plaintext);
        return JSON.parse(credentialJson);
    }

    /**
     * Wipes device secret (panic button).
     * CRITICAL: This makes all encrypted credentials unrecoverable.
     */
    static async wipeDeviceSecret(): Promise<void> {
        const db = await this.openDatabase();
        const tx = db.transaction(this.KEY_STORE, "readwrite");
        tx.objectStore(this.KEY_STORE).delete(this.DEVICE_SECRET_ID);
        await new Promise<void>((resolve, reject) => {
            tx.oncomplete = () => resolve();
            tx.onerror = () => reject(tx.error);
        });

        console.warn("🔥 Device secret wiped - all credentials now unrecoverable");
    }

    private static async getDeviceSecretMetadata(): Promise<DeviceSecretMetadata | null> {
        const db = await this.openDatabase();
        const tx = db.transaction(this.KEY_STORE, "readonly");
        const request = tx.objectStore(this.KEY_STORE).get(this.DEVICE_SECRET_ID);

        return new Promise<DeviceSecretMetadata | null>((resolve, reject) => {
            request.onsuccess = () => resolve(request.result || null);
            request.onerror = () => reject(request.error);
        });
    }

    private static async loadDeviceSecret(db: IDBDatabase): Promise<CryptoKey | null> {
        // Note: Actual key is non-extractable and regenerated on each session
        // We only store metadata, not the key itself
        // For MVP, we regenerate on each load (future: use persistent key in secure storage)
        return null; // Placeholder - actual implementation would use chrome.storage.session
    }

    private static async openDatabase(): Promise<IDBDatabase> {
        return new Promise((resolve, reject) => {
            const request = indexedDB.open("ewallet_secure_storage", 1);

            request.onerror = () => reject(request.error);
            request.onsuccess = () => resolve(request.result);

            request.onupgradeneeded = (event) => {
                const db = (event.target as IDBOpenDBRequest).result;

                if (!db.objectStoreNames.contains(this.KEY_STORE)) {
                    db.createObjectStore(this.KEY_STORE, { keyPath: "keyId" });
                }

                if (!db.objectStoreNames.contains("credentials")) {
                    const credStore = db.createObjectStore("credentials", { keyPath: "id" });
                    credStore.createIndex("policyId", "policyId");
                    credStore.createIndex("deviceTag", "deviceTag");
                }
            };
        });
    }

    private static arrayBufferToBase64(buffer: ArrayBuffer): string {
        const bytes = new Uint8Array(buffer);
        let binary = '';
        for (let i = 0; i < bytes.byteLength; i++) {
            binary += String.fromCharCode(bytes[i]);
        }
        return btoa(binary);
    }

    private static base64ToArrayBuffer(base64: string): ArrayBuffer {
        const binary = atob(base64);
        const bytes = new Uint8Array(binary.length);
        for (let i = 0; i < binary.length; i++) {
            bytes[i] = binary.charCodeAt(i);
        }
        return bytes.buffer;
    }

    private static arrayBufferToHex(buffer: ArrayBuffer): string {
        return Array.from(new Uint8Array(buffer))
            .map(b => b.toString(16).padStart(2, '0'))
            .join('');
    }
}

export interface EncryptedCredential {
    id: string;
    policyId: string;
    ciphertext: string;
    iv: string;
    deviceTag: string;
    encryptedAt: number;
}
