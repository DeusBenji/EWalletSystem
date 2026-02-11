# ZKP Wallet SDK Integration Guide

## Overview

The ZKP Wallet SDK allows websites to request zero-knowledge proofs from users without accessing their credentials. This guide shows how to integrate the SDK safely and securely.

## Installation

### Option 1: NPM (Recommended)

```bash
npm install @zkp-wallet/sdk
```

```javascript
import { ZKPWalletSDK } from '@zkp-wallet/sdk';
```

### Option 2: CDN with Subresource Integrity (SRI)

**CRITICAL**: Always use SRI to ensure the SDK hasn't been tampered with.

```html
<script 
  src="https://cdn.zkp-wallet.dev/v1.0.0/zkp-wallet-sdk.min.js"
  integrity="sha384-ABC123..." 
  crossorigin="anonymous">
</script>
```

**Generate SRI hash:**
```bash
openssl dgst -sha384 -binary zkp-wallet-sdk.min.js | openssl base64 -A
```

## Basic Usage

### Age Verification Example

```javascript
// Request proof that user is over 18
const result = await ZKPWalletSDK.verifyPolicy({
  policyId: 'age_over_18'
});

if (result.success) {
  // User proved they are over 18 (without revealing exact age or identity)
  console.log('✓ Age verified');
  grantAccess();
} else {
  // Verification failed or user declined
  console.error('✗ Age verification failed:', result.error);
  denyAccess();
}
```

### Driver's License Verification

```javascript
const result = await ZKPWalletSDK.verifyPolicy({
  policyId: 'drivers_license',
  timeout: 60000 // 60 seconds
});

if (result.success) {
  console.log('✓ Driver's license verified');
  // User has a valid driver's license
  enableCarRental();
} else {
  console.error('✗ Verification failed:', result.error);
}
```

## Advanced Usage

### Custom Challenge (Anti-Replay)

```javascript
// Generate server-side nonce
const serverNonce = await fetch('/api/generate-nonce').then(r => r.json());

const result = await ZKPWalletSDK.verifyPolicy({
  policyId: 'age_over_18',
  challenge: {
    nonce: serverNonce.nonce,
    timestamp: Date.now()
  }
});

if (result.success) {
  // Send proof to backend for validation
  await fetch('/api/validate-proof', {
    method: 'POST',
    body: JSON.stringify({
      proof: result.proof,
      nonce: serverNonce.nonce
    })
  });
}
```

### Check Extension Installation

```javascript
const isInstalled = await ZKPWalletSDK.isExtensionInstalled();

if (!isInstalled) {
  // Show installation prompt
  showExtensionInstallBanner();
}
```

## Error Handling

The SDK uses fail-closed security. **Always check `result.success` before granting access.**

```javascript
const result = await ZKPWalletSDK.verifyPolicy({ policyId: 'age_over_18' });

if (!result.success) {
  // Handle error
  switch (result.error?.code) {
    case 'TIMEOUT':
      console.error('Request timed out');
      break;
    case 'USER_DECLINED':
      console.error('User declined to provide proof');
      break;
    case 'NO_CREDENTIAL':
      console.error('User does not have required credential');
      break;
    case 'EXTENSION_NOT_INSTALLED':
      console.error('Extension not installed');
      break;
    default:
      console.error('Unknown error:', result.error?.message);
  }
  
  // CRITICAL: Deny access on any error
  denyAccess();
}
```

## Security Best Practices

### 1. Always Verify on Backend

**Never trust client-side verification alone.**

```javascript
// Frontend
const result = await ZKPWalletSDK.verifyPolicy({ policyId: 'age_over_18' });

// Send proof to backend
const response = await fetch('/api/verify', {
  method: 'POST',
  body: JSON.stringify({ proof: result.proof })
});

// Backend validates proof envelope
if (response.ok) {
  grantAccess();
}
```

### 2. Use Subresource Integrity (SRI)

