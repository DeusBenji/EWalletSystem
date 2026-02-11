# GDPR Compliance Documentation

## Overview

The ZKP Credential Platform is designed to minimize personal data processing through zero-knowledge proofs. This document details GDPR compliance measures.

## Data Protection Principles

### 1. Data Minimization
**Implementation:**
- ZKP proofs reveal ONLY the claim (e.g., "over 18")
- NO personal identifiers transmitted
- NO birthdates, names, or addresses in proofs

**Evidence:**
```typescript
// Public signals contain ONLY the claim result
export interface PublicSignals {
  policyId: string;        // e.g., "age_over_18"
  claimResult: boolean;    // true/false
  policyVersion: string;   // "1.0.0"
  // NO PII
}
```

### 2. Purpose Limitation
**Purposes:**
- Age verification
- License verification  
- Citizenship verification

**NOT used for:**
- User tracking
- Profiling
- Marketing

### 3. Storage Limitation
**Retention Periods:**
| Data Type | Retention | Justification |
|-----------|-----------|---------------|
| Credentials | Until expiry or deletion | User consent |
| Audit logs | 90 days | Security monitoring |
| Validation logs | 30 days | Fraud detection |
| Analytics | 365 days (aggregated) | Service improvement |

### 4. Accuracy
- Credentials issued by authoritative sources (MitID)
- Automatic expiry prevents stale data
- User-initiated refresh flow

### 5. Integrity & Confidentiality
- AES-256-GCM encryption for credentials
- Non-extractable device secrets
- HTTPS/TLS for all communications
- Signed artifacts prevent tampering

### 6. Accountability
- Audit logs for all operations
- DPO designated
- DPIA completed

## Lawful Basis

### User Consent (Article 6(1)(a))
```javascript
// Explicit consent required before credential issuance
async function issueCredential() {
  const consent = await showConsentDialog({
    purpose: 'Age verification',
    data: 'Birthdate from MitID',
    retention: 'Until credential expiry',
    rights: 'You can delete anytime via panic button'
  });
  
  if (!consent.granted) {
    throw new Error('User declined consent');
  }
  
  // Proceed with issuance
}
```

## User Rights (Chapter III)

### Right to Access (Article 15)
**Implementation:**
```bash
# Users can export all their data
GET /api/user/export

Response:
{
  "credentials": [
    {
      "policyId": "age_over_18",
      "issuedAt": "2024-01-01T00:00:00Z",
      "expiresAt": "2025-01-01T00:00:00Z",
      "status": "active"
    }
  ],
  "audit_log": [ ... ],
  "device_bindings": [ ... ]
}
```

### Right to Erasure (Article 17)
**Implementation:**
- Panic button provides immediate erasure
- All local data wiped (credentials, device secret, circuits)
- Server-side deletion on request

```typescript
// Panic button implementation
async function exerciseRightToErasure() {
  await PanicButton.execute('user_right_to_erasure');
  // Logs: "User exercised right to erasure per GDPR Art. 17"
}
```

### Right to Rectification (Article 16)
- Users can re-authenticate to get updated credentials
- Old credentials automatically expired

### Right to Data Portability (Article 20)
```json
// Machine-readable export format
{
  "format": "JSON-LD",
  "credentials": [
    {
      "@context": "https://www.w3.org/2018/credentials/v1",
      "type": "VerifiableCredential",
      "credentialSubject": { ... }
    }
  }
}
```

### Right to Object (Article 21)
- Users can decline credential issuance
- Users can revoke consent and delete credentials

## Data Processing Activities

### Processing Activity Record
```yaml
Activity: ZKP Age Verification
Controller: ZKP Wallet ApS
DPO: dpo@zkp-wallet.dev
Purpose: Age verification without revealing birthdate
Legal Basis: Consent (Art. 6(1)(a))
Categories of Data:
  - Birthdate (processed into ZKP, not stored in clear)
  - Device identifier (for binding)
Recipients:
  - None (data stays on user device)
International Transfers: None
Retention: Until credential expiry (max 1 year)
Security Measures:
  - Encryption (AES-256-GCM)
  - Device binding
  - Audit logging
```

## Data Protection Impact Assessment (DPIA)

