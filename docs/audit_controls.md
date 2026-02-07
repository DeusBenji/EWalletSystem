# Privacy & Security Control Summary (Audit Ready)

> [!IMPORTANT]
> **Executive Summary**: IdentityService is architected as a **Privacy-First Age Verification Gate**. It relies on data minimization strategies that make it **structurally impossible** to persist sensitive identity data (CPR, National ID) in the persistence layer.

## 1. Data Inventory

We classify data into **Verification Metadata** (Stored) and **Transient Identity Data** (Processed but NOT Stored).

| Field | Storage | Classification | Purpose |
|-------|---------|----------------|---------|
| `ProviderId` | **Stored** | Metadata | Routing & Audit |
| `SubjectId` | **Stored** | Pseudonym | Account matching (Signicat pairwise ID) |
| `IsAdult` | **Stored** | Result | Age gating functionality |
| `VerifiedAt` | **Stored** | Audit | Timestamp of verification |
| `AssuranceLevel` | **Stored** | Security | Trust level (Substantial/High) |
| `ExpiresAt` | **Stored** | Policy | Re-verification trigger |
| `CreatedAt` | **Stored** | Audit | Record lifecycle |
| `CPR / CPR Number` | **NEVER STORED** | **Restricted** | Age calculation (transient only) |
| `DateOfBirth` | **NEVER STORED** | **Restricted** | Age calculation (transient only) |
| `Name / Address` | **NEVER STORED** | **Restricted** | Not requested / Not processed |

## 2. Purpose Limitation

*   **Primary Purpose**: Age Verification ("Are you 18+?").
*   **Secondary Use**: None. Data is not used for marketing, analytics, or user tracking beyond the specific service access gate.
*   **Enforcement**:
    *   DTOs (`AgeVerificationDto`) strictly exclude PII fields.
    *   Database Schema (`AgeVerifications`) has no columns for personal data.

## 3. Retention & Lifecycle

*   **Policy**: Verification records are retained to maintain proof of compliance (e.g., "User X verified on Date Y").
*   **Expiration**: Records may carry an `ExpiresAt` timestamp.
*   **Right to be Forgotten**: Deletion of a `SubjectId` record effectively anonymizes the audit trail, as `SubjectId` is a provider-specific pseudonym not linkable to a physical person without the Identity Provider's cooperation.

## 4. Access Controls

| Access Type | Actor | Mechanism |
|-------------|-------|-----------|
| **Write** | IdentityService | Service Account (Internal) |
| **Read (Self)** | User (via API) | Session-bound Access Token |
| **Read (Admin)** | Operator | Role-Based Access Control (RBAC) - `OperatorAccess` |
| **Database** | Application User | Least-privilege SQL User |

## 5. Logging Controls

**Threat**: Accidental logging of sensitive data (CPR, Session JSON) during debugging.

**Controls**:
1.  **SafeLogger Pattern**: All sensitive services use `ISafeLogger<T>` which explicitly rejects complex objects.
2.  **PII Redaction Service**: Intercepts all log messages and masks patterns matching CPR, SSN, and PII keys (`dateOfBirth`).
3.  **Hard Compilation Gates**: CI pipeline fails if `ILogger` is used directly in sensitive namespaces.
4.  **No Raw HTTP Logs**: `SignicatHttpClient` masks response bodies before logging errors.

## 6. Audit Trail

Every verification attempt generates a sanitized audit log entry:
```json
{
  "event": "AgeVerificationSuccess",
  "provider": "mitid",
  "subjectId": "pairwise-uuid-masked",
  "timestamp": "2023-10-27T10:00:00Z",
  "assurance": "substantial"
}
```
*No PII is present in these logs.*
