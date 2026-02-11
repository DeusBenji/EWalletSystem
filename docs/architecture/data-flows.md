# Data Flow Documentation

## Overview

This document details all major data flows in the ZKP Credential Platform using sequence diagrams and explanations.

---

## 1. Credential Issuance Flow

Complete flow from user authentication to encrypted credential storage.

```mermaid
sequenceDiagram
    actor User
    participant Ext as Browser Extension
    participant IS as IdentityService
    participant MitID
    participant TS as TokenService
    participant PR as PolicyRegistry
    participant Fabric as GoServices/Fabric
    participant Storage as chrome.storage

    User->>Ext: Request credential
    Ext->>IS: POST /api/auth/authorize
    IS-->>Ext: authorizationUrl + state
    
    Ext->>MitID: Redirect to authorizationUrl
    MitID->>User: Show login page
    User->>MitID: Authenticate (BankID/NemID)
    MitID-->>Ext: Redirect with code
    
    Ext->>IS: POST /api/auth/token {code}
    IS->>MitID: Exchange code for tokens
    MitID-->>IS: ID token + Access token
    IS-->>Ext: Tokens
    
    Ext->>TS: POST /api/credentials/issue<br/>{policyId, Bearer token}
    TS->>PR: GET /api/policies/{policyId}
    PR-->>TS: Policy definition
    
    TS->>Fabric: IssueCredential(credentialID, userID, ...)
    Fabric-->>TS: Transaction ID
    
    TS->>TS: Sign credential (ES256)
    TS-->>Ext: Signed JWT credential
    
    Ext->>Ext: Generate device secret (AES-256-GCM)
    Ext->>Ext: Encrypt credential
    Ext->>Storage: Store encrypted credential
    
    Ext-->>User: ✅ Credential issued
```

**Duration:** ~5-8 seconds

**Key Points:**
1. MitID authentication (OAuth 2.0)
2. Policy-based credential issuance
3. Blockchain recording (immutable)
4. Device-bound encryption

---

## 2. Proof Generation Flow

Zero-knowledge proof generation on user device.

```mermaid
sequenceDiagram
    actor User
    participant Website
    participant SDK as ZKP Wallet SDK
    participant Content as Content Script
    participant Background as Extension Background
    participant CR as CircuitLoader
    participant ZKP as ZkpProofGenerator
    participant Storage as chrome.storage

    Website->>SDK: verifyPolicy({policyId: "age_over_18"})
    SDK->>SDK: Generate challenge nonce
    
    SDK->>Content: postMessage: ZKP_WALLET_REQUEST
    Content->>Content: Validate origin
    Content->>Background: runtime.sendMessage<br/>GENERATE_PROOF_FOR_WEBSITE
    
    Background->>Background: Validate origin matches tab URL
    Background->>User: Show consent dialog<br/>"example.com requests age proof"
    User-->>Background: ✅ Approve
    
    Background->>Storage: Get credentials for policyId
    Storage-->>Background: Encrypted credential
    
    Background->>Background: Decrypt credential<br/>(device secret)
    
    Background->>CR: loadCircuit(policyId, version)
    CR->>CR: Verify circuit manifest signature
    CR->>CR: Validate hashes (WASM + vKey)
    CR-->>Background: Circuit loaded
    
    Background->>ZKP: generateProof({<br/>  credential,<br/>  challenge,<br/>  circuit<br/>})
    ZKP->>ZKP: Prepare inputs:<br/>- birthdate → field elements<br/>- challenge → hash
    ZKP->>ZKP: snarkjs.groth16.fullProve()
    ZKP->>ZKP: Assemble proof envelope
    ZKP->>ZKP: Sign envelope (device-bound)
    ZKP-->>Background: ProofEnvelope
    
    Background-->>Content: ProofEnvelope
    Content-->>SDK: postMessage: ZKP_WALLET_RESPONSE
    SDK-->>Website: {success: true, proof}
    
    Website->>Website: Send proof to backend
```

**Duration:** ~2-3 seconds (age verification)

