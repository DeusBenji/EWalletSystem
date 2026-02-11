# Extension Compromise Runbook

## Purpose
Response procedures when the browser extension itself is compromised.

## Scenarios

### 1. Malicious Extension Update
**Indicators:**
- Reports of unauthorized transactions
- Unexpected credential exports
- Suspicious network activity from extension

### 2. Extension Marketplace Hijack
**Indicators:**
- Unauthorized extension version published
- Publisher account takeover
- Fake extension with similar name

### 3. Zero-Day Vulnerability
**Indicators:**
- Public CVE disclosure
- Exploit code published
- Active exploitation detected

## Immediate Response (First Hour)

### 1. Verify Reports
```javascript
// Check extension version distribution
fetch('https://chrome.google.com/webstore/detail/zkp-wallet/[id]')
  .then(r => r.text())
  .then(html => console.log(html.match(/version.*?\d+\.\d+\.\d+/)));
```

### 2. Emergency Actions

#### Option A: Request Chrome Web Store Takedown
```bash
# Submit emergency takedown request
# https://support.google.com/chrome_webstore/contact/dev_account_issues

# Reason: Security incident
# Evidence: [Attach proof of compromise]
```

#### Option B: Push Panic Update
```javascript
// Create emergency update that:
// 1. Disables all functionality
// 2. Shows warning to users
// 3. Initiates panic button automatically

// manifest.json v1.X.Y-emergency
{
  "version": "1.X.Y",
  "content_scripts": [{
    "matches": ["<all_urls>"],
    "js": ["emergency-disable.js"],
    "run_at": "document_start"
  }]
}
```

```javascript
// emergency-disable.js
(function() {
  'use strict';
  
  // Disable all extension functionality
  chrome.runtime.sendMessage({
    type: 'EMERGENCY_DISABLE',
    reason: 'Security incident detected'
  });
  
  // Show warning
  if (window === window.top) {
    alert('⚠️ ZKP Wallet Security Alert\n\n' +
          'We have detected a security issue. The extension has been disabled.\n' +
          'Please visit https://zkp-wallet.dev/security for updates.');
  }
})();
```

### 3. User Communication

**Immediate (< 1 hour):**
```
🚨 SECURITY ALERT

We are investigating a potential security issue with ZKP Wallet.

IMMEDIATE ACTIONS:
1. Disable the extension: chrome://extensions
2. Activate panic button if possible
3. Monitor accounts for unauthorized activity

Updates: https://zkp-wallet.dev/security
Support: security@zkp-wallet.dev
```

## Recovery Procedure

### Step 1: Assess Impact

```bash
# Check affected versions
cat <<EOF > affected-versions.json
{
  "compromised_versions": ["1.2.3", "1.2.4"],
  "safe_versions": ["1.2.2"],
  "patched_versions": ["1.2.5"]
}
EOF

# Estimate affected users
curl -H "Authorization: Bearer $ANALYTICS_TOKEN" \
  https://api.zkp-wallet.dev/analytics/version-distribution
```

### Step 2: Remediate Vulnerability

```bash
# Create patch branch
git checkout -b hotfix/security-CVE-2024-XXXX

# Apply security patch
# ...

# Test thoroughly
npm run test:security
npm run test:e2e

# Build patched version
npm run build

# Increment version
npm version patch -m "Security fix: CVE-2024-XXXX"
```

### Step 3: Emergency Deployment

```bash
# Package extension
npm run package

# Submit to Chrome Web Store (expedited review)
# Mark as critical security update

# Upload via CWS dashboard
# Request expedited review (usually 24-48 hours)
```

### Step 4: Force Update

```javascript
// Update minimum version on server
await fetch('https://api.zkp-wallet.dev/admin/extension/minimum-version', {
  method: 'POST',
  headers: { 'Authorization': `Bearer ${ADMIN_TOKEN}` },
  body: JSON.stringify({
    version: '1.2.5',
    force: true,
    reason: 'Critical security update'
  })
});

// Extension will check on startup and force update
```

### Step 5: Monitor Rollout

```bash
# Monitor version adoption
watch -n 60 'curl -s https://api.zkp-wallet.dev/analytics/versions | jq'

# Expected: 90% adoption within 48 hours
```

## User Remediation Guide

### For Affected Users