### Necessity Assessment
**Question:** Is ZKP necessary for age verification?
**Answer:** Yes. Alternative (sending birthdate) would violate data minimization.

### Risks Identified
| Risk | Impact | Likelihood | Mitigation |
|------|--------|------------|------------|
| Device theft | Medium | Low | Device binding |
| Extension compromise | High | Low | Code signing, review |
| Credential theft | Low | Low | Encryption |

### Conclusion
Risks are acceptable with mitigations in place.

## Technical & Organizational Measures

### Access Controls
```yaml
Data Access Matrix:
  Encrypted Credentials:
    - User Device: Full access
    - Backend Services: No access
  
  Audit Logs:
    - Security Team: Read-only
    - DPO: Read-only
    - Users: Own data only
  
  Signing Keys:
    - Release Manager: Sign-only (HSM)
    - No one: No export
```

### Encryption
- **At Rest:** AES-256-GCM (credentials), AES-256 (audit logs)
- **In Transit:** TLS 1.3
- **Key Management:** Web Crypto API (non-extractable keys)

### Monitoring
```javascript
// Automated GDPR compliance monitoring
setInterval(async () => {
  // Check data retention compliance
  const expiredData = await findExpiredData();
  if (expiredData.length > 0) {
    await deleteExpiredData(expiredData);
    await logDeletion('Automatic deletion per retention policy');
  }
  
  // Check for PII in logs (should never happen)
  const piiInLogs = await scanLogsForPII();
  if (piiInLogs) {
    await alertSecurityTeam('PII detected in logs!');
  }
}, 24 * 60 * 60 * 1000); // Daily
```

## Breach Notification (Article 33)

### Detection
```bash
# Automated breach detection
if detected_breach:
  severity = assess_severity()
  if severity >= HIGH:
    notify_dpo()
    start_72h_clock()
```

### Notification Template
```markdown
# Personal Data Breach Notification

**Date:** [Date]  
**Time:** [Time]  
**DPO:** dpo@zkp-wallet.dev  

## Nature of Breach
[Description]

## Categories & Numbers
- Affected users: [Number]
- Data categories: [List]

## Likely Consequences
[Impact assessment]

## Measures Taken
[Remediation steps]

## Contact Point
dpo@zkp-wallet.dev
```

## Processor Agreements (Article 28)

### Sub-processors
| Processor | Purpose | Location | DPA Signed |
|-----------|---------|----------|------------|
| Google Cloud | Hosting | EU | ✅ |
| Chrome Web Store | Distribution | Global | ✅ |

## International Transfers (Chapter V)

**Stance:** Data does not leave user device.

If backend processing needed:
- EU hosting only
- Standard Contractual Clauses (SCCs)
- Supplementary measures per Schrems II

## User Transparency

### Privacy Notice
```markdown
# ZKP Wallet Privacy Notice

## What We Collect
- Your birthdate (from MitID, processed into ZKP)
- Device identifier (for security)

## How We Use It
- Generate zero-knowledge proofs
- Verify your age without revealing birthdate

## Where It's Stored
- On your device only (encrypted)
- Not sent to our servers

## Your Rights
- Access your data (export)
- Delete your data (panic button)
- Withdraw consent (delete extension)

## Contact
DPO: dpo@zkp-wallet.dev
```

### Cookie Banner
```html
<!-- No cookies used! -->
<div class="gdpr-notice">
  <p>✅ We don't use cookies or tracking.</p>
  <p>Your credentials stay on your device.</p>
  <button>Learn More</button>
</div>
```

## Auditing & Compliance

### Annual Review
- [ ] Review data flows
- [ ] Update DPIA
- [ ] Verify retention policies
- [ ] Test data export
- [ ] Test panic button
- [ ] Review processor agreements

### Compliance Checklist
- [x] DPO appointed
- [x] DPIA completed
- [x] Privacy notice published
- [x] Data processing records maintained
- [x] User rights implemented
- [x] Breach notification procedure
- [x] Staff training completed
- [x] Technical measures documented

## Contact

**Data Protection Officer (DPO)**
- Email: dpo@zkp-wallet.dev
- Address: [Company address]

**Supervisory Authority**
- Danish Data Protection Agency (Datatilsynet)
- https://www.datatilsynet.dk/
