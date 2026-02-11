# Browser Extension (Wallet) API Reference

## Overview

The ZKP Wallet browser extension manages credentials and generates zero-knowledge proofs. It exposes a message-based API for communication between the extension's content scripts, background service worker, and popup UI.

**Extension ID:** `zkp-wallet-extension`  
**Supported Browsers:** Chrome 88+, Edge 88+  
**Manifest Version:** 3

---

## Architecture

```
┌─────────────┐      ┌──────────────────┐      ┌───────────────┐
│   Website   │ ←───→│ Content Script   │ ←───→│   Background  │
│             │      │ (content.ts)     │      │  (background  │
└─────────────┘      └──────────────────┘      │   .ts)        │
                                                └───────┬───────┘
                                                        │
                                                        ↓
                                                ┌───────────────┐
                                                │   Storage     │
                                                │(chrome.storage│
                                                │   .local)     │
                                                └───────────────┘
```

---

## Message API

### Message Types

All messages follow this structure:

```typescript
interface ExtensionMessage {
  type: MessageType;
  payload: any;
}

type MessageType =
  | 'GENERATE_PROOF'
  | 'GENERATE_PROOF_FOR_WEBSITE'
  | 'ISSUE_CREDENTIAL'
  | 'LIST_CREDENTIALS'
  | 'DELETE_CREDENTIAL'
  | 'PANIC_BUTTON'
  | 'CHECK_EXPIRY';
```

---

### GENERATE_PROOF

Generates a ZKP proof for a specific credential.

**Sender:** Popup UI  
**Receiver:** Background script

**Request:**
```typescript
{
  type: 'GENERATE_PROOF',
  payload: {
    credentialId: string;
    challenge: {
      nonce: string;
      timestamp: number;
      origin: string;
    }
  }
}
```

**Response:**
```typescript
{
  success: boolean;
  data?: ProofEnvelope;    // If successful
  error?: string;          // If failed
}

interface ProofEnvelope {
  version: string;
  policyId: string;
  policyVersion: string;
  circuitVersion: string;
  proof: ZKProof;
  publicSignals: PublicSignals;
  metadata: ProofMetadata;
}
```

**Example (Popup UI):**
```typescript
// Send message to background
const response = await chrome.runtime.sendMessage({
  type: 'GENERATE_PROOF',
  payload: {
    credentialId: 'cred_abc123',
    challenge: {
      nonce: 'random-nonce-xyz',
      timestamp: Date.now(),
      origin: 'https://example.com'
    }
  }
});

if (response.success) {
  console.log('Proof generated:', response.data);
} else {
  console.error('Proof generation failed:', response.error);
}
```

---

### GENERATE_PROOF_FOR_WEBSITE

Generates a proof in response to a website request.

**Sender:** Content script (relaying from website)  
**Receiver:** Background script

**Request:**
```typescript
{
  type: 'GENERATE_PROOF_FOR_WEBSITE',
  payload: {
    policyId: string;
    challenge: {
      nonce: string;
      timestamp: number;
      origin: string;
    };
    origin: string;        // Website origin for validation
  }
}
```

**Security:**
- Origin is validated (must match tab URL)
- Challenge origin must match request origin
- User consent required (TODO: implement consent dialog)

**Example (Content Script):**
```typescript
// Relay website request to background
const response = await chrome.runtime.sendMessage({
  type: 'GENERATE_PROOF_FOR_WEBSITE',
  payload: {
    policyId: 'age_over_18',
    challenge: websiteChallenge,
    origin: window.location.origin
  }
});

// Send response back to website
window.postMessage({
  type: 'ZKP_WALLET_RESPONSE',
  requestId: originalRequestId,
  result: response
}, '*');
```

---

### ISSUE_CREDENTIAL

Initiates credential issuance flow via MitID.

**Sender:** Popup UI  
**Receiver:** Background script

**Request:**
```typescript
{
  type: 'ISSUE_CREDENTIAL',
  payload: {
    policyId: string;      // e.g., "age_over_18"
  }
}
```

