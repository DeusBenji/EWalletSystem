# Security Invariants — ZKP Credential Platform

**Version:** 1.0  
**Last Updated:** 2026-02-11  
**Purpose:** Testable security guarantees enforced by automated tests

---

## What Are Security Invariants?

Security invariants are **non-negotiable rules** that the system MUST uphold at all times. Each invariant has:
1. **Clear statement** (what must be true)
2. **Automated test** (how we verify it)
3. **Failure mode** (what happens if violated)

These invariants are **enforced in CI** — PRs cannot merge if tests fail.

---

## 1. Origin Binding

### Invariant
**Every proof MUST be bound to the origin (domain) that requested it.**

### Rationale
Prevents proof theft — a proof generated for `gambling.com` cannot be used on `casino.com`.

### Implementation
- Origin included in proof's public inputs
- Extension verifies `origin === activeTab.url.origin`
- Validator checks origin matches website's expected domain

### Test Suite
```csharp
[Test]
public void Proof_FromDomainA_RejectedOnDomainB()
{
    var proof = GenerateProof(origin: "https://gambling.com");
    var result = Validator.Verify(proof, expectedOrigin: "https://casino.com");
    Assert.IsFalse(result.Valid);
    Assert.Equal(ReasonCode.OriginMismatch, result.Reason);
}
```

### Failure Mode
If violated → **Cross-domain replay attack** possible

---

## 2. PolicyHash Binding

### Invariant
**Proofs MUST include policyHash in challenge to prevent cross-policy replay.**

### Rationale
Prevents a user from generating "age_over_18" proof and using it as "licensed_doctor".

### Implementation
- policyHash = SHA256(policyId + version + circuitId)
- Challenge includes policyHash
- Validator verifies policyHash matches expected policy

### Test Suite
```csharp
[Test]
public void AgeProof_CannotBeUsedAs_DoctorProof()
{
    var ageProof = GenerateProof(policyId: "age_over_18");
    var result = Validator.Verify(ageProof, expectedPolicy: "licensed_doctor");
    Assert.IsFalse(result.Valid);
    Assert.Equal(ReasonCode.PolicyMismatch, result.Reason);
}
```

### Failure Mode
If violated → **Cross-policy credential abuse**

---

## 3. Fail Closed on Verification Errors

### Invariant
**Any error during verification MUST result in rejection (fail closed, not fail open).**

### Rationale
Prevents accidental acceptance of invalid proofs due to exceptions.

### Implementation
- All verification steps wrapped in try/catch
- Exceptions → `Valid = false`
- Missing fields → rejection
- Default response: DENY

### Test Suite
```csharp
[Test]
public void MissingNonce_CausesRejection()
{
    var proof = GenerateProof(omitNonce: true);
    var result = Validator.Verify(proof);
    Assert.IsFalse(result.Valid);
    Assert.Equal(ReasonCode.MissingRequiredField, result.Reason);
}

[Test]
public void VerificationException_CausesRejection()
{
    var malformedProof = "{ invalid json";
    var result = Validator.Verify(malformedProof);
    Assert.IsFalse(result.Valid);
}
```

### Failure Mode
If violated → **Accidental acceptance** of invalid proofs

---

## 4. No PII in Logs

### Invariant
**Personal Identifiable Information (PII) MUST NEVER appear in logs.**

### Rationale
GDPR compliance + privacy-by-design.

### Implementation
- Safe-logger abstraction redacts forbidden fields
- Static analysis scans for `birthdate`, `cpr`, `ssn`, `name`, etc.
- CI gate blocks PR if PII detected

### Forbidden Claims
- `birthdate`, `birthYear` (in logs)
- `cpr`, `ssn`, `nationalId`
- `name`, `fullName`, `firstName`, `lastName`
- `email`, `phoneNumber`
- `address`, `postalCode`

### Test Suite
```csharp
[Test]
public void Logger_RedactsForbiddenClaims()
{
    var user = new { birthdate = "1990-01-01", name = "John Doe" };
    var logOutput = SafeLogger.Log(user);
    Assert.DoesNotContain("1990-01-01", logOutput);
    Assert.DoesNotContain("John Doe", logOutput);
    Assert.Contains("[REDACTED]", logOutput);
}
```

### CI Gate
```yaml
# .github/workflows/security-gates.yml
- name: PII Detection
  run: |
    # Scan all log statements for forbidden claims
    grep -r "birthdate\|cpr\|ssn" src/ && exit 1 || exit 0
```

### Failure Mode
If violated → **Privacy breach + GDPR violation**

---

## 5. vKey NEVER Accepted from Client

### Invariant
**Verification keys (vKey) MUST only be loaded server-side, NEVER from client.**

### Rationale
Client-provided vKey allows proof of arbitrary false statements.

### Implementation
- vKey embedded in ValidationService or fetched from trusted CDN
- vKey fingerprint verified against policy registry
- Client cannot supply vKey in proof request

### Test Suite
```csharp
[Test]
public void ClientSuppliedVKey_IsRejected()
{
    var proof = new ProofEnvelope
    {
        proof = validProof,
        vKey = maliciousVKey  // Client tries to supply vKey
    };
    var result = Validator.Verify(proof);
    Assert.IsFalse(result.Valid);
    Assert.Equal(ReasonCode.ClientVKeyRejected, result.Reason);
}
```

### Failure Mode
If violated → **Complete system compromise** (proof of false claims)

---

## 6. Retired Keys Immediately Invalid