**Security Highlights:**
- User consent required
- Origin validation (double-check)
- Circuit integrity verification
- Device-bound signing

---

## 3. Proof Validation Flow

Backend validation of zero-knowledge proof.

```mermaid
sequenceDiagram
    participant Website as Website Backend
    participant VS as ValidationService
    participant PR as PolicyRegistry
    participant Cache as Redis (Nonce Cache)
    participant Verifier as ZK Verifier

    Website->>VS: POST /api/validate<br/>{ProofEnvelope}
    
    VS->>VS: Parse envelope
    
    VS->>PR: GET /api/policies/{policyId}
    PR-->>VS: Policy + minimum version
    
    VS->>VS: Check anti-downgrade:<br/>policyVersion >= minimumVersion
    alt Version too old
        VS-->>Website: 403 ANTI_DOWNGRADE_VIOLATION
    end
    
    VS->>VS: Validate origin binding:<br/>envelope.origin == challenge.origin
    alt Origin mismatch
        VS-->>Website: 403 ORIGIN_MISMATCH
    end
    
    VS->>Cache: Check if nonce used
    Cache-->>VS: Not found (good)
    alt Nonce already used
        VS-->>Website: 403 NONCE_ALREADY_USED
    end
    
    VS->>VS: Check clock skew:<br/>|now - timestamp| <= 300s
    alt Clock skew too large
        VS-->>Website: 403 TIMESTAMP_OUT_OF_RANGE
    end
    
    VS->>Verifier: verifyProof({<br/>  proof,<br/>  publicSignals,<br/>  verificationKey<br/>})
    Verifier->>Verifier: Groth16 pairing check
    Verifier-->>VS: Valid: true
    
    VS->>Cache: Store nonce (TTL: 10 min)
    VS->>VS: Log validation (no PII)
    
    VS-->>Website: {<br/>  valid: true,<br/>  claimResult: true,<br/>  policyId: "age_over_18"<br/>}
    
    Website->>Website: Grant access
```

**Duration:** ~100-150ms

**Validation Steps:**
1. ✅ Anti-downgrade protection
2. ✅ Origin binding
3. ✅ Replay prevention (nonce)
4. ✅ Clock skew tolerance
5. ✅ ZK proof verification

---

## 4. MitID Authentication Flow

OAuth 2.0 + PKCE flow with MitID.

```mermaid
sequenceDiagram
    participant Ext as Extension
    participant IS as IdentityService
    participant MitID
    participant User

    Ext->>Ext: Generate PKCE:<br/>codeVerifier, codeChallenge
    
    Ext->>IS: POST /api/auth/authorize {<br/>  clientId,<br/>  redirectUri,<br/>  codeChallenge,<br/>  codeChallengeMethod: "S256"<br/>}
    
    IS->>IS: Generate state (CSRF)
    IS->>MitID: Build authorization URL
    IS-->>Ext: {authorizationUrl, state}
    
    Ext->>MitID: chrome.identity.launchWebAuthFlow(url)
    
    MitID->>User: Show authentication<br/>(BankID/NemID/App)
    User->>MitID: Authenticate
    
    MitID->>IS: Redirect: /callback?code=...&state=...
    IS->>IS: Validate state
    IS-->>Ext: Redirect: extensionCallback?code=ABC
    
    Ext->>IS: POST /api/auth/token {<br/>  code: "ABC",<br/>  codeVerifier<br/>}
    
    IS->>IS: Validate code verifier:<br/>hash(codeVerifier) == codeChallenge
    
    IS->>MitID: POST /token {<br/>  code,<br/>  client_credentials<br/>}
    
    MitID->>MitID: Validate authorization code
    MitID-->>IS: {<br/>  access_token,<br/>  id_token,<br/>  expires_in: 3600<br/>}
    
    IS->>IS: Decode + validate ID token
    IS-->>Ext: {<br/>  access_token,<br/>  id_token,<br/>  user: {sub, birthdate, ...}<br/>}
    
    Ext->>Ext: Store tokens (temporary)
```

