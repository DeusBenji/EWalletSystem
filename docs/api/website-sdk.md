# Website SDK API Reference

## Overview

The ZKP Wallet SDK enables websites to request zero-knowledge proofs from users via the browser extension. It provides a simple, secure API for age verification and credential validation without accessing personal data.

**Package:** `@zkp-wallet/sdk`  
**Version:** v1.0  
**License:** MIT

---

## Installation

### NPM

```bash
npm install @zkp-wallet/sdk
```

```typescript
import { ZKPWalletSDK } from '@zkp-wallet/sdk';
```

### CDN with SRI

```html
<script 
  src="https://cdn.zkp-wallet.dev/v1.0.0/zkp-wallet-sdk.min.js"
  integrity="sha384-oqVuAfXRKap7fdgcCY5uykM6+R9GqQ8K/ux=" 
  crossorigin="anonymous">
</script>
```

**Generate SRI hash:**
```bash
openssl dgst -sha384 -binary zkp-wallet-sdk.min.js | openssl base64 -A
```

---

## API Reference

### ZKPWalletSDK.verifyPolicy()

Requests a zero-knowledge proof for a specific policy.

**Signature:**
```typescript
async function verifyPolicy(options: VerifyPolicyOptions): Promise<VerifyPolicyResult>
```

**Parameters:**
```typescript
interface VerifyPolicyOptions {
  policyId: string;              // Policy to verify (e.g., "age_over_18")
  challenge?: Challenge;         // Optional custom challenge (recommended)
  timeout?: number;              // Timeout in milliseconds (default: 30000)
}

interface Challenge {
  nonce?: string;                // Random nonce (auto-generated if not provided)
  timestamp?: number;            // Unix timestamp (auto-generated if not provided)
}
```

**Returns:**
```typescript
interface VerifyPolicyResult {
  success: boolean;
  proof?: ProofEnvelope;         // ZK proof envelope (if success)
  error?: {
    code: string;
    message: string;
  };
}

interface ProofEnvelope {
  version: string;
  policyId: string;
  policyVersion: string;
  circuitVersion: string;
  proof: object;                 // ZK proof
  publicSignals: {
    claimResult: boolean;        // The verification result
    policyHash: string;
    credentialHash: string;
    timestamp: number;
  };
  metadata: {
    origin: string;
    challenge: Challenge;
    signature: string;
    timestamp: number;
  };
}
``` **Examples:**

**Basic Usage:**
```typescript
const result = await ZKPWalletSDK.verifyPolicy({
  policyId: 'age_over_18'
});

if (result.success && result.proof.publicSignals.claimResult) {
  console.log('✅ User is over 18');
  grantAccess();
} else {
  console.log('❌ Verification failed');
  denyAccess();
}
```

**With Custom Challenge:**
```typescript
// Get server-side nonce
const nonce = await fetch('/api/generate-nonce').then(r => r.json());

const result = await ZKPWalletSDK.verifyPolicy({
  policyId: 'age_over_18',
  challenge: {
    nonce: nonce.value,
    timestamp: Date.now()
  },
  timeout: 60000  // 60 seconds
});

if (result.success) {
  // Validate proof on backend
  const validation = await fetch('/api/validate-proof', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({
      proof: result.proof,
      nonce: nonce.value
    })
  });
  
  if (validation.ok) {
    grantAccess();
  }
}
```

**Error Handling:**
```typescript
const result = await ZKPWalletSDK.verifyPolicy({ policyId: 'age_over_18' });

if (!result.success) {
  switch (result.error.code) {
    case 'TIMEOUT':
      alert('Request timed out. Please try again.');
      break;
    case 'USER_DECLINED':
      alert('You declined to provide proof.');
      break;
    case 'NO_CREDENTIAL':
      alert('You need to add a credential first.');
      break;
    case 'EXTENSION_NOT_INSTALLED':
      window.location = 'https://zkp-wallet.dev/install';
      break;
    default:
      alert('Verification failed: ' + result.error.message);
  }
}
```

---

### ZKPWalletSDK.isExtensionInstalled()

Checks if the ZKP Wallet extension is installed.

**Signature:**
```typescript
async function isExtensionInstalled(): Promise<boolean>
```

**Returns:** `true` if extension is installed, `false` otherwise.