**Response:**
```typescript
{
  success: boolean;
  data?: {
    credentialId: string;
  };
  error?: string;
}
```

**Flow:**
1. Background script opens MitID auth popup
2. User authenticates with MitID
3. Background exchanges code for tokens
4. Background requests credential from TokenService
5. Credential is encrypted and stored locally

**Example (Popup UI):**
```typescript
const response = await chrome.runtime.sendMessage({
  type: 'ISSUE_CREDENTIAL',
  payload: {
    policyId: 'age_over_18'
  }
});

if (response.success) {
  console.log('Credential issued:', response.data.credentialId);
} else {
  console.error('Issuance failed:', response.error);
}
```

---

### LIST_CREDENTIALS

Lists stored credentials with optional filtering.

**Sender:** Popup UI  
**Receiver:** Background script

**Request:**
```typescript
{
  type: 'LIST_CREDENTIALS',
  payload: {
    filter?: {
      policyId?: string;
      status?: 'active' | 'expired' | 'revoked';
    }
  }
}
```

**Response:**
```typescript
{
  success: boolean;
  data?: {
    credentials: StoredCredential[];
  };
  error?: string;
}

interface StoredCredential {
  id: string;
  policyId: string;
  policyVersion: string;
  issuer: string;
  issuedAt: number;       // Unix timestamp
  expiresAt: number;      // Unix timestamp
  status: 'active' | 'expired' | 'revoked';
}
```

**Example (Popup UI):**
```typescript
const response = await chrome.runtime.sendMessage({
  type: 'LIST_CREDENTIALS',
  payload: {
    filter: {
      status: 'active'
    }
  }
});

if (response.success) {
  console.log('Active credentials:', response.data.credentials);
}
```

---

### DELETE_CREDENTIAL

Deletes a specific credential.

**Sender:** Popup UI  
**Receiver:** Background script

**Request:**
```typescript
{
  type: 'DELETE_CREDENTIAL',
  payload: {
    credentialId: string;
  }
}
```

**Response:**
```typescript
{
  success: boolean;
  error?: string;
}
```

**Example (Popup UI):**
```typescript
const response = await chrome.runtime.sendMessage({
  type: 'DELETE_CREDENTIAL',
  payload: {
    credentialId: 'cred_abc123'
  }
});

if (response.success) {
  console.log('Credential deleted');
}
```

---

### PANIC_BUTTON

Emergency wipe of all data (credentials, device secret, circuits).

**Sender:** Popup UI  
**Receiver:** Background script

**Request:**
```typescript
{
  type: 'PANIC_BUTTON',
  payload: {}
}
```

**Response:**
```typescript
{
  success: boolean;
  error?: string;
}
```

**Security:**
- Irreversible operation
- Wipes ALL data
- Creates audit log entry
- Shows user notification

**Example (Popup UI):**
```typescript
if (confirm('⚠️ This will delete ALL your credentials. Continue?')) {
  const response = await chrome.runtime.sendMessage({
    type: 'PANIC_BUTTON',
    payload: {}
  });
  
  if (response.success) {
    console.log('All data wiped');
  }
}
```

---

### CHECK_EXPIRY

Manually triggers expiry check.

**Sender:** Popup UI  
**Receiver:** Background script

**Request:**
```typescript
{
  type: 'CHECK_EXPIRY',
  payload: {}
}
```

**Response:**
```typescript
{
  success: boolean;
}
```

---

## Storage Schema

### chrome.storage.local

