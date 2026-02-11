# TokenService API Reference

## Overview

The TokenService issues verifiable credentials based on authenticated user identities from IdentityService. Credentials are signed JWTs containing user claims for specific policies.

**Base URL:** `https://token-service.zkp-wallet.dev/api`  
**Version:** v1.0  
**Protocol:** REST (JSON)

---

## Core Concepts

### Credentials
Signed JWT tokens containing user claims tailored to specific verification policies.

**Example Credential (JWT payload):**
```json
{
  "iss": "https://token-service.zkp-wallet.dev",
  "sub": "user-unique-id",
  "aud": "zkp-wallet-extension",
  "exp": 1735689600,
  "iat": 1704067200,
  "policyId": "age_over_18",
  "policyVersion": "1.0.0",
  "claims": {
    "birthdate": "1990-01-15",
    "isOver18": true
  }
}
```

### Policies
Define what claims are included in credentials (e.g., age_over_18, drivers_license).

---

## API Endpoints

### POST /api/credentials/issue

Issues a new credential for an authenticated user.

**Request:**
```http
POST /api/credentials/issue HTTP/1.1
Authorization: Bearer <access_token_from_identity_service>
Content-Type: application/json

{
  "policyId": "age_over_18",
  "policyVersion": "1.0.0"
}
```

**Request Model:**
```typescript
interface IssueCredentialRequest {
  policyId: string;        // Policy identifier (e.g., "age_over_18")
  policyVersion?: string;  // Specific policy version (default: latest)
}
```

**Response (200 OK):**
```json
{
  "credential": "eyJhbGciOiJFUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJodHRwczovL3Rva2VuLXNlcnZpY2UuemtwLXdhbGxldC 5kZXYiLCJzdWIiOiJ1c2VyLXVuaXF1ZS1pZCIsImF1ZCI6InprcC13YWxsZXQtZXh0ZW5zaW9uIiwiZXhwIjoxNzM1Njg5NjAwLCJpYXQiOjE3MDQwNjcyMDAsInBvbGljeUlkIjoiYWdlX292ZXJfMTgiLCJwb2xpY3lWZXJzaW9uIjoiMS4wLjAiLCJjbGFpbXMiOnsiYmlydGhkYXRlIjoiMTk5MC0wMS0xNSIsImlzT3ZlcjE4Ijp0cnVlfX0.signature",
  "credentialId": "cred_abc123",
  "expiresAt": "2025-01-01T00:00:00Z",
  "policyId": "age_over_18",
  "policyVersion": "1.0.0"
}
```

**Response Model:**
```typescript
interface IssueCredentialResponse {
  credential: string;      // Signed JWT credential
  credentialId: string;    // Unique credential identifier
  expiresAt: string;       // ISO 8601 expiration time
  policyId: string;        // Policy used for issuance
  policyVersion: string;   // Policy version
}
```

**Errors:**
- `400 Bad Request` - Invalid policy ID or version
- `401 Unauthorized` - Missing or invalid access token
- `403 Forbidden` - User doesn't meet policy requirements
- `404 Not Found` - Policy not found
- `500 Internal Server Error` - Server error

**Example (curl):**
```bash
curl -X POST https://token-service.zkp-wallet.dev/api/credentials/issue \
  -H "Authorization: Bearer eyJhbGci..." \
  -H "Content-Type: application/json" \
  -d '{"policyId": "age_over_18"}'
```

**Example (JavaScript):**
```javascript
const response = await fetch('https://token-service.zkp-wallet.dev/api/credentials/issue', {
  method: 'POST',
  headers: {
    'Authorization': `Bearer ${accessToken}`,
    'Content-Type': 'application/json'
  },
  body: JSON.stringify({
    policyId: 'age_over_18'
  })
});

const { credential, expiresAt } = await response.json();
console.log('Credential issued, expires:', expiresAt);
```

---

### POST /api/credentials/revoke

Revokes an issued credential.