**Example:**
```typescript
const installed = await ZKPWalletSDK.isExtensionInstalled();

if (!installed) {
  showInstallPrompt();
} else {
  // Proceed with verification
  const result = await ZKPWalletSDK.verifyPolicy({ policyId: 'age_over_18' });
}
```

---

### ZKPWalletSDK.getVersion()

Gets the SDK version.

**Signature:**
```typescript
function getVersion(): string
```

**Returns:** Semantic version string (e.g., "1.0.0").

**Example:**
```typescript
console.log('SDK version:', ZKPWalletSDK.getVersion());
```

---

## Error Codes

| Code | Description | User Action |
|------|-------------|-------------|
| `EXTENSION_NOT_INSTALLED` | Extension not found | Install extension |
| `TIMEOUT` | Request timed out | Retry or increase timeout |
| `USER_DECLINED` | User rejected request | Inform user verification needed |
| `NO_CREDENTIAL` | No credential for policy | Prompt credential issuance |
| `INVALID_CHALLENGE` | Challenge format invalid | Check challenge parameters |
| `NETWORK_ERROR` | Communication failure | Check network, retry |
| `UNKNOWN_ERROR` | Unexpected error | Contact support |

---

## Complete Examples

### Age-Restricted Content

```typescript
// Check if extension is installed
const installed = await ZKPWalletSDK.isExtensionInstalled();
if (!installed) {
  document.getElementById('install-prompt').style.display = 'block';
  return;
}

// Request age verification
const result = await ZKPWalletSDK.verifyPolicy({
  policyId: 'age_over_18',
  timeout: 30000
});

if (result.success && result.proof.publicSignals.claimResult) {
  // Validate proof on backend
  const response = await fetch('/api/verify-age', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ proof: result.proof })
  });
  
  if (response.ok) {
    // Grant access to age-restricted content
    document.getElementById('restricted-content').style.display = 'block';
  } else {
    alert('Backend validation failed');
  }
} else {
  alert('Age verification failed');
}
```

### Driver's License Verification

```typescript
const result = await ZKPWalletSDK.verifyPolicy({
  policyId: 'drivers_license'
});

if (result.success && result.proof.publicSignals.claimResult) {
  // User has valid driver's license
  enableCarRental();
} else {
  alert('Valid driver\'s license required');
}
```

### Anti-Replay with Server Nonce

```typescript
// 1. Get nonce from server
const nonceResponse = await fetch('/api/auth/nonce', {
  method: 'POST',
  credentials: 'include'
});
const { nonce, expiresAt } = await nonceResponse.json();

// 2. Request proof with server nonce
const result = await ZKPWalletSDK.verifyPolicy({
  policyId: 'age_over_18',
  challenge: {
    nonce: nonce,
    timestamp: Date.now()
  }
});

if (!result.success) {
  console.error('Proof generation failed:', result.error);
  return;
}

// 3. Validate proof on server
const validationResponse = await fetch('/api/auth/verify', {
  method: 'POST',
  headers: { 'Content-Type': 'application/json' },
  credentials: 'include',
  body: JSON.stringify({
    proof: result.proof,
    nonce: nonce
  })
});

const validation = await validationResponse.json();

if (validation.valid) {
  // Set session cookie, grant access
  window.location = '/dashboard';
} else {
  alert('Validation failed');
}
```

---

## Security Best Practices

### 1. Always Verify on Backend

**❌ DON'T:**
```typescript
// Client-side only verification (INSECURE!)
const result = await ZKPWalletSDK.verifyPolicy({ policyId: 'age_over_18' });
if (result.success) {
  grantAccess(); // ❌ Can be bypassed
}
```

**✅ DO:**
```typescript
// Client gets proof, backend validates
const result = await ZKPWalletSDK.verifyPolicy({ policyId: 'age_over_18' });
const validation = await fetch('/api/validate', {
  method: 'POST',
  body: JSON.stringify({ proof: result.proof })
});

if (validation.ok) {
  grantAccess(); // ✅ Server-side validation
}
```

### 2. Use Server-Side Nonces

```typescript
// Generate nonce on server
const nonce = await generateSecureNonce(); // Server-side

// Send to client
res.json({ nonce });

// Client includes in challenge
const result = await ZKPWalletSDK.verifyPolicy({
  policyId: 'age_over_18',
  challenge: { nonce }
});

// Server validates nonce was issued and unused
```

