/**
 * Panic Button - Emergency Credential Wipe
 * 
 * Allows user to instantly wipe all credentials and device secrets.
 * Used when device is stolen or compromised.
 * 
 * CRITICAL: This action is IRREVERSIBLE.
 */

interface PanicButtonAuditLog {
    timestamp: number;
    trigger: "user_initiated" | "remote_wipe" | "suspicious_activity";
    credentialsWiped: number;
    deviceSecretWiped: boolean;
    circuitsWiped: number;
}

export class PanicButton {
    private static readonly CREDENTIALS_STORE = "credentials";
    private static readonly CIRCUITS_STORE = "circuits";
    private static readonly AUDIT_LOG_STORE = "audit_log";

    /**
     * Executes panic button - WIPES ALL DATA.
     * 
     * WARNING: This is IRREVERSIBLE. All credentials will be lost.
     * User must re-authenticate to obtain new credentials.
     */
    static async execute(trigger: "user_initiated" | "remote_wipe" = "user_initiated"): Promise<void> {
        console.warn("🔥 PANIC BUTTON ACTIVATED");
        console.warn(`Trigger: ${trigger}`);

        const db = await this.openDatabase();

        // Count items before wipe (for audit log)
        const credentialCount = await this.countItems(db, this.CREDENTIALS_STORE);
        const circuitCount = await this.countItems(db, this.CIRCUITS_STORE);

        try {
            // 1. Wipe all credentials
            await this.wipeStore(db, this.CREDENTIALS_STORE);
            console.log(`✓ Wiped ${credentialCount} credentials`);

            // 2. Wipe device secret (makes recovery impossible)
            const { DeviceSecretManager } = await import("./DeviceSecretManager");
            await DeviceSecretManager.wipeDeviceSecret();
            console.log("✓ Wiped device secret");

            // 3. Wipe cached circuits
            await this.wipeStore(db, this.CIRCUITS_STORE);
            console.log(`✓ Wiped ${circuitCount} circuits`);

            // 4. Clear session storage
            await this.clearSessionStorage();
            console.log("✓ Cleared session storage");

            // 5. Log panic button activation
            const auditEntry: PanicButtonAuditLog = {
                timestamp: Date.now(),
                trigger: trigger,
                credentialsWiped: credentialCount,
                deviceSecretWiped: true,
                circuitsWiped: circuitCount
            };

            await this.logPanicButtonActivation(db, auditEntry);
            console.log("✓ Logged panic button activation");

            // 6. Show user confirmation
            await this.showUserNotification();

            console.log("🎉 Panic button completed successfully");

        } catch (error) {
            console.error("❌ Panic button failed:", error);

            // Even if panic button fails, try to wipe as much as possible
            // This is a best-effort operation in critical situation
            throw new Error(`Panic button failed: ${error}`);
        }
    }

    /**
     * Checks if panic button should be auto-triggered.
     * (Future enhancement: detect suspicious activity)
     */
    static async checkForAutoTrigger(): Promise<boolean> {
        // TODO: Implement suspicious activity detection
        // Examples:
        // - Too many failed proof attempts
        // - Proof requests from blacklisted origins
        // - Unusual access patterns

        return false; // Not implemented in MVP
    }

    /**
     * Shows confirmation dialog before executing panic button.
     */
    static async confirmAndExecute(): Promise<boolean> {
        const confirmed = confirm(
            "🔥 PANIC BUTTON\n\n" +
            "This will permanently delete ALL credentials and device data.\n\n" +
            "You will need to re-authenticate to obtain new credentials.\n\n" +
            "This action CANNOT be undone.\n\n" +
            "Are you absolutely sure?"
        );

        if (confirmed) {
            await this.execute("user_initiated");
            return true;
        }

        return false;
    }

    private static async wipeStore(db: IDBDatabase, storeName: string): Promise<void> {
        if (!db.objectStoreNames.contains(storeName)) {
            return; // Store doesn't exist, nothing to wipe
        }

        const tx = db.transaction(storeName, "readwrite");
        const store = tx.objectStore(storeName);
        await store.clear();
        await tx.done;
    }

    private static async countItems(db: IDBDatabase, storeName: string): Promise<number> {
        if (!db.objectStoreNames.contains(storeName)) {
            return 0;
        }

        const tx = db.transaction(storeName, "readonly");
        const store = tx.objectStore(storeName);
        const count = await store.count();
        return count;
    }

    private static async clearSessionStorage(): Promise<void> {
        // Clear chrome.storage.session (ephemeral keys)
        if (typeof chrome !== "undefined" && chrome.storage?.session) {
            await chrome.storage.session.clear();
        }

        // Clear sessionStorage (web API)
        if (typeof sessionStorage !== "undefined") {
            sessionStorage.clear();
        }
    }

    private static async logPanicButtonActivation(
        db: IDBDatabase,
        entry: PanicButtonAuditLog
    ): Promise<void> {
        // Create audit log store if it doesn't exist
        if (!db.objectStoreNames.contains(this.AUDIT_LOG_STORE)) {
            // Upgrade needed - skip logging for now
            console.warn("Audit log store not found - skipping log");
            return;
        }

        const tx = db.transaction(this.AUDIT_LOG_STORE, "readwrite");
        const store = tx.objectStore(this.AUDIT_LOG_STORE);

        await store.add({
            id: crypto.randomUUID(),
            type: "PANIC_BUTTON_ACTIVATED",
            ...entry
        });

        await tx.done;
    }

    private static async showUserNotification(): Promise<void> {
        // Chrome extension notification
        if (typeof chrome !== "undefined" && chrome.notifications) {
            await chrome.notifications.create({
                type: "basic",
                iconUrl: "icons/icon128.png",
                title: "🔥 All Credentials Wiped",
                message: "Panic button activated. All credentials and device data have been permanently deleted.",
                priority: 2
            });
        }

        // Fallback: alert
        if (typeof alert !== "undefined") {
            alert("🔥 All credentials wiped successfully.\n\nExtension has been reset.");
        }
    }

    private static async openDatabase(): Promise<IDBDatabase> {
        return new Promise((resolve, reject) => {
            const request = indexedDB.open("ewallet_secure_storage", 1);

            request.onerror = () => reject(request.error);
            request.onsuccess = () => resolve(request.result);

            request.onupgradeneeded = (event) => {
                const db = (event.target as IDBOpenDBRequest).result;

                if (!db.objectStoreNames.contains(this.CREDENTIALS_STORE)) {
                    const credStore = db.createObjectStore(this.CREDENTIALS_STORE, { keyPath: "id" });
                    credStore.createIndex("policyId", "policyId");
                }

                if (!db.objectStoreNames.contains(this.CIRCUITS_STORE)) {
                    db.createObjectStore(this.CIRCUITS_STORE, { keyPath: "circuitId" });
                }

                if (!db.objectStoreNames.contains(this.AUDIT_LOG_STORE)) {
                    const auditStore = db.createObjectStore(this.AUDIT_LOG_STORE, { keyPath: "id" });
                    auditStore.createIndex("timestamp", "timestamp");
                    auditStore.createIndex("type", "type");
                }
            };
        });
    }
}