```markdown
# Recovery Steps for Affected Users

1. **Remove Extension**
   - Go to chrome://extensions
   - Find "ZKP Wallet"
   - Click "Remove"

2. **Clear Extension Data**
   - Settings → Privacy → Clear browsing data
   - Select "Cookies and site data" for "All time"
   - Clear

3. **Reinstall Safe Version**
   - Visit https://chrome.google.com/webstore/detail/zkp-wallet/[id]
   - Verify version is 1.2.5 or higher
   - Click "Add to Chrome"

4. **Re-authenticate**
   - Open extension
   - Sign in with MitID
   - Re-issue credentials

5. **Monitor Activity**
   - Check for unauthorized proofs
   - Review audit log
   - Report suspicious activity
```

## Backup Distribution Channels

If Chrome Web Store is unavailable:

### Self-Hosted Distribution
```bash
# Host .crx file on our CDN
aws s3 cp zkp-wallet-1.2.5.crx s3://cdn.zkp-wallet.dev/extension/

# Update download page
cat > download.html <<'EOF'
<!DOCTYPE html>
<html>
<head><title>ZKP Wallet - Emergency Download</title></head>
<body>
  <h1>Emergency Extension Download</h1>
  <p>Chrome Web Store is currently unavailable.</p>
  
  <h2>Installation Instructions</h2>
  <ol>
    <li>Download: <a href="https://cdn.zkp-wallet.dev/extension/zkp-wallet-1.2.5.crx">
        zkp-wallet-1.2.5.crx</a></li>
    <li>Verify SHA256: <code>abc123...</code></li>
    <li>Drag .crx file to chrome://extensions</li>
  </ol>
  
  <h2>Verification</h2>
  <pre>
  sha256sum zkp-wallet-1.2.5.crx
  # Expected: abc123...
  </pre>
</body>
</html>
EOF
```

### Edge Add-ons / Firefox
```bash
# Submit to alternative stores for redundancy
# Microsoft Edge Add-ons: https://partner.microsoft.com/dashboard/microsoftedge
# Firefox Add-ons: https://addons.mozilla.org/developers/
```

## CVE Disclosure Process

### 1. Internal Disclosure
- Security team notified
- Exploit proof-of-concept created
- Patch developed and tested

### 2. Coordinated Disclosure (90 days)
```bash
# Register CVE
curl -X POST https://cveform.mitre.org/ \
  -d "product=ZKP Wallet Extension" \
  -d "version=1.2.3" \
  -d "vulnerability=XSS in content script"

# Assign CVE-2024-XXXX
```

### 3. Public Disclosure
```markdown
# Security Advisory: CVE-2024-XXXX

**Severity:** High  
**Affected Versions:** 1.2.0 - 1.2.4  
**Fixed in:** 1.2.5  

## Description
A cross-site scripting vulnerability in the content script allowed
malicious websites to access extension storage.

## Impact
Attackers could potentially extract encrypted credentials from
extension storage. Device secret remained secure.

## Remediation
Update to version 1.2.5 or later immediately.

## Timeline
- 2024-01-01: Vulnerability discovered
- 2024-01-02: Patch developed
- 2024-01-03: Version 1.2.5 released
- 2024-01-05: Public disclosure

## Credits
Thanks to [Security Researcher] for responsible disclosure.
```

## Post-Incident

### 1. Root Cause Analysis
```markdown
# RCA: Extension Compromise Incident

## Timeline
[Detailed incident timeline]

## Root Cause
[Technical root cause]

## Contributing Factors
- [Factor 1]
- [Factor 2]

## Remediation
- [Fix applied]
- [Prevention measures]

## Action Items
- [ ] Update security review checklist
- [ ] Add automated detection
- [ ] Improve monitoring
```

### 2. Update Security Measures
```javascript
// Add runtime protection
chrome.runtime.onMessage.addListener((msg, sender, respond) => {
  // Validate message origin
  if (!sender.tab || !sender.tab.url.startsWith('https://')) {
    console.error('Blocked message from non-HTTPS origin');
    return;
  }
  
  // Rate limiting
  if (isRateLimited(sender.tab.id)) {
    console.error('Rate limit exceeded');
    return;
  }
  
  // ... handle message
});
```

## Testing

```bash
# Simulate compromise scenario
./scripts/test-emergency-response.sh

# Expected: Complete remediation in < 6 hours
```

## Contacts

| Role | Contact | SLA |
|------|---------|-----|
| Chrome Web Store | webstore-support@google.com | 24-48h |
| Security Team | security@zkp-wallet.dev | < 1h |
| PR/Comms | pr@zkp-wallet.dev | < 2h |
