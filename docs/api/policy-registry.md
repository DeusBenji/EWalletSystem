# PolicyRegistry API Reference

## Overview

The PolicyRegistry manages credential verification policies, including versioning, lifecycle states, and required claims.

**Base URL:** `https://policy-registry.zkp-wallet.dev/api`  
**Version:** v1.0  
**Protocol:** REST (JSON)

---

## Core Concepts

### Policy
Defines what needs to be proven (e.g., "user is over 18") and what claims are required from the identity provider.

**Example Policy:**
```json
{
  "policyId": "age_over_18",
  "name": "Age Verification (18+)",
  "description": "Proves user is 18 or older without revealing exact birthdate",
  "version": "1.0.0",
  "status": "active",
  "circuit": "age_verification",
  "requiredClaims": ["birthdate"],
  "publicOutputs": ["isOver18"],
  "minimumVersion": "1.0.0",
  "createdAt": "2026-01-01T00:00:00Z",
  "publishedAt": "2026-01-01T00:00:00Z"
}
```

---

## API Endpoints

### GET /api/policies

Lists all available policies.

**Request:**
```http
GET /api/policies HTTP/1.1
```

**Query Parameters:**
- `status` (optional): Filter by status (`active`, `deprecated`, `sunset`)
- `page` (optional): Page number (default: 1)
- `pageSize` (optional): Results per page (default: 50, max: 100)

**Response (200 OK):**
```json
{
  "policies": [
    {
      "policyId": "age_over_18",
      "name": "Age Verification (18+)",
      "description": "Proves user is 18 or older",
      "latestVersion": "1.2.0",
      "status": "active",
      "circuitType": "age_verification"
    },
    {
      "policyId": "drivers_license",
      "name": "Driver's License Verification",
      "description": "Proves valid driver's license",
      "latestVersion": "1.0.0",
      "status": "active",
      "circuitType": "license_verification"
    }
  ],
  "pagination": {
    "page": 1,
    "pageSize": 50,
    "totalCount": 2,
    "totalPages": 1
  }
}
```

**Response Model:**
```typescript
interface PolicyListResponse {
  policies: PolicySummary[];
  pagination: PaginationInfo;
}

interface PolicySummary {
  policyId: string;
  name: string;
  description: string;
  latestVersion: string;
  status: PolicyStatus;
  circuitType: string;
}

type PolicyStatus = 'active' | 'deprecated' | 'sunset';
```

---

### GET /api/policies/{policyId}

Gets detailed information about a specific policy (latest version).

**Request:**
```http
GET /api/policies/age_over_18 HTTP/1.1
```

**Response (200 OK):**
```json
{
  "policyId": "age_over_18",
  "name": "Age Verification (18+)",
  "description": "Proves user is 18 or older without revealing exact birthdate",
  "version": "1.2.0",
  "status": "active",
  "circuit": "age_verification",
  "circuitVersion": "1.2.0",
  "requiredClaims": ["birthdate"],
  "publicOutputs": ["isOver18"],
  "minimumVersion": "1.0.0",
  "createdAt": "2026-01-01T00:00:00Z",
  "publishedAt": "2026-02-01T00:00:00Z",
  "versionHistory": [
    {
      "version": "1.0.0",
      "status": "active",
      "publishedAt": "2026-01-01T00:00:00Z",
      "changes": "Initial release"
    },
    {
      "version": "1.1.0",
      "status": "active",
      "publishedAt": "2026-01-15T00:00:00Z",
      "changes": "Improved circuit efficiency"
    },
    {
      "version": "1.2.0",
      "status": "active",
      "publishedAt": "2026-02-01T00:00:00Z",
      "changes": "Added additional age brackets"
    }
  ]
}
```

**Response Model:**
```typescript
interface PolicyDetail {
  policyId: string;
  name: string;
  description: string;
  version: string;              // Latest version
  status: PolicyStatus;
  circuit: string;              // Circuit identifier
  circuitVersion: string;       // Circuit version
  requiredClaims: string[];     // Claims needed from identity provider
  publicOutputs: string[];      // ZK proof public outputs
  minimumVersion: string;       // Minimum accepted version (anti-downgrade)
  createdAt: string;            // ISO 8601
  publishedAt: string;          // ISO 8601
  deprecatedAt?: string;        // ISO 8601 (if deprecated)
  sunsetAt?: string;            // ISO 8601 (if sunset)
  versionHistory: PolicyVersionInfo[];
}

interface PolicyVersionInfo {
  version: string;
  status: PolicyStatus;
  publishedAt: string;
  changes: string;              // Changelog
}
```

---

### GET /api/policies/{policyId}/versions/{version}

Gets a specific version of a policy.

**Request:**
```http
GET /api/policies/age_over_18/versions/1.0.0 HTTP/1.1
```

**Response:** Same structure as GET /api/policies/{policyId}, but for the specific version.

---

### POST /api/policies (Admin Only)

Creates a new policy.

**Request:**
```http
POST /api/policies HTTP/1.1
Authorization: Bearer <admin_token>
Content-Type: application/json

{
  "policyId": "eu_citizenship",
  "name": "EU Citizenship Verification",
  "description": "Proves EU citizenship without revealing country",
  "version": "1.0.0",
  "circuit": "citizenship_verification",
  "circuitVersion": "1.0.0",
  "requiredClaims": ["nationality"],
  "publicOutputs": ["isEUCitizen"]
}
```

