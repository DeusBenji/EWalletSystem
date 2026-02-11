/**
 * Credential Refresh Manager
 * 
 * Monitors credential expiry and handles automatic renewal.
 * Uses chrome.alarms for daily checks.
 */

import { CredentialStorageManager, StoredCredential } from './CredentialStorageManager';
import { MitIdCredentialIssuer } from './MitIdCredentialIssuer';

export class CredentialRefreshManager {
    private static readonly ALARM_NAME = 'credential_expiry_check';
    private static readonly CHECK_INTERVAL_MINUTES = 24 * 60; // Daily
    private static readonly EXPIRY_WARNING_DAYS = 2;

    /**
     * Initializes the credential refresh manager.
     * Sets up daily alarm for expiry checks.
     */
    static async initialize(): Promise<void> {
        console.log('⏰ Initializing credential refresh manager');

        // Create daily alarm
        await this.createDailyAlarm();

        // Register alarm listener
        this.registerAlarmListener();

        console.log('✓ Credential refresh manager initialized');
    }

    /**
     * Creates daily alarm for credential expiry checks.
     */
    private static async createDailyAlarm(): Promise<void> {
        const chromeGlobal = (globalThis as any).chrome;
        if (!chromeGlobal?.alarms) {
            console.warn('chrome.alarms not available');
            return;
        }

        // Clear existing alarm
        await chromeGlobal.alarms.clear(this.ALARM_NAME);

        // Create new alarm (daily, starting in 1 minute)
        await chromeGlobal.alarms.create(this.ALARM_NAME, {
            delayInMinutes: 1,
            periodInMinutes: this.CHECK_INTERVAL_MINUTES
        });

        console.log('✓ Daily expiry check alarm created');
    }

    /**
     * Registers listener for alarm events.
     */
    private static registerAlarmListener(): void {
        const chromeGlobal = (globalThis as any).chrome;
        if (!chromeGlobal?.alarms) {
            return;
        }

        chromeGlobal.alarms.onAlarm.addListener((alarm: any) => {
            if (alarm.name === this.ALARM_NAME) {
                console.log('⏰ Daily expiry check triggered');
                this.performExpiryCheck().catch(error => {
                    console.error('❌ Expiry check failed:', error);
                });
            }
        });
    }

    /**
     * Performs expiry check for all credentials.
     */
    static async performExpiryCheck(): Promise<void> {
        console.log('🔍 Checking credential expiry');

        // Get all active credentials
        const credentials = await CredentialStorageManager.listCredentials({
            status: 'active'
        });

        const now = Date.now();
        const warnings: StoredCredential[] = [];
        let expiredCount = 0;

        for (const credential of credentials) {
            const daysUntilExpiry = (credential.expiresAt - now) / (1000 * 60 * 60 * 24);

            // Check if expired
            if (daysUntilExpiry <= 0) {
                await this.handleExpiredCredential(credential);
                expiredCount++;
                continue;
            }

            // Check if expiring soon
            if (daysUntilExpiry <= this.EXPIRY_WARNING_DAYS) {
                warnings.push(credential);
            }
        }

        // Send warnings
        if (warnings.length > 0) {
            await this.sendExpiryWarning(warnings);
        }

        console.log(`✓ Expiry check complete: ${expiredCount} expired, ${warnings.length} warnings`);
    }

    /**
     * Handles expired credential.
     */
    private static async handleExpiredCredential(credential: StoredCredential): Promise<void> {
        console.log(`⚠️ Credential expired: ${credential.id} (policy: ${credential.policyId})`);

        // Mark as expired in storage
        await CredentialStorageManager.deleteCredential(credential.id);

        // Send notification
        await this.sendExpiredNotification(credential);
    }

    /**
     * Sends expiry warning notification.
     */
    private static async sendExpiryWarning(credentials: StoredCredential[]): Promise<void> {
        const chromeGlobal = (globalThis as any).chrome;
        if (!chromeGlobal?.notifications) {
            return;
        }

        const daysText = this.EXPIRY_WARNING_DAYS === 1 ? 'day' : 'days';
        const credentialsText = credentials.length === 1 ? 'credential' : 'credentials';

        await chromeGlobal.notifications.create({
            type: 'basic',
            iconUrl: 'icons/icon128.png',
            title: '⚠️ Credentials Expiring Soon',
            message: `${credentials.length} ${credentialsText} will expire within ${this.EXPIRY_WARNING_DAYS} ${daysText}. Click to renew.`,
            priority: 2,
            requireInteraction: true,
            buttons: [
                { title: 'Renew Now' },
                { title: 'Remind Later' }
            ]
        });

        console.log(`✓ Sent expiry warning for ${credentials.length} credentials`);
    }

    /**
     * Sends expired notification.
     */
    private static async sendExpiredNotification(credential: StoredCredential): Promise<void> {
        const chromeGlobal = (globalThis as any).chrome;
        if (!chromeGlobal?.notifications) {
            return;
        }

        await chromeGlobal.notifications.create({
            type: 'basic',
            iconUrl: 'icons/icon128.png',
            title: '❌ Credential Expired',
            message: `Your ${credential.policyId} credential has expired. Click to renew.`,
            priority: 2,
            requireInteraction: true,
            buttons: [
                { title: 'Renew Now' }
            ]
        });

        console.log(`✓ Sent expired notification for credential: ${credential.id}`);
    }

    /**
     * Handles notification button click.
     */
    static async handleNotificationClick(notificationId: string, buttonIndex?: number): Promise<void> {
        const chromeGlobal = (globalThis as any).chrome;

        if (buttonIndex === 0) {
            // "Renew Now" clicked
            console.log('🔄 User initiated credential renewal');
            await this.initiateRenewal();
        } else if (buttonIndex === 1) {
            // "Remind Later" clicked
            console.log('⏰ User postponed renewal');
            // Clear notification
            await chromeGlobal.notifications.clear(notificationId);
        }
    }

    /**
     * Initiates credential renewal flow.
     */
    private static async initiateRenewal(): Promise<void> {
        // Get expiring credentials
        const credentials = await this.getExpiringCredentials();

        if (credentials.length === 0) {
            console.log('No credentials need renewal');
            return;
        }

        // Open popup for renewal
        const chromeGlobal = (globalThis as any).chrome;
        if (chromeGlobal?.action?.openPopup) {
            await chromeGlobal.action.openPopup();
        } else if (chromeGlobal?.browserAction?.openPopup) {
            await chromeGlobal.browserAction.openPopup();
        }
    }

    /**
     * Gets credentials that are expiring soon or expired.
     */
    private static async getExpiringCredentials(): Promise<StoredCredential[]> {
        const credentials = await CredentialStorageManager.listCredentials({
            status: 'active'
        });

        const now = Date.now();
        const expiringThreshold = now + (this.EXPIRY_WARNING_DAYS * 24 * 60 * 60 * 1000);

        return credentials.filter(c => c.expiresAt <= expiringThreshold);
    }

    /**
     * Manually triggers expiry check (for testing).
     */
    static async triggerManualCheck(): Promise<void> {
        console.log('🔍 Manual expiry check triggered');
        await this.performExpiryCheck();
    }
}