**Request:**
```http
POST /api/credentials/revoke HTTP/1.1
Authorization: Bearer <access_token>
Content-Type: application/json

{
  "credentialId": "cred_abc123",
  "reason": "user_request"
}
```

**Request Model:**
```typescript
interface RevokeCredentialRequest {
  credentialId: string;    // Credential to revoke
  reason?: string;         // Revocation reason
}

type RevocationReason = 
  | 'user_request'         // User requested revocation
  | 'credential_compromise' // Credential compromised
  | 'policy_change'        // Policy updated/deprecated
  | 'administrative';      // Admin action
```

**Response (200 OK):**
```json
{
  "revoked": true,
  "revokedAt": "2026-02-11T16:00:00Z"
}
```

**Example (curl):**
```bash
curl -X POST https://token-service.zkp-wallet.dev/api/credentials/revoke \
  -H "Authorization: Bearer eyJhbGci..." \
  -H "Content-Type: application/json" \
  -d '{"credentialId": "cred_abc123", "reason": "user_request"}'
```

---

### GET /api/credentials/status/{credentialId}

Checks the status of a credential.

**Request:**
```http
GET /api/credentials/status/cred_abc123 HTTP/1.1
Authorization: Bearer <access_token>
```

**Response (200 OK):**
```json
{
  "credentialId": "cred_abc123",
  "status": "active",
  "issuedAt": "2026-01-01T00:00:00Z",
  "expiresAt": "2027-01-01T00:00:00Z",
  "revokedAt": null
}
```

**Response Model:**
```typescript
interface CredentialStatus {
  credentialId: string;
  status: 'active' | 'expired' | 'revoked';
  issuedAt: string;        // ISO 8601 timestamp
  expiresAt: string;       // ISO 8601 timestamp
  revokedAt?: string;      // ISO 8601 timestamp (if revoked)
}
```

**Example (JavaScript):**
```javascript
const response = await fetch(
  `https://token-service.zkp-wallet.dev/api/credentials/status/${credentialId}`,
  {
    headers: { 'Authorization': `Bearer ${accessToken}` }
  }
);

const status = await response.json();
console.log('Credential status:', status.status);
```

---

### GET /api/policies

Lists available credential policies.

**Request:**
```http
GET /api/policies HTTP/1.1
```

**Response (200 OK):**
```json
{
  "policies": [
    {
      "policyId": "age_over_18",
      "name": "Age Verification (18+)",
      "description": "Proves user is 18 or older",
      "latestVersion": "1.0.0",
      "status": "active",
      "requiredClaims": ["birthdate"]
    },
    {
      "policyId": "drivers_license",
      "name": "Driver's License Verification",
      "description": "Proves user has valid driver's license",
      "latestVersion": "1.0.0",
      "status": "active",
      "requiredClaims": ["license_number", "license_expiry"]
    }
  ]
}
```

**Response Model:**
```typescript
interface PolicyListResponse {
  policies: PolicyInfo[];
}

interface PolicyInfo {
  policyId: string;
  name: string;
  description: string;
  latestVersion: string;
  status: 'active' | 'deprecated' | 'sunset';
  requiredClaims: string[];  // Claims needed from identity provider
}
```

---

## Credential Structure

### JWT Header
```json
{
  "alg": "ES256",          // ECDSA with P-256 and SHA-256
  "typ": "JWT",
  "kid": "key-id-2026-01"  // Key identifier for rotation
}
```

### JWT Payload
```json
{
  "iss": "https://token-service.zkp-wallet.dev",
  "sub": "user-unique-id",
  "aud": "zkp-wallet-extension",
  "exp": 1735689600,
  "iat": 1704067200,
  "nbf": 1704067200,
  "jti": "cred_abc123",
  
  "policyId": "age_over_18",
  "policyVersion": "1.0.0",
  
  "claims": {
    "birthdate": "1990-01-15",
    "isOver18": true
  }
}
```

**Standard Claims:**
- `iss` (Issuer): Token service URL
- `sub` (Subject): Unique user identifier
- `aud` (Audience): Intended recipient (extension)
- `exp` (Expiration): Unix timestamp
- `iat` (Issued At): Unix timestamp
- `nbf` (Not Before): Unix timestamp
- `jti` (JWT ID): Credential identifier

**Custom Claims:**
- `policyId`: Policy identifier
- `policyVersion`: Semantic version
- `claims`: Policy-specific user claims

---

## Configuration

### Environment Variables

```bash
# Database
DATABASE_CONNECTION_STRING=Server=localhost;Database=TokenService;

