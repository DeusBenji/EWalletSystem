# Threat Model — ZKP Credential Platform

**Version:** 1.0  
**Last Updated:** 2026-02-11  
**Status:** Active

---

## 1. System Overview

The ZKP Credential Platform enables users to prove attributes (age, licenses, certifications) to websites using Zero-Knowledge Proofs without revealing sensitive personal information. The system consists of:

- **IdentityService**: Issues policy credentials after user authentication
- **ValidationService**: Verifies ZKP proofs from websites
- **Browser Extension**: Acts as user's HSM (Hardware Security Module)
- **Website SDK**: Integration library for websites
- **Circuit Assets**: zk-SNARK circuits for proof generation

---

## 2. Actors

### Legitimate Actors
- **User**: Person proving attributes via browser extension
- **Website**: Relying party requesting attribute verification
- **Issuer (IdentityService)**: Issues signed policy credentials
- **Validator (ValidationService)**: Verifies ZKP proofs
- **Circuit Authority**: Signs circuit artifacts offline

### Threat Actors
- **Malicious Website**: Attempts to steal credentials or correlate users
- **Malicious Extension**: Fake extension attempting to phish credentials
- **Network Attacker**: MitM, replay, tampering
- **Compromised Issuer**: Signing key leaked
- **Supply Chain Attacker**: Tampers with circuits or dependencies

---

## 3. Assets

### Critical Assets
1. **Issuer Signing Keys**: JWT signing keys for policy credentials
2. **Circuit Signing Key**: Offline key for circuit manifest attestation
3. **User Credentials**: Policy credentials stored in extension
4. **User Secrets**: birthYear, salt, deviceTag stored locally
5. **Verification Keys**: Public keys for proof verification
6. **Circuit Artifacts**: WASM, zKey files

### Sensitive Data
- User PII (birthdate, name, CPR) — **NEVER stored in platform**
- Policy credentials (expiry, policyId, issuedAt)
- Device binding tags

---

## 4. Threat Analysis (STRIDE)

### 4.1 Spoofing

#### Threat: Fake Browser Extension
**Attack:** Malicious actor publishes fake extension with similar name  
**Impact:** User installs fake extension, credentials stolen  
**Mitigation:**
- Official extension ID hardcoded in documentation
- Visual verification guide (screenshots)
- Chrome Web Store verification badge
- **Test:** User education materials created

#### Threat: Issuer Impersonation
**Attack:** Attacker issues fake credentials with forged signature  
**Impact:** Invalid credentials accepted by validator  
**Mitigation:**
- JWKS endpoint with key rotation
- Credential signature verification (server-side)
- Key ID binding in credentials
- **Test:** Forged signature rejection test

---

### 4.2 Tampering

#### Threat: Circuit Artifact Modification
**Attack:** Attacker replaces circuit WASM with backdoored version  
**Impact:** Proof generation leaks secrets or accepts invalid inputs  
**Mitigation:**
- Offline signing of circuit manifests
- Public key embedded in ValidationService (compile-time)
- SHA256 integrity hashes verified before loading
- CDN immutability
- **Test:** Tampered WASM rejection test, unsigned manifest rejection

#### Threat: Proof Manipulation
**Attack:** Attacker modifies proof after generation  
**Impact:** Invalid proof accepted  
**Mitigation:**
- Proof + publicSignals verified together
- Message signing in extension (anti-hijacking)
- Origin binding in public inputs
- **Test:** Modified proof rejection test

#### Threat: Credential Payload Tampering
**Attack:** User modifies credential claims (expiry, policyId)  
**Impact:** Expired credential accepted  
**Mitigation:**
- JWT signature verification
- issuedAt + expiry validation
- Clock skew tolerance (±5 min)
- **Test:** Expired credential rejection, modified claims rejection

---

### 4.3 Repudiation

#### Threat: Proof Replay Attack
**Attack:** Attacker captures valid proof and reuses it  
**Impact:** Unauthorized access on different website or different session  
**Mitigation:**
- Challenge nonce binding (website-generated)
- Origin binding (proof tied to domain)
- Timestamp validation (short validity window)
- policyHash binding (prevents cross-policy replay)
- **Test:** Cross-domain replay rejection, cross-policy replay rejection

#### Threat: Credential Sharing
**Attack:** User shares credential with another person  
**Impact:** Unauthorized person gains access  
**Mitigation:**
- Device binding (deviceTag in proof)
- Short TTL (24-72h)
- Panic button (user can wipe credentials)
- **Test:** Stolen credential on different device rejection

---

### 4.4 Information Disclosure

#### Threat: PII Leakage in Logs
**Attack:** Accidental logging of sensitive claims (birthdate, CPR)  
**Impact:** Privacy violation, GDPR breach  
**Mitigation:**
- Safe-logger abstraction (redacts forbidden fields)
- Automated PII detection in CI (blocks PR)
- Static analysis for forbidden claim names
- **Test:** CI gate test (fails if PII detected in logs)

#### Threat: Proof Correlation
**Attack:** Multiple proofs linked to same user across websites  
**Impact:** User tracking, loss of anonymity  
**Mitigation:**
- No stable identifiers in proofs
- deviceTag is local-only (not shared with websites)
- Different nonces per proof
- **Test:** Proof linkability analysis (should fail)

