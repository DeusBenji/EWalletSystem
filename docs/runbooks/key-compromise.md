# Key Compromise Runbook

## Purpose
This runbook defines immediate actions and recovery procedures when signing keys are compromised.

## Detection

### Indicators of Compromise
- Unexpected signature verifications passing
- Unauthorized circuit manifests
- Anomalous key usage patterns in logs
- Security audit alerts

### Monitoring
```bash
# Check for suspicious key usage
kubectl logs -n zkp-system validation-service | grep "SIGNATURE_INVALID"

# Monitor key access
tail -f /var/log/zkp/key-access.log
```

## Severity Classification

| Severity | Description | Response Time |
|----------|-------------|---------------|
| **CRITICAL** | Circuit signing key compromised | Immediate (< 1 hour) |
| **HIGH** | Extension signing key compromised | < 4 hours |
| **MEDIUM** | Service-to-service key compromised | < 24 hours |

## Immediate Actions (First Hour)

### 1. Verify Compromise
```bash
# Check key access logs
grep "SIGNING_KEY_ACCESS" /var/log/zkp/audit.log | tail -100

# Verify current key fingerprint
openssl x509 -in circuit-signing-key.pub -fingerprint -noout
```

### 2. Contain Breach
```bash
# IMMEDIATE: Revoke compromised key in key registry
curl -X POST https://api.zkp-wallet.dev/admin/keys/revoke \
  -H "Authorization: Bearer $ADMIN_TOKEN" \
  -d '{"keyId": "compromised-key-id", "reason": "COMPROMISED"}'

# Disable affected services
kubectl scale deployment validation-service --replicas=0
```

### 3. Notify Stakeholders
- **Internal:** Security team, engineering leads, CTO
- **External:** Users via extension notification, website banner
- **Regulatory:** GDPR DPO (if PII affected)

## Recovery Procedure

### Step 1: Generate New Keys (Offline)

```bash
# Generate new ECDSA P-256 key pair (OFFLINE machine)
openssl ecparam -name prime256v1 -genkey -noout -out new-signing-key.pem

# Extract public key
openssl ec -in new-signing-key.pem -pubout -out new-signing-key.pub

# Get fingerprint
openssl x509 -in new-signing-key.pub -fingerprint -noout
# Store fingerprint securely
```

### Step 2: Update Key Registry

```bash
# Upload new public key to registry
curl -X POST https://api.zkp-wallet.dev/admin/keys/register \
  -H "Authorization: Bearer $ADMIN_TOKEN" \
  -d @new-key-registration.json

# Verify registration
curl https://api.zkp-wallet.dev/keys/current
```

### Step 3: Re-sign All Circuits

```bash
# Re-sign all circuits with new key
cd circuits/build
for manifest in circuit-*/manifest.json; do
  ./sign-manifest.sh "$manifest" ../new-signing-key.pem
done

# Verify signatures
./verify-all-signatures.sh
```

### Step 4: Deploy New keys

```bash
# Update Kubernetes secrets
kubectl create secret generic circuit-verification-key \
  --from-file=public-key=new-signing-key.pub \
  --dry-run=client -o yaml | kubectl apply -f -

# Restart validation service
kubectl rollout restart deployment/validation-service

# Verify deployment
kubectl rollout status deployment/validation-service
```

### Step 5: Force Extension Update

```bash
# Increment minimum extension version
curl -X POST https://api.zkp-wallet.dev/admin/extension/minimum-version \
  -H "Authorization: Bearer $ADMIN_TOKEN" \
  -d '{"version": "1.3.0", "reason": "Security update"}'

# Push extension update to Chrome Web Store (emergency)
# This triggers automatic updates within 24-48 hours
```

## Communication Templates

### User Notification (Extension)
```
🔒 Security Update Required

We've detected a potential security issue and have updated our systems.
Please update to the latest version of ZKP Wallet to continue using the service.

This update will happen automatically within 24 hours.

Your credentials are safe and encrypted.
```

### Public Statement
```
Security Incident Response - [Date]

We have identified and addressed a security issue affecting our signing infrastructure.
No user credentials or personal data were compromised.

Actions taken:
- Rotated all signing keys
- Re-signed all circuit artifacts
- Updated all services with new keys

Users should update to extension version X.Y.Z when available.

Timeline:
- Detection: [Time]
- Containment: [Time]
- Resolution: [Time]
```

## Recovery Time Objectives

| Component | RTO | RPO |
|-----------|-----|-----|
| Key rotation | 2 hours | N/A |
| Circuit re-signing | 4 hours | N/A |
| Service restoration | 6 hours | N/A |
| Extension deployment | 48 hours | N/A |

## Post-Incident Actions

### 1. Forensic Analysis
- Determine compromise source
- Review audit logs for unauthorized access
- Identify affected artifacts

### 2. Update Procedures
- Document lessons learned
- Update detection methods
- Improve key storage security

### 3. Regulatory Reporting
- GDPR breach notification (if applicable, within 72 hours)
- Incident report to stakeholders
- Update compliance documentation

## Prevention

### Key Storage Best Practices
- Hardware Security Module (HSM) for production keys
- Offline cold storage for master keys
- Multi-signature requirements for key operations
- Regular key rotation schedule (every 90 days)

### Access Controls
```yaml
# Example IAM policy for key access
KeyAccessPolicy:
  Effect: Allow
  Principal:
    - SecurityTeam
    - ReleaseManager
  Action:
    - kms:Sign
    - kms:Verify
  Resource: arn:aws:kms:*:*:key/circuit-signing-key
  Condition:
    IpAddress:
      aws:SourceIp:
        - "10.0.0.0/8"  # Internal network only
```

### Monitoring
- Alert on any key access outside business hours
- Alert on failed signature verifications
- Alert on key export attempts

## Testing

### Quarterly Drill
```bash
# Simulate key compromise
./run-key-compromise-drill.sh

# Expected completion: < 6 hours
# Success criteria: All steps completed without manual intervention
```

## Contact Information

| Role | Contact | Availability |
|------|---------|--------------|
| Security Lead | security@zkp-wallet.dev | 24/7 |
| On-Call Engineer | oncall@zkp-wallet.dev | 24/7 |
| CTO | cto@zkp-wallet.dev | Business hours |

## Appendix

### A. Key Fingerprint Verification
```bash
# Expected fingerprint for current key
echo "SHA256:abc123def456..." > expected-fingerprint.txt

# Verify current key matches
openssl x509 -in current-key.pub -fingerprint -sha256 -noout | \
  diff - expected-fingerprint.txt
```

### B. Emergency Contacts
- Chrome Web Store Support: https://support.google.com/chrome_webstore
- Cloud Provider Support: [Provider emergency number]
- Legal: legal@zkp-wallet.dev
