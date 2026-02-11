# IdentityService API Reference

## Overview

The IdentityService handles user authentication via MitID (Denmark's national digital identity solution) using OAuth 2.0 and OpenID Connect protocols.

**Base URL:** `https://identity-service.zkp-wallet.dev/api`  
**Version:** v1.0  
**Protocol:** REST (JSON)

---

## Authentication Flow

```mermaid
sequenceDiagram
    User->>Browser Extension: Request credential
    Browser Extension->>IdentityService: POST /api/auth/authorize
    IdentityService-->>Browser Extension: authorizationUrl + state
    Browser Extension->>MitID: Redirect to authorizationUrl
    MitID->>User: Authenticate (BankID/NemID/etc)
    User-->>MitID: Credentials
    MitID->>IdentityService: Redirect with code
    IdentityService->>Browser Extension: Authorization code
    Browser Extension->>IdentityService: POST /api/auth/token
    IdentityService->>MitID: Exchange code for tokens
    MitID-->>IdentityService: ID token + Access token
    IdentityService-->>Browser Extension: Tokens
```

---

## API Endpoints

### POST /api/auth/authorize

Initiates OAuth 2.0 authorization flow with MitID.

**Request:**
```http
POST /api/auth/authorize HTTP/1.1
Content-Type: application/json

{
  "clientId": "zkp-wallet-extension",
  "redirectUri": "https://extension-redirect-url",
  "scope": "openid profile",
  "responseType": "code",
  "state": "random-string-for-csrf-protection"
}
```

**Request Model:**
```typescript
interface AuthorizeRequest {
  clientId: string;        // Registered client identifier
  redirectUri: string;     // Callback URL for authorization code
  scope: string;           // Space-separated scopes (openid required)
  responseType: string;    // Always "code" for authorization code flow
  state?: string;          // CSRF protection token
  nonce?: string;          // Replay attack protection
}
```

**Response (200 OK):**
```json
{
  "authorizationUrl": "https://mitid.dk/authorize?client_id=...&redirect_uri=...&scope=openid%20profile&response_type=code&state=...",
  "state": "random-string-for-csrf-protection"
}
```

**Response Model:**
```typescript
interface AuthorizeResponse {
  authorizationUrl: string;  // URL to redirect user to for MitID login
  state: string;             // State parameter for CSRF validation
}
```

**Errors:**
- `400 Bad Request` - Invalid request parameters
- `401 Unauthorized` - Invalid client credentials
- `500 Internal Server Error` - Server error

**Example (curl):**
```bash
curl -X POST https://identity-service.zkp-wallet.dev/api/auth/authorize \
  -H "Content-Type: application/json" \
  -d '{
    "clientId": "zkp-wallet-extension",
    "redirectUri": "https://extension-callback",
    "scope": "openid profile",
    "responseType": "code"
  }'
```

**Example (JavaScript):**
```javascript
const response = await fetch('https://identity-service.zkp-wallet.dev/api/auth/authorize', {
  method: 'POST',
  headers: { 'Content-Type': 'application/json' },
  body: JSON.stringify({
    clientId: 'zkp-wallet-extension',
    redirectUri: 'https://extension-callback',
    scope: 'openid profile',
    responseType: 'code'
  })
});

const { authorizationUrl, state } = await response.json();
window.location.href = authorizationUrl; // Redirect to MitID
```

---

### POST /api/auth/token

Exchanges authorization code for access and ID tokens.

**Request:**
```http
POST /api/auth/token HTTP/1.1
Content-Type: application/json

{
  "code": "authorization-code-from-mitid",
  "redirectUri": "https://extension-redirect-url",
  "clientId": "zkp-wallet-extension",
  "codeVerifier": "pkce-code-verifier" 
}
```

**Request Model:**
```typescript
interface TokenRequest {
  code: string;           // Authorization code from MitID callback
  redirectUri: string;    // Same redirect URI used in /authorize
  clientId: string;       // Client identifier
  codeVerifier?: string;  // PKCE code verifier (recommended)
}
```

**Response (200 OK):**
```json
{
  "access_token": "eyJhbGciOiJSUzI1NiIs...",
  "id_token": "eyJhbGciOiJSUzI1NiIs...",
  "token_type": "Bearer",
  "expires_in": 3600,
  "scope": "openid profile"
}
```

**Response Model:**
```typescript
interface TokenResponse {
  access_token: string;   // JWT access token for API calls
  id_token: string;       // JWT ID token with user claims
  token_type: string;     // Always "Bearer"
  expires_in: number;     // Token lifetime in seconds
  refresh_token?: string; // Refresh token (if requested)
  scope: string;          // Granted scopes
}
```

**ID Token Claims:**
```typescript
interface IDTokenClaims {
  iss: string;           // Issuer (MitID)
  sub: string;           // Subject (unique user ID)
  aud: string;           // Audience (clientId)
  exp: number;           // Expiration time (Unix timestamp)
  iat: number;           // Issued at (Unix timestamp)
  nonce?: string;        // Nonce from /authorize request
  
  // MitID specific claims
  name?: string;         // Full name
  birthdate?: string;    // ISO 8601 date (YYYY-MM-DD)
  cpr?: string;          // CPR number (Danish SSN)
}
```

**Errors:**
- `400 Bad Request` - Invalid authorization code or parameters
- `401 Unauthorized` - Invalid client credentials
- `500 Internal Server Error` - Server error

**Example (curl):**
```bash
curl -X POST https://identity-service.zkp-wallet.dev/api/auth/token \
  -H "Content-Type: application/json" \
  -d '{
    "code": "auth-code-from-callback",
    "redirectUri": "https://extension-callback",
    "clientId": "zkp-wallet-extension"
  }'
```

**Example (JavaScript):**
```javascript
const response = await fetch('https://identity-service.zkp-wallet.dev/api/auth/token', {
  method: 'POST',
  headers: { 'Content-Type': 'application/json' },
  body: JSON.stringify({
    code: authorizationCode,
    redirectUri: 'https://extension-callback',
    clientId: 'zkp-wallet-extension'
  })
});

const tokens = await response.json();
console.log('Access token:', tokens.access_token);
console.log('ID token:', tokens.id_token);
```

---

### GET /api/auth/userinfo

Retrieves user information using access token.

**Request:**
```http
GET /api/auth/userinfo HTTP/1.1
Authorization: Bearer <access_token>
```

**Response (200 OK):**
```json
{
  "sub": "unique-user-id",
  "name": "John Doe",
  "birthdate": "1990-01-15",
  "email": "john@example.dk",
  "email_verified": true
}
```

**Response Model:**
```typescript
interface UserInfo {
  sub: string;              // Unique user identifier
  name?: string;            // Full name
  given_name?: string;      // First name
  family_name?: string;     // Last name
  birthdate?: string;       // ISO 8601 date
  email?: string;           // Email address
  email_verified?: boolean; // Email verification status
  phone_number?: string;    // Phone number
  address?: Address;        // Address object
}

interface Address {
  street_address?: string;
  locality?: string;        // City
  postal_code?: string;
  country?: string;
}
```

**Errors:**
- `401 Unauthorized` - Invalid or expired access token
- `403 Forbidden` - Insufficient scope
- `500 Internal Server Error` - Server error

**Example (curl):**
```bash
curl https://identity-service.zkp-wallet.dev/api/auth/userinfo \
  -H "Authorization: Bearer eyJhbGciOiJSUzI1NiIs..."
```

---

## Configuration

### Client Registration

Clients must be pre-registered with scopes and redirect URIs.

```json
{
  "clientId": "zkp-wallet-extension",
  "clientSecret": "secret-for-confidential-clients",
  "redirectUris": [
    "https://extension-callback",
    "https://localhost:3000/callback"
  ],
  "allowedScopes": ["openid", "profile", "email"],
  "tokenEndpointAuthMethod": "client_secret_basic"
}
```

### Environment Variables

```bash
# MitID Configuration
MITID_ISSUER=https://mitid.dk
MITID_CLIENT_ID=your-client-id
MITID_CLIENT_SECRET=your-client-secret
MITID_REDIRECT_URI=https://your-service/callback

# Database
DATABASE_CONNECTION_STRING=Server=localhost;Database=Identity;

# Secrets
JWT_SIGNING_KEY=path/to/rsa-key.pem
```

---

## Error Codes

| Code | HTTP Status | Description |
|------|-------------|-------------|
| `invalid_request` | 400 | Malformed request |
| `invalid_client` | 401 | Unknown or invalid client |
| `invalid_grant` | 400 | Invalid authorization code |
| `unauthorized_client` | 401 | Client not authorized for grant type |
| `unsupported_grant_type` | 400 | Grant type not supported |
| `invalid_scope` | 400 | Requested scope invalid or unknown |
| `server_error` | 500 | Internal server error |

---

## Security Considerations

### PKCE (Proof Key for Code Exchange)

**Highly recommended** for browser extensions to prevent authorization code interception.

```javascript
// Generate PKCE challenge
const codeVerifier = generateRandomString(64);
const codeChallenge = await sha256(codeVerifier);
const codeChallengeMethod = 'S256';

// Include in /authorize request
const authorizeRequest = {
  // ... other fields
  codeChallenge: codeChallenge,
  codeChallengeMethod: codeChallengeMethod
};

// Include verifier in /token request
const tokenRequest = {
  // ... other fields
  codeVerifier: codeVerifier
};
```

### State Parameter

Always use `state` parameter for CSRF protection:

```javascript
const state = generateRandomString(32);
sessionStorage.setItem('oauth_state', state);

// In callback:
const receivedState = urlParams.get('state');
if (receivedState !== sessionStorage.getItem('oauth_state')) {
  throw new Error('CSRF attack detected');
}
```

### Token Storage

- **NEVER** store tokens in localStorage
- Use secure httpOnly cookies or encrypted storage
- Set appropriate token expiration

---

## Rate Limiting

- **Authorization endpoint:** 10 requests/minute per IP
- **Token endpoint:** 20 requests/minute per client
- **UserInfo endpoint:** 100 requests/minute per token

**Headers:**
```
X-RateLimit-Limit: 10
X-RateLimit-Remaining: 7
X-RateLimit-Reset: 1610000000
```

---

## Testing

### Test Credentials (Development Only)

```json
{
  "username": "test-user@example.dk",
  "password": "test-password-123",
  "cpr": "0101901234"
}
```

### Mock MitID Response

For testing without real MitID:

```bash
# Set environment variable
MITID_MOCK_MODE=true

# Mock will return predefined user data
```

---

## Support

- **Documentation:** https://docs.zkp-wallet.dev/identity-service
- **Issues:** https://github.com/zkp-wallet/identity-service/issues
- **Email:** api-support@zkp-wallet.dev

---

## Changelog

### v1.0.0 (2026-02-11)
- Initial release
- MitID integration
- OAuth 2.0 + OIDC support
- PKCE support
