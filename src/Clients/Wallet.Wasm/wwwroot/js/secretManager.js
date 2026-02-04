// secretManager.js - Secure wallet secret storage using IndexedDB
// CRITICAL: Never use localStorage for secrets - it's not secure and persists across sessions

const DB_NAME = 'WalletSecretDB';
const DB_VERSION = 1;
const STORE_NAME = 'secrets';
const SECRET_KEY = 'walletSecret';

/**
 * Initialize IndexedDB database
 */
function initDB() {
    return new Promise((resolve, reject) => {
        const request = indexedDB.open(DB_NAME, DB_VERSION);

        request.onerror = () => reject(request.error);
        request.onsuccess = () => resolve(request.result);

        request.onupgradeneeded = (event) => {
            const db = event.target.result;
            if (!db.objectStoreNames.contains(STORE_NAME)) {
                db.createObjectStore(STORE_NAME);
            }
        };
    });
}

/**
 * Generate a cryptographically secure 256-bit random secret
 * @returns {string} Hex-encoded secret (64 characters)
 */
function generateSecret() {
    const array = new Uint8Array(32); // 256 bits = 32 bytes
    crypto.getRandomValues(array);
    return Array.from(array, byte => byte.toString(16).padStart(2, '0')).join('');
}

/**
 * Get secret from IndexedDB
 * @returns {Promise<string|null>} The secret if it exists, null otherwise
 */
async function getSecret() {
    try {
        const db = await initDB();
        return new Promise((resolve, reject) => {
            const transaction = db.transaction([STORE_NAME], 'readonly');
            const store = transaction.objectStore(STORE_NAME);
            const request = store.get(SECRET_KEY);

            request.onsuccess = () => resolve(request.result || null);
            request.onerror = () => reject(request.error);
        });
    } catch (error) {
        console.error('[SecretManager] Error getting secret:', error);
        return null;
    }
}

/**
 * Save secret to IndexedDB
 * @param {string} secret - The secret to save
 * @returns {Promise<void>}
 */
async function setSecret(secret) {
    try {
        const db = await initDB();
        return new Promise((resolve, reject) => {
            const transaction = db.transaction([STORE_NAME], 'readwrite');
            const store = transaction.objectStore(STORE_NAME);
            const request = store.put(secret, SECRET_KEY);

            request.onsuccess = () => resolve();
            request.onerror = () => reject(request.error);
        });
    } catch (error) {
        console.error('[SecretManager] Error setting secret:', error);
        throw error;
    }
}

/**
 * Get or create wallet secret
 * If secret doesn't exist, generates a new one and stores it
 * @returns {Promise<string>} The wallet secret (hex-encoded)
 */
async function getOrCreateSecret() {
    try {
        let secret = await getSecret();

        if (!secret) {
            console.log('[SecretManager] No secret found, generating new one');
            secret = generateSecret();
            await setSecret(secret);
            console.log('[SecretManager] New secret generated and stored');
        } else {
            console.log('[SecretManager] Existing secret retrieved');
        }

        return secret;
    } catch (error) {
        console.error('[SecretManager] Error in getOrCreateSecret:', error);
        throw error;
    }
}

/**
 * Delete secret from IndexedDB (for testing/reset purposes)
 * @returns {Promise<void>}
 */
async function deleteSecret() {
    try {
        const db = await initDB();
        return new Promise((resolve, reject) => {
            const transaction = db.transaction([STORE_NAME], 'readwrite');
            const store = transaction.objectStore(STORE_NAME);
            const request = store.delete(SECRET_KEY);

            request.onsuccess = () => resolve();
            request.onerror = () => reject(request.error);
        });
    } catch (error) {
        console.error('[SecretManager] Error deleting secret:', error);
        throw error;
    }
}

// Export to global window object for C# JSInterop
window.secretManager = {
    getOrCreateSecret,
    getSecret,
    setSecret,
    deleteSecret
};

console.log('[SecretManager] Module loaded - using IndexedDB for secure storage');