```typescript
// Credentials (encrypted)
{
  "credentials": {
    "cred_abc123": {
      id: string;
      policyId: string;
      policyVersion: string;
      encryptedData: {
        ciphertext: string;     // Base64-encoded
        iv: string;             // Base64-encoded
        authTag: string;        // Base64-encoded
        deviceTag: string;      // Device binding
      };
      issuedAt: number;
      expiresAt: number;
      issuer: string;
      status: 'active' | 'expired' | 'revoked';
    }
  },
  
  // Credential index (for fast lookups)
  "credentialIndex": {
    "byPolicyId": {
      "age_over_18": ["cred_abc123", "cred_def456"]
    },
    "byStatus": {
      "active": ["cred_abc123"],
      "expired": ["cred_def456"]
    }
  },
  
  // Device secret (non-extractable key ID)
  "deviceSecretKeyId": "device-secret-key-2026-02-11",
  
  // Audit log
  "auditLog": [
    {
      timestamp: number;
      eventType: string;
      details: object;       // No PII
    }
  ]
}
```

**Encryption:** AES-256-GCM with non-extractable key (Web Crypto API)

---

## Content Script Communication

### Website → Extension

Websites send messages via `window.postMessage`:

```typescript
// Website code
window.postMessage({
  type: 'ZKP_WALLET_REQUEST',
  requestId: 'req_123',
  method: 'verifyPolicy',
  params: {
    policyId: 'age_over_18',
    challenge: {
      nonce: 'random-nonce',
      timestamp: Date.now()
    }
  }
}, '*');
```

### Extension → Website

Content script responds via `window.postMessage`:

```typescript
// Content script
window.postMessage({
  type: 'ZKP_WALLET_RESPONSE',
  requestId: 'req_123',
  result: {
    success: true,
    proof: proofEnvelope
  }
}, '*');
```

**Security:** Content script validates origin before forwarding to background.

---

## Permissions

Required Chrome permissions (manifest.json):

```json
{
  "permissions": [
    "storage",           // chrome.storage.local
    "alarms",            // Expiry checks
    "notifications",     // User notifications
    "identity"           // OAuth (chrome.identity.launchWebAuthFlow)
  ],
  "host_permissions": [
    "https://identity-service.zkp-wallet.dev/*",
    "https://token-service.zkp-wallet.dev/*"
  ]
}
```

---

## Event Listeners

### Extension Startup

```typescript
// background.ts
chrome.runtime.onInstalled.addListener((details) => {
  if (details.reason === 'install') {
    chrome.tabs.create({ url: 'welcome.html' });
  }
});

// Initialize credential refresh manager
CredentialRefreshManager.initialize();
```

### Message Listener

```typescript
// background.ts
chrome.runtime.onMessage.addListener((message, sender, sendResponse) => {
  handleMessage(message, sender)
    .then(result => sendResponse({ success: true, data: result }))
    .catch(error => sendResponse({ success: false, error: error.message }));
  
  return true; // Async response
});
```

### Alarm Listener (Expiry Checks)

```typescript
// background.ts
chrome.alarms.onAlarm.addListener((alarm) => {
  if (alarm.name === 'credential-expiry-check') {
    CredentialRefreshManager.checkExpiry();
  }
});
```

---

## Security Considerations

### Device Binding

Credentials are bound to device via non-extractable key:

```typescript
// Generate device-bound key (only once)
const key = await crypto.subtle.generateKey(
  { name: 'AES-GCM', length: 256 },
  false,  // ❌ NOT extractable
  ['encrypt', 'decrypt']
);

// Store key in IndexedDB (not extractable from storage)
await indexedDB.put('device-secret', key);
```

### No PII in Logs

```typescript
// ❌ BAD
console.log('User birthdate:', credential.birthdate);

// ✅ GOOD
console.log('Credential issued for policy:', credential.policyId);
```

---

## Testing

### Manual Testing

Load unpacked extension:
1. Open `chrome://extensions`
2. Enable "Developer mode"
3. Click "Load unpacked"
4. Select `browser-extension/dist`

### Unit Tests

```bash
cd browser-extension
npm test
```

---

## Support

- **Extension:** https://chrome.google.com/webstore/detail/zkp-wallet/[id]
- **Issues:** https://github.com/zkp-wallet/browser-extension/issues
- **Email:** extension-support@zkp-wallet.dev

---

## Changelog

### v1.0.0 (2026-02-11)
- Initial release
- MitID authentication
- Credential storage
- ZKP proof generation
- Panic button