**Security Features:**
- PKCE (prevents authorization code interception)
- State parameter (CSRF protection)
- ID token validation (signature, claims)

---

## 5. Device Binding Flow

Credential encryption with device-specific key.

```mermaid
sequenceDiagram
    participant TS as TokenService
    participant Ext as Extension
    participant WebCrypto as Web Crypto API
    participant IDB as IndexedDB
    participant Storage as chrome.storage

    TS-->>Ext: Credential (JWT)
    
    alt First time (no device secret)
        Ext->>WebCrypto: generateKey({<br/>  name: "AES-GCM",<br/>  length: 256<br/>}, extractable: false)
        WebCrypto-->>Ext: CryptoKey (non-extractable)
        Ext->>IDB: Store key<br/>(cannot be exported!)
    end
    
    Ext->>IDB: Get device secret
    IDB-->>Ext: CryptoKey
    
    Ext->>Ext: Generate deviceTag:<br/>hash(key.fingerprint || timestamp)
    
    Ext->>WebCrypto: encrypt({<br/>  name: "AES-GCM",<br/>  iv: random(12 bytes)<br/>}, key, credential)
    
    WebCrypto-->>Ext: {<br/>  ciphertext,<br/>  iv,<br/>  authTag<br/>}
    
    Ext->>Storage: Store {<br/>  id,<br/>  policyId,<br/>  encryptedData: {<br/>    ciphertext,<br/>    iv,<br/>    authTag,<br/>    deviceTag<br/>  },<br/>  expiresAt<br/>}
    
    Note over Ext,Storage: Credential bound to this device<br/>Cannot be used on other devices
```

**Security:**
- Non-extractable key (cannot leave browser)
- Device tag prevents credential theft
- AES-256-GCM (authenticated encryption)

---

## 6. Panic Button Flow

Emergency data wipe.

```mermaid
sequenceDiagram
    actor User
    participant Popup as Extension Popup
    participant Background
    participant PB as PanicButton
    participant Storage as chrome.storage
    participant IDB as IndexedDB
    participant Audit as Audit Logger

    User->>Popup: Click "Panic Button"
    Popup->>User: ⚠️ Confirm:<br/>"Delete ALL data?"
    User-->>Popup: ✅ Confirm
    
    Popup->>Background: runtime.sendMessage<br/>PANIC_BUTTON
    Background->>PB: execute("user_initiated")
    
    PB->>Audit: Log: PANIC_BUTTON_ACTIVATED
    
    par Wipe all data
        PB->>Storage: Clear all credentials
        and
        PB->>IDB: Delete device secret key
        and
        PB->>Storage: Clear cached circuits
        and
        PB->>Storage: Clear credential index
    end
    
    PB->>PB: Verify all data deleted
    
    PB->>Background: Notify user
    Background->>Popup: chrome.notifications.create({<br/>  title: "Data Wiped",<br/>  message: "All credentials deleted"<br/>})
    
    PB-->>Background: Success
    Background-->>Popup: {success: true}
    
    Popup-->>User: ✅ All data wiped<br/>Restart to re-authenticate
```

**Duration:** < 1 second

**Wipes:**
- ✅ All credentials
- ✅ Device secret
- ✅ Cached circuits
- ✅ Credential index

**Preserves:**
- ✅ Audit log (for compliance)

---

## 7. Credential Refresh Flow

Automatic expiry monitoring and renewal.

```mermaid
sequenceDiagram
    participant Alarm as chrome.alarms
    participant Manager as CredentialRefreshManager
    participant Storage as chrome.storage
    participant User
    participant TS as TokenService

    Alarm->>Manager: Daily alarm triggered
    Manager->>Storage: List all credentials
    Storage-->>Manager: Credentials
    
    loop For each credential
        Manager->>Manager: Check expiry
        
        alt Expires in < 2 days
            Manager->>User: chrome.notifications.create({<br/>  title: "Credential Expiring",<br/>  message: "age_over_18 expires in 1 day",<br/>  buttons: ["Renew Now", "Remind Later"]<br/>})
            
            alt User clicks "Renew Now"
                User->>Manager: notification.onButtonClicked
                Manager->>TS: Re-issue credential
                TS-->>Manager: New credential
                Manager->>Storage: Replace old credential
                Manager-->>User: ✅ Renewed
            end
        else
            alt Expired
                Manager->>Storage: Delete credential
                Manager-->>User: ℹ️ Credential expired
            end
        end
    end
```