# Signing Keys  
JWT_SIGNING_KEY_PATH=/secrets/signing-key.pem
JWT_KEY_ID=key-2026-01

# Policy Registry
POLICY_REGISTRY_URL=https://policy-registry.zkp-wallet.dev

# Hyperledger Fabric
FABRIC_NETWORK_CONFIG=/config/fabric-network.yaml
FABRIC_USER_CERT=/secrets/fabric-user.pem
```

---

## Security Considerations

### Credential Signing

Credentials are signed with **ES256** (ECDSA P-256):

```csharp
// C# example
var signingKey = ECDsa.Create(ECCurve.NamedCurves.nistP256);
var signingCredentials = new SigningCredentials(
    new ECDsaSecurityKey(signingKey), 
    SecurityAlgorithms.EcdsaSha256
);

var credential = new JwtSecurityToken(
    issuer: "https://token-service.zkp-wallet.dev",
    audience: "zkp-wallet-extension",
    claims: userClaims,
    expires: DateTime.UtcNow.AddYears(1),
    signingCredentials: signingCredentials
);
```

### Key Rotation

Keys should be rotated every 90 days:
1. Generate new key pair
2. Publish new public key with new `kid`
3. Sign new credentials with new key
4. Keep old key for verification (6 month overlap)

---

## Rate Limiting

- **Issue endpoint:** 10 requests/hour per user
- **Revoke endpoint:** 20 requests/hour per user
- **Status endpoint:** 100 requests/hour per user
- **Policies endpoint:** Unlimited (cached)

---

## Error Codes

| Code | HTTP Status | Description |
|------|-------------|-------------|
| `INVALID_POLICY` | 400 | Policy ID or version invalid |
| `POLICY_NOT_FOUND` | 404 | Policy doesn't exist |
| `POLICY_REQUIREMENTS_NOT_MET` | 403 | User doesn't meet policy criteria |
| `CREDENTIAL_NOT_FOUND` | 404 | Credential doesn't exist |
| `CREDENTIAL_ALREADY_REVOKED` | 409 | Credential already revoked |
| `RATE_LIMIT_EXCEEDED` | 429 | Too many requests |

---

## Examples

### Complete Issuance Flow

```typescript
// 1. Authenticate user
const identityResponse = await fetch('https://identity-service.../auth/token', {
  method: 'POST',
  body: JSON.stringify({ code: authCode })
});
const { access_token } = await identityResponse.json();

// 2. Issue credential
const credentialResponse = await fetch('https://token-service.../credentials/issue', {
  method: 'POST',
  headers: {
    'Authorization': `Bearer ${access_token}`,
    'Content-Type': 'application/json'
  },
  body: JSON.stringify({
    policyId: 'age_over_18'
  })
});

const { credential, expiresAt } = await credentialResponse.json();

// 3. Store credential securely
await storageManager.storeCredential(credential);

console.log('Credential issued, valid until:', expiresAt);
```

---

## Support

- **Documentation:** https://docs.zkp-wallet.dev/token-service
- **Issues:** https://github.com/zkp-wallet/token-service/issues
- **Email:** api-support@zkp-wallet.dev

---

## Changelog

### v1.0.0 (2026-02-11)
- Initial release
- Age verification policy
- Driver's license policy
- Hyperledger Fabric integration
- JWT signing with ES256