#### Threat: Extension Storage Exfiltration
**Attack:** Malicious website reads extension storage via exploit  
**Impact:** Credentials stolen  
**Mitigation:**
- chrome.storage isolation (not accessible to websites)
- Content script does NOT handle secrets (background only)
- Storage encryption (future enhancement)
- **Test:** Cross-origin storage access attempt (should fail)

---

### 4.5 Denial of Service

#### Threat: Proof Generation Resource Exhaustion
**Attack:** Website triggers thousands of proof requests  
**Impact:** Browser freezes, poor UX  
**Mitigation:**
- Rate limiting in extension (max 1 proof per 2s per origin)
- Timeout handling (30s max)
- User abort button
- **Test:** Rapid proof request handling

#### Threat: Circuit Downgrade Attack
**Attack:** Attacker serves old vulnerable circuit version  
**Impact:** User generates invalid proof or leaks data  
**Mitigation:**
- Minimum circuit version enforced in extension
- Manifest signature verification
- Anti-downgrade protection
- **Test:** Downgrade attempt rejection

---

### 4.6 Elevation of Privilege

#### Threat: Cross-Policy Proof Abuse
**Attack:** User generates "age_over_18" proof, tries to use as "licensed_doctor"  
**Impact:** Unauthorized access to restricted resources  
**Mitigation:**
- policyHash binding in challenge
- PolicyId validation in public signals
- Validator rejects mismatched policyId
- **Test:** Cross-policy replay rejection

#### Threat: Origin Spoofing
**Attack:** Malicious website tricks extension into generating proof for legitimate origin  
**Impact:** Proof stolen and used on legitimate site  
**Mitigation:**
- Extension verifies `origin === activeTab.url.origin`
- Origin embedded in public inputs
- SDK validates origin match
- **Test:** Origin spoofing attempt rejection

---

## 5. Misuse Scenarios

### Scenario 1: User Shares Credential
**Attack Path:**
1. User exports credential from extension
2. Shares with friend via messaging app
3. Friend imports credential into their extension

**Mitigations:**
- Device binding (proof includes deviceTag)
- Credential won't verify on different device
- Short TTL (24-72h) limits damage window

**Residual Risk:** Low (proof fails on different device)

---

### Scenario 2: Extension Compromised
**Attack Path:**
1. User installs malicious browser add-on
2. Add-on reads extension storage
3. Credentials exfiltrated

**Mitigations:**
- Panic button (user wipes credentials immediately)
- Short TTL (credentials expire quickly)
- Re-authentication required for fresh credentials
- Device binding prevents use elsewhere

**Residual Risk:** Medium (window between compromise and user detection)

---

### Scenario 3: Supply Chain Attack on Circuits
**Attack Path:**
1. Attacker compromises CI pipeline
2. Replaces circuit WASM with backdoored version
3. Users download malicious circuit

**Mitigations:**
- Offline signing (CI cannot sign manifests)
- Public key embedded in ValidationService
- Signature verification before circuit loading
- Reproducible builds

**Residual Risk:** Very Low (offline key not in CI)

---

### Scenario 4: Issuer Key Compromise
**Attack Path:**
1. Issuer signing key leaked
2. Attacker issues fake credentials
3. Credentials accepted by validator

**Mitigations:**
- Key retirement workflow (mark key as retired)
- All credentials from retired key invalid immediately
- Grace period only for rotation, NOT compromise
- Signed audit log tracks key state changes

**Residual Risk:** Low (rapid key retirement response)

---

## 6. Security Invariants (Testable)

All security invariants MUST have corresponding automated tests:

1. ✅ **Proof MUST be origin-bound**  
   → Test: Cross-domain replay rejected

2. ✅ **PolicyHash MUST be verified**  
   → Test: Cross-policy replay rejected

3. ✅ **Fail closed on any verification step**  
   → Test: Missing field → rejection

4. ✅ **No PII in logs**  
   → Test: CI gate blocks forbidden claims

5. ✅ **vKey NEVER accepted from client**  
   → Test: Server-side vKey only

6. ✅ **Credentials from retired keys invalid**  
   → Test: Retired key rejection (even within TTL)

7. ✅ **Circuit manifest MUST be signed**  
   → Test: Unsigned manifest rejected

8. ✅ **Clock skew tolerance ≤ 5 minutes**  
   → Test: Future timestamp rejection

9. ✅ **Device binding enforced**  
   → Test: deviceTag mismatch rejection

10. ✅ **Extension secrets isolated to background**  
    → Test: Content script has no access

---

## 7. Threat Model Maintenance

**Update triggers:**
- Protocol changes (proof envelope, challenge format)
- Permission changes (manifest.json)
- New attack vectors discovered
- Post-incident reviews

**Review schedule:** Quarterly + ad-hoc (before major releases)

**Ownership:** Security team + senior engineers

---

## 8. References

- [OWASP Threat Modeling](https://owasp.org/www-community/Threat_Modeling)
- [STRIDE Methodology](https://learn.microsoft.com/en-us/azure/security/develop/threat-modeling-tool-threats)
- [Zero-Knowledge Proof Security](https://z.cash/technology/zksnarks/)

---

**Approval:**  
This threat model has been reviewed and approved for Fase -1 implementation.