**Schedule:** Daily at 9:00 AM (user timezone)

**Notifications:**
- 2 days before expiry
- 1 day before expiry
- On expiry (deletion)

---

## 8. Cross-Domain Replay Prevention

How origin binding prevents replay attacks.

```mermaid
sequenceDiagram
    participant SiteA as Site A<br/>(example.com)
    participant Ext as Extension
    participant SiteB as Site B<br/>(attacker.com)
    participant VS as ValidationService

    SiteA->>Ext: Request proof<br/>origin: "https://example.com"
    Ext->>Ext: Generate proof<br/>+ bind to origin
    Ext-->>SiteA: ProofEnvelope {<br/>  metadata: {<br/>    origin: "https://example.com",<br/>    challenge: {<br/>      origin: "https://example.com"<br/>    }<br/>  }<br/>}
    
    Note over SiteA: Attacker intercepts proof
    
    SiteA->>SiteB: Attacker copies proof
    SiteB->>VS: POST /api/validate<br/>{ProofEnvelope}
    
    VS->>VS: Check origin binding:<br/>envelope.origin === "https://example.com"<br/>requestor === "https://attacker.com"
    
    VS-->>SiteB: ❌ 403 ORIGIN_MISMATCH<br/>"Proof bound to https://example.com"
    
    SiteB-->>SiteA: ❌ Attack failed
```

**Security:**
- Proof is bound to specific origin
- ValidationService checks origin
- Cross-domain replay impossible

---

## 9. Fabric Transaction Flow

Writing credential to blockchain.

```mermaid
sequenceDiagram
    participant TS as TokenService
    participant SDK as Fabric SDK
    participant Peer as Fabric Peer
    participant Orderer
    participant Ledger

    TS->>SDK: submitTransaction(<br/>  "IssueCredential",<br/>  credentialID,<br/>  userID,<br/>  policyID,<br/>  ...<br/>)
    
    SDK->>SDK: Create proposal
    SDK->>Peer: Send proposal
    
    Peer->>Peer: Simulate transaction<br/>(chaincode execution)
    Peer->>Peer: Endorse proposal
    Peer-->>SDK: Endorsement
    
    SDK->>SDK: Collect endorsements<br/>(from multiple peers)
    
    SDK->>Orderer: Submit transaction
    Orderer->>Orderer: Order transaction
    Orderer->>Orderer: Create block
    
    Orderer->>Peer: Distribute block
    Peer->>Peer: Validate block
    Peer->>Ledger: Commit block
    Peer->>CouchDB: Update state database
    
    Peer-->>SDK: Transaction committed
    SDK-->>TS: Transaction ID:<br/>"0x123abc..."
```

**Endorsement Policy:** Requires majority of org endorsements

**Confirmation Time:** ~2-3 seconds

---

## Performance Summary

| Flow | Duration | Bottleneck |
|------|----------|------------|
| Credential Issuance | 5-8s | MitID authentication |
| Proof Generation | 2-3s | ZKP computation (client) |
| Proof Validation | 100-150ms | ZK verification |
| MitID Auth | 3-5s | User interaction |
| Device Binding | < 100ms | Encryption |
| Panic Button | < 1s | Storage operations |
| Fabric Transaction | 2-3s | Consensus |

---

## Support

- **Data Flow Docs:** https://docs.zkp-wallet.dev/architecture/data-flows
- **Sequence Diagrams:** https://github.com/zkp-wallet/docs/tree/main/diagrams
- **Email:** architecture@zkp-wallet.dev