Always use SRI when loading from CDN to prevent tampering.

### 3. Implement Timeout

```javascript
const result = await ZKPWalletSDK.verifyPolicy({
  policyId: 'age_over_18',
  timeout: 30000 // 30 seconds max
});
```

### 4. Validate Origin

The extension automatically validates the origin. The proof is bound to your domain and cannot be replayed on other sites.

### 5. Use HTTPS Only

**Never use this SDK over HTTP.** The extension will reject requests from non-HTTPS origins.

## Backend Validation

Your backend must validate the proof envelope using the ValidationService.

```csharp
// C# example
var result = await proofEnvelopeValidator.ValidateEnvelopeAsync(
    proofEnvelope,
    expectedOrigin: "https://your-site.com"
);

if (result.Valid) {
    // Proof is valid
    grantAccess();
} else {
    // Proof is invalid
    log.Error($"Proof validation failed: {result.ErrorCode}");
    denyAccess();
}
```

## Supported Policies

| Policy ID | Description |
|-----------|-------------|
| `age_over_18` | Proves user is 18+ without revealing birthdate |
| `age_over_21` | Proves user is 21+ without revealing birthdate |
| `drivers_license` | Proves user has valid driver's license |
| `eu_citizen` | Proves EU citizenship without revealing country |

## Browser Compatibility

| Browser | Minimum Version |
|---------|----------------|
| Chrome | 88+ |
| Edge | 88+ |
| Firefox | 90+ (Coming Soon) |
| Safari | Not Supported |

## Troubleshooting

### Extension Not Detected

```javascript
const isInstalled = await ZKPWalletSDK.isExtensionInstalled();
if (!isInstalled) {
  // Show install instructions
  window.location = 'https://zkp-wallet.dev/install';
}
```

### Timeout Issues

Increase timeout for slower devices:

```javascript
const result = await ZKPWalletSDK.verifyPolicy({
  policyId: 'age_over_18',
  timeout: 60000 // 60 seconds
});
```

### CORS Issues

Make sure your backend allows requests from your frontend domain.

## Complete Example

```html
<!DOCTYPE html>
<html>
<head>
  <title>Age-Restricted Content</title>
  <script 
    src="https://cdn.zkp-wallet.dev/v1.0.0/zkp-wallet-sdk.min.js"
    integrity="sha384-ABC123..."
    crossorigin="anonymous">
  </script>
</head>
<body>
  <div id="loading">Verifying age...</div>
  <div id="content" style="display: none">
    <h1>Welcome to age-restricted content!</h1>
  </div>
  <div id="error" style="display: none">
    <h1>Access Denied</h1>
    <p id="error-message"></p>
  </div>

  <script>
    (async function() {
      try {
        // Check extension installed
        const installed = await ZKPWalletSDK.isExtensionInstalled();
        if (!installed) {
          throw new Error('Please install ZKP Wallet extension');
        }

        // Request age verification
        const result = await ZKPWalletSDK.verifyPolicy({
          policyId: 'age_over_18',
          timeout: 30000
        });

        if (result.success) {
          // Verify on backend
          const validated = await fetch('/api/validate-proof', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ proof: result.proof })
          });

          if (validated.ok) {
            // Show content
            document.getElementById('loading').style.display = 'none';
            document.getElementById('content').style.display = 'block';
          } else {
            throw new Error('Backend validation failed');
          }
        } else {
          throw new Error(result.error?.message || 'Verification failed');
        }
      } catch (error) {
        // Show error
        document.getElementById('loading').style.display = 'none';
        document.getElementById('error').style.display = 'block';
        document.getElementById('error-message').textContent = error.message;
      }
    })();
  </script>
</body>
</html>
```

## Support

- Documentation: https://docs.zkp-wallet.dev
- GitHub: https://github.com/zkp-wallet/sdk
- Discord: https://discord.gg/zkp-wallet