### Invariant
**Credentials signed with retired keys MUST be rejected, even within TTL.**

### Rationale
Key compromise requires instant invalidation, grace period only for rotation.

### Implementation
- Key states: `current`, `deprecated` (grace period), `retired` (no grace)
- Validator checks key state before verification
- Retired key → immediate rejection

### Test Suite
```csharp
[Test]
public void RetiredKey_RejectedEvenWithinTTL()
{
    IssuerKeyStore.MarkKeyRetired(keyId: "key-2024-01");
    var credential = GenerateCredential(
        keyId: "key-2024-01",
        issuedAt: DateTime.Now.AddHours(-1),  // Fresh credential
        expiry: DateTime.Now.AddDays(1)       // Not expired
    );
    var result = Validator.VerifyCredential(credential);
    Assert.IsFalse(result.Valid);
    Assert.Equal(ReasonCode.RetiredKeyUsed, result.Reason);
}
```

### Failure Mode
If violated → **Compromised credentials remain valid**

---

## 7. Circuit Manifest MUST Be Signed

### Invariant
**All circuit manifests MUST have valid offline signature before use.**

### Rationale
Prevents supply chain attacks (tampered circuits).

### Implementation
- Circuit manifest signed offline with dedicated key
- Public key embedded in ValidationService (compile-time)
- Signature verified before loading circuit

### Test Suite
```csharp
[Test]
public void UnsignedManifest_IsRejected()
{
    var manifest = LoadManifest(signed: false);
    var result = CircuitLoader.LoadCircuit(manifest);
    Assert.IsFalse(result.Success);
    Assert.Equal(LoadError.MissingSignature, result.Error);
}

[Test]
public void WrongSignature_IsRejected()
{
    var manifest = LoadManifest(signedWith: differentKey);
    var result = CircuitLoader.LoadCircuit(manifest);
    Assert.IsFalse(result.Success);
    Assert.Equal(LoadError.InvalidSignature, result.Error);
}
```

### Failure Mode
If violated → **Supply chain compromise** (backdoored circuits)

---

## 8. Clock Skew Tolerance ≤ 5 Minutes

### Invariant
**Credential timestamps MUST be within ±5 minutes of validator clock.**

### Rationale
Prevents replay attacks with far-future timestamps.

### Implementation
- Validator checks: `|now - issuedAt| ≤ 5 minutes`
- `nbf` (not-before) claim validated
- Future timestamps rejected

### Test Suite
```csharp
[Test]
public void FutureCredential_IsRejected()
{
    var credential = GenerateCredential(
        issuedAt: DateTime.Now.AddMinutes(10)  // 10 minutes in future
    );
    var result = Validator.VerifyCredential(credential);
    Assert.IsFalse(result.Valid);
    Assert.Equal(ReasonCode.ClockSkewExceeded, result.Reason);
}

[Test]
public void WithinSkewTolerance_IsAccepted()
{
    var credential = GenerateCredential(
        issuedAt: DateTime.Now.AddMinutes(3)  // 3 minutes in future (OK)
    );
    var result = Validator.VerifyCredential(credential);
    Assert.IsTrue(result.Valid);
}
```

### Failure Mode
If violated → **Replay attacks** with far-future credentials

---

## 9. Device Binding Enforced

### Invariant
**Proofs MUST include deviceTag, verified against credential binding.**

### Rationale
Prevents credential sharing between users.

### Implementation
- deviceTag = random 256-bit (NOT hardware ID)
- Credential includes deviceTag hash
- Proof generation requires deviceTag match

### Test Suite
```csharp
[Test]
public void WrongDeviceTag_CausesProofRejection()
{
    var credential = IssueCredential(deviceTag: "device-A");
    var proof = GenerateProof(credential, deviceTag: "device-B");
    // Proof generation should fail client-side
    Assert.IsNull(proof);
}
```

### Failure Mode
If violated → **Credential sharing** possible

---

## 10. Extension Secrets Isolated to Background

### Invariant
**Content scripts MUST NOT have access to secrets (credentials, deviceTag, keys).**

### Rationale
Content scripts run in website context — vulnerable to XSS.

### Implementation
- All secrets stored/accessed only in background service worker
- Content script → background message passing (no direct access)
- Message signing prevents hijacking

### Test Suite
```javascript
// Extension integration test
test('Content script cannot access storage', async () => {
  const result = await chrome.storage.local.get('credentials');
  expect(result).toBeUndefined();  // Content script has no access
});

test('Background worker can access storage', async () => {
  // Run in background context
  const result = await chrome.storage.local.get('credentials');
  expect(result.credentials).toBeDefined();
});
```

### Failure Mode
If violated → **XSS = credential theft**

---

## CI Enforcement

All security invariants are enforced via CI gates:

```yaml
# .github/workflows/security-gates.yml
name: Security Gates

on: [pull_request]

jobs:
  security-invariants:
    runs-on: ubuntu-latest
    steps:
      - name: Run Security Invariant Tests
        run: dotnet test --filter Category=SecurityInvariant
      
      - name: PII Detection
        run: ./scripts/detect-pii.sh
      
      - name: Block if tests fail
        if: failure()
        run: exit 1
```

**PR CANNOT MERGE if ANY security invariant test fails.**

---

## Maintenance

**Update triggers:**
- New security invariant identified
- Threat model changes
- Protocol changes
- Post-incident findings

**Review:** Quarterly + before major releases

**Ownership:** Security team + platform engineers