### 3. Set Appropriate Timeouts

```typescript
// Short timeout for better UX
const result = await ZKPWalletSDK.verifyPolicy({
  policyId: 'age_over_18',
  timeout: 30000  // 30 seconds
});

// Handle timeout gracefully
if (result.error?.code === 'TIMEOUT') {
  showRetryButton();
}
```

### 4. Use HTTPS Only

```html
<!-- ❌ INSECURE -->
<script src="http://cdn.zkp-wallet.dev/sdk.js"></script>

<!-- ✅ SECURE -->
<script 
  src="https://cdn.zkp-wallet.dev/sdk.js"
  integrity="sha384-..."
  crossorigin="anonymous">
</script>
```

### 5. Implement SRI (Subresource Integrity)

Always use SRI when loading from CDN:

```html
<script 
  src="https://cdn.zkp-wallet.dev/v1.0.0/zkp-wallet-sdk.min.js"
  integrity="sha384-oqVuAfXRKap7fdgcCY5uykM6+R9GqQ8K/ux="
  crossorigin="anonymous">
</script>
```

---

## Backend Validation

Your backend MUST validate proofs:

```typescript
// Example Express.js endpoint
app.post('/api/validate-proof', async (req, res) => {
  const { proof, nonce } = req.body;
  
  // 1. Validate nonce
  if (!await validateNonce(nonce)) {
    return res.status(400).json({ valid: false, error: 'Invalid nonce' });
  }
  
  // 2. Send proof to ValidationService
  const response = await fetch('https://validation-service.zkp-wallet.dev/api/validate', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(proof)
  });
  
  const result = await response.json();
  
  // 3. Check result
  if (result.valid && result.claimResult) {
    // Mark nonce as used
    await markNonceUsed(nonce);
    
    // Grant access
    res.json({ valid: true });
  } else {
    res.json({ valid: false, error: result.errorMessage });
  }
});
```

---

## Browser Compatibility

| Browser | Minimum Version | Support Status |
|---------|----------------|----------------|
| Chrome | 88+ | ✅ Full support |
| Edge | 88+ | ✅ Full support |
| Firefox | 90+ | 🚧 Coming soon |
| Safari | - | ❌ Not supported |
| Opera | 74+ | ✅ Full support |

---

## TypeScript Support

The SDK includes TypeScript declarations:

```typescript
import { ZKPWalletSDK, VerifyPolicyOptions, VerifyPolicyResult } from '@zkp-wallet/sdk';

const options: VerifyPolicyOptions = {
  policyId: 'age_over_18',
  timeout: 30000
};

const result: VerifyPolicyResult = await ZKPWalletSDK.verifyPolicy(options);
```

---

## Troubleshooting

### Extension Not Detected

```typescript
const installed = await ZKPWalletSDK.isExtensionInstalled();
if (!installed) {
  console.error('Extension not installed');
  // Show install instructions
  showInstallPrompt();
}
```

### Timeout Issues

```typescript
// Increase timeout for slower devices
const result = await ZKPWalletSDK.verifyPolicy({
  policyId: 'age_over_18',
  timeout: 60000  // 60 seconds
});
```

### CORS Errors

Make sure your backend allows requests:

```typescript
// Example CORS configuration
app.use(cors({
  origin: 'https://your-website.com',
  credentials: true
}));
```

---

## Testing

### Test Mode

For development, use test mode:

```typescript
// Enable test mode (bypasses extension)
ZKPWalletSDK.setTestMode(true);

// Mock proof generation
ZKPWalletSDK.mockProof({
  policyId: 'age_over_18',
  claimResult: true
});
```

---

## Support

- **Documentation:** https://docs.zkp-wallet.dev/sdk
- **GitHub:** https://github.com/zkp-wallet/sdk
- **NPM:** https://npmjs.com/package/@zkp-wallet/sdk
- **Discord:** https://discord.gg/zkp-wallet
- **Email:** sdk-support@zkp-wallet.dev

---

## Changelog

### v1.0.0 (2026-02-11)
- Initial release
- `verifyPolicy()` function
- Extension detection
- Timeout handling
- Error handling
- TypeScript support
