/**
 * Content Script
 * 
 * Injected into web pages to handle communication between website and extension.
 * Acts as a secure bridge with origin validation.
 */

// Listen for messages from website
window.addEventListener('message', async (event) => {
    // Only accept messages from same window
    if (event.source !== window) {
        return;
    }

    // Ignore messages without proper structure
    if (!event.data || !event.data.type || event.data.source !== 'zkp-wallet-sdk') {
        return;
    }

    // Handle different message types
    switch (event.data.type) {
        case 'ZKP_EXTENSION_PING':
            handleExtensionPing();
            break;

        case 'ZKP_PROOF_REQUEST':
            await handleProofRequest(event.data);
            break;

        default:
            console.warn('Unknown message type from website:', event.data.type);
    }
});

/**
 * Handles extension ping (check if installed).
 */
function handleExtensionPing() {
    window.postMessage({
        type: 'ZKP_EXTENSION_PONG',
        source: 'zkp-wallet-extension'
    }, '*');
}

/**
 * Handles proof request from website.
 * 
 * SECURITY:
 * - Validates origin before forwarding to background script
 * - Never exposes secrets or credentials to webpage
 * - All crypto operations happen in background script
 */
async function handleProofRequest(data: any) {
    const { policyId, challenge } = data;

    console.log(`📨 Proof request from website: ${policyId}`);
    console.log(`   Origin: ${window.location.origin}`);

    try {
        // CRITICAL: Add origin to challenge
        const challengeWithOrigin = {
            ...challenge,
            origin: window.location.origin
        };

        // Forward to background script (with origin validation)
        const response = await chrome.runtime.sendMessage({
            type: 'GENERATE_PROOF_FOR_WEBSITE',
            payload: {
                policyId,
                challenge: challengeWithOrigin,
                origin: window.location.origin
            }
        });

        // Send response back to website
        window.postMessage({
            type: 'ZKP_PROOF_RESPONSE',
            source: 'zkp-wallet-extension',
            policyId: policyId,
            proof: response.data,
            error: response.error
        }, '*');

    } catch (error: any) {
        console.error('❌ Proof request failed:', error);

        // Send error back to website
        window.postMessage({
            type: 'ZKP_PROOF_RESPONSE',
            source: 'zkp-wallet-extension',
            policyId: policyId,
            error: {
                code: 'PROOF_GENERATION_FAILED',
                message: error.message || 'Failed to generate proof'
            }
        }, '*');
    }
}

console.log('✅ ZKP Wallet content script loaded');
