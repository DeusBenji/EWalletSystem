# EWalletSystem IdentityService - Operational Runbook

## 1. System Overview
**IdentityService** is a critical security component responsible for multi-provider age verification (MitID, Swedish BankID, Norwegian BankID). It enforces strict privacy-by-design (no PII storage).

**Key Dependencies**:
- **Signicat** (External IdP)
- **SQL Server** (Persistence)
- **Redis** (Session Cache & Distributed Locking)
- **ZKP Service** (Go-based Zero-Knowledge Proof generation)

---

## 2. Incident Response

### Scenario A: Signicat Outage / 5xx Errors
**Symptoms**: Users cannot start auth, logs show `502 Bad Gateway` or `503 Service Unavailable` from Signicat API.
**Action**:
1. Check [Signicat Status Page](https://status.signicat.com/).
2. If confirmed outage: Enable **Circuit Breaker** (automatic via Polly) or manually switch feature flag to "Maintenance Mode" if available.
3. **Do not** retry indefinitely; wait for status recovery.

### Scenario B: Redis Data Loss / Flush
**Symptoms**: `CSRF_FAIL` errors spike; users get "Session expired" immediately after callback.
**Impact**: Active authentication flows will fail. Stateless verifications (already in DB) are unaffected.
**Action**:
1. Acknowledge that in-flight logins are lost.
2. Verify Redis connectivity.
3. Users must restart the flow (`/api/auth/{providerId}/start`).

### Scenario C: ZKP Verification Failures
**Symptoms**: `ValidationService` rejects proofs with `PROOF_INVALID`.
**Action**:
1. Check `zkp-service` logs for panic or logic errors.
2. Verify `zkp-service` is reachable from `ValidationService`.
3. If persistent: Check for "Epoch Mismatch" (Server time vs. Client time) or "Public Input Mismatch" (PolicyID/Commitment).

---

## 3. Troubleshooting Guide

| Error Code | Meaning | Common Cause | Resolution |
|------------|---------|--------------|------------|
| `MISSING_ATTRIBUTE` | Provider didn't return `dateOfBirth`. | User has restricted visibility or provider config issue. | Check Signicat Dashboard config for requested attributes. |
| `CSRF_FAIL` | Session ID in callback doesn't match cache. | Browser cookie issue, clearing cache, or replay attack. | Ask user to retry. Check Redis. |
| `REPLAY_DETECTED` | ZKP Challenge Hash reused. | Client trying to use old proof. | Client must request new challenge. |
| `BINDING_MISMATCH` | VC commitment != Proof commitment. | Man-in-the-Middle or client bugs. | Treat as security incident. |

---

## 4. Maintenance

### Database Drift
**Check**: Run `dotnet ef migrations script --idempotent` against prod DB.
**Fix**: Apply pending migrations via pipeline. **Do not** modify `AgeVerifications` table manually.

### Key Rotation
**Scope**: Signicat Client Secret, JWT Signing Keys.
**Procedure**:
1. Generate new keys.
2. Update Secrets (Key Vault / Kubernetes Secrets).
3. Restart `IdentityService` to reload config.
