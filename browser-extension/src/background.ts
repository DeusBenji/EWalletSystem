/**
 * Background Script
 * 
 * Main service worker for the browser extension.
 * Handles credential storage, proof generation, and message routing.
 */

import { CredentialStorageManager } from './CredentialStorageManager';
import { MitIdCredentialIssuer } from './MitIdCredentialIssuer';
import { ZkpProofGenerator } from './ZkpProofGenerator';
import { PanicButton } from './PanicButton';

// Message types from content script / popup
interface ExtensionMessage {
    type: 'GENERATE_PROOF' | 'ISSUE_CREDENTIAL' | 'LIST_CREDENTIALS' | 'DELETE_CREDENTIAL' | 'PANIC_BUTTON';
    payload: any;
}

/**
 * Handles messages from content scripts and popup.
 */
chrome.runtime.onMessage.addListener((message: ExtensionMessage, sender, sendResponse) => {
    console.log('📨 Message received:', message.type);

    // Handle message async
    handleMessage(message, sender)
        .then(result => {
            sendResponse({ success: true, data: result });
        })
        .catch(error => {
            console.error('❌ Message handler error:', error);
            sendResponse({ success: false, error: error.message });
        });

    // Return true to indicate async response
    return true;
});

/**
 * Routes messages to appropriate handlers.
 */
async function handleMessage(message: ExtensionMessage, sender: chrome.runtime.MessageSender): Promise<any> {
    switch (message.type) {
        case 'GENERATE_PROOF':
            return await handleGenerateProof(message.payload);

        case 'ISSUE_CREDENTIAL':
            return await handleIssueCredential(message.payload);

        case 'LIST_CREDENTIALS':
            return await handleListCredentials(message.payload);

        case 'DELETE_CREDENTIAL':
            return await handleDeleteCredential(message.payload);

        case 'PANIC_BUTTON':
            return await handlePanicButton(message.payload);

        default:
            throw new Error(`Unknown message type: ${message.type}`);
    }
}

/**
 * Handles GENERATE_PROOF message.
 */
async function handleGenerateProof(payload: any): Promise<any> {
    const { credentialId, challenge } = payload;

    // 1. Get credential
    const credential = await CredentialStorageManager.getCredential(credentialId);

    // 2. Generate proof
    const envelope = await ZkpProofGenerator.generateProof({
        policyId: credential.policyId,
        challenge: challenge,
        credential: credential
    });

    return envelope;
}

/**
 * Handles ISSUE_CREDENTIAL message.
 */
async function handleIssueCredential(payload: any): Promise<any> {
    const { policyId } = payload;

    // 1. Authenticate with MitID
    const authResult = await MitIdCredentialIssuer.initiateMitIdAuth();

    // 2. Issue credential
    const credentialId = await MitIdCredentialIssuer.issueCredential(authResult, policyId);

    return { credentialId };
}

/**
 * Handles LIST_CREDENTIALS message.
 */
async function handleListCredentials(payload: any): Promise<any> {
    const { filter } = payload;
    const credentials = await CredentialStorageManager.listCredentials(filter);
    return { credentials };
}

/**
 * Handles DELETE_CREDENTIAL message.
 */
async function handleDeleteCredential(payload: any): Promise<any> {
    const { credentialId } = payload;
    await CredentialStorageManager.deleteCredential(credentialId);
    return { success: true };
}

/**
 * Handles PANIC_BUTTON message.
 */
async function handlePanicButton(payload: any): Promise<any> {
    await PanicButton.execute('user_initiated');
    return { success: true };
}

/**
 * Extension installed/updated handler.
 */
chrome.runtime.onInstalled.addListener((details) => {
    console.log('🎉 Extension installed:', details.reason);

    if (details.reason === 'install') {
        // First install - show welcome page
        chrome.tabs.create({ url: 'welcome.html' });
    }
});

console.log('✅ Background script loaded');