**Request Model:**
```typescript
interface CreatePolicyRequest {
  policyId: string;             // Unique identifier (URL-safe)
  name: string;                 // Display name
  description: string;          // User-facing description
  version: string;              // Semantic version (must be 1.0.0 initially)
  circuit: string;              // Circuit identifier
  circuitVersion: string;       // Circuit version
  requiredClaims: string[];     // Claims from identity provider
  publicOutputs: string[];      // ZK proof public outputs
}
```

**Response (201 Created):**
```json
{
  "policyId": "eu_citizenship",
  "version": "1.0.0",
  "status": "active",
  "publishedAt": "2026-02-11T16:00:00Z"
}
```

---

### PUT /api/policies/{policyId}/versions (Admin Only)

Publishes a new version of an existing policy.

**Request:**
```http
PUT /api/policies/age_over_18/versions HTTP/1.1
Authorization: Bearer <admin_token>
Content-Type: application/json

{
  "version": "1.3.0",
  "circuitVersion": "1.3.0",
  "changes": "Added support for age 21+ verification",
  "breakingChange": false
}
```

**Request Model:**
```typescript
interface PublishPolicyVersionRequest {
  version: string;              // New semantic version
  circuitVersion: string;       // Circuit version to use
  changes: string;              // Changelog
  breakingChange: boolean;      // If true, major version MUST increment
}
```

**Versioning Rules:**
- **Patch (1.0.X):** Bug fixes, optimizations
- **Minor (1.X.0):** New features, backwards compatible
- **Major (X.0.0):** Breaking changes

---

### PATCH /api/policies/{policyId}/status (Admin Only)

Updates policy lifecycle status.

**Request:**
```http
PATCH /api/policies/age_over_18/status HTTP/1.1
Authorization: Bearer <admin_token>
Content-Type: application/json

{
  "status": "deprecated",
  "reason": "Replaced by age_over_18_v2"
}
```

**Request Model:**
```typescript
interface UpdatePolicyStatusRequest {
  status: 'active' | 'deprecated' | 'sunset';
  reason: string;
  effectiveDate?: string;       // ISO 8601 (if scheduled)
}
```

**Lifecycle:**
1. **Active:** Policy is available for use
2. **Deprecated:** Policy still works but discouraged (show warning)
3. **Sunset:** Policy will be removed (grace period: 90 days)

---

## Policy Schema

### Complete Policy Definition

```typescript
interface Policy {
  // Identity
  policyId: string;             // Unique identifier
  name: string;                 // Display name
  description: string;          // User-facing description
  
  // Versioning
  version: string;              // Semantic version
  minimumVersion: string;       // Anti-downgrade protection
  
  // Circuit
  circuit: string;              // Circuit identifier
  circuitVersion: string;       // Circuit version
  
  // Claims
  requiredClaims: string[];     // Claims from identity provider
  publicOutputs: string[];      // ZK proof public outputs
  
  // Lifecycle
  status: PolicyStatus;
  createdAt: string;            // ISO 8601
  publishedAt: string;          // ISO 8601
  deprecatedAt?: string;        // ISO 8601
  sunsetAt?: string;            // ISO 8601
  
  // Metadata
  tags?: string[];              // For categorization
  documentation?: string;       // URL to policy docs
}
```

---

## Examples

### Check Minimum Version

```typescript
// Extension checks before generating proof
const policyResponse = await fetch(
  `https://policy-registry.../api/policies/${policyId}`
);
const policy = await policyResponse.json();

if (currentPolicyVersion < policy.minimumVersion) {
  throw new Error(
    `Policy version ${currentPolicyVersion} is below minimum ${policy.minimumVersion}. Please update.`
  );
}
```

### List All Active Policies

```bash
curl "https://policy-registry.zkp-wallet.dev/api/policies?status=active"
```

---

## Configuration

```bash
# Database
DATABASE_CONNECTION_STRING=Server=localhost;Database=PolicyRegistry;

# Admin Authentication
ADMIN_API_KEY=secret-admin-key

# Caching
REDIS_CONNECTION_STRING=localhost:6379
CACHE_TTL_SECONDS=3600
```

---

## Rate Limiting

- **Public endpoints** (GET): Unlimited (cached)
- **Admin endpoints** (POST/PUT/PATCH): 10 requests/minute

---

## Error Codes

| Code | HTTP Status | Description |
|------|-------------|-------------|
| `POLICY_NOT_FOUND` | 404 | Policy doesn't exist |
| `VERSION_NOT_FOUND` | 404 | Policy version doesn't exist |
| `INVALID_VERSION` | 400 | Version format invalid |
| `BREAKING_CHANGE_REQUIRES_MAJOR_VERSION` | 400 | Breaking change needs major version increment |
| `UNAUTHORIZED` | 401 | Missing/invalid admin token |

---

## Support

- **Documentation:** https://docs.zkp-wallet.dev/policy-registry
- **Issues:** https://github.com/zkp-wallet/policy-registry/issues
- **Email:** api-support@zkp-wallet.dev

---

## Changelog

### v1.0.0 (2026-02-11)
- Initial release
- Policy CRUD operations
- Versioning support
- Lifecycle management
