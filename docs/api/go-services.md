# GoServices (Hyperledger Fabric) API Reference

## Overview

GoServices provides blockchain integration using Hyperledger Fabric for immutable credential storage and audit trail. Written in Go, it exposes chaincode functions for credential lifecycle management.

**Network:** Hyperledger Fabric v2.5  
**Language:** Go  
**Channel:** `credential-channel`  
**Chaincode:** `credential-contract`

---

## Core Concepts

### Chaincode
Smart contract functions running on Fabric peers for credential operations.

### Ledger
Immutable blockchain storing credential issuance/revocation events.

### CouchDB
State database for rich queries (indexed by policy ID, user ID, status).

---

## Chaincode Functions

### IssueCredential

Issues a new credential and stores it on the blockchain.

**Function Signature:**
```go
func (cc *CredentialContract) IssueCredential(
    ctx contractapi.TransactionContextInterface,
    credentialID string,
    userID string,
    policyID string,
    policyVersion string,
    issuedAt int64,
    expiresAt int64,
) error
```

**Parameters:**
```typescript
{
  credentialID: string;      // Unique credential identifier
  userID: string;            // User identifier (hashed, no PII)
  policyID: string;          // Policy identifier
  policyVersion: string;     // Policy version
  issuedAt: number;          // Unix timestamp
  expiresAt: number;         // Unix timestamp
}
```

**Ledger Record:**
```json
{
  "docType": "credential",
  "credentialID": "cred_abc123",
  "userID": "hash_of_user_id",
  "policyID": "age_over_18",
  "policyVersion": "1.0.0",
  "status": "active",
  "issuedAt": 1704067200,
  "expiresAt": 1735689600,
  "revokedAt": null,
  "txID": "0x123abc..."
}
```

**Example (Fabric SDK - Node.js):**
```javascript
const gateway = new Gateway();
await gateway.connect(connectionProfile, {
  wallet,
  identity: 'appUser',
  discovery: { enabled: true, asLocalhost: true }
});

const network = await gateway.getNetwork('credential-channel');
const contract = network.getContract('credential-contract');

await contract.submitTransaction(
  'IssueCredential',
  'cred_abc123',
  'user_hash_456',
  'age_over_18',
  '1.0.0',
  '1704067200',
  '1735689600'
);

console.log('Credential issued on blockchain');
```

**Example (Fabric SDK - Go):**
```go
contract := network.GetContract("credential-contract")

_, err := contract.SubmitTransaction(
    "IssueCredential",
    "cred_abc123",
    "user_hash_456",
    "age_over_18",
    "1.0.0",
    "1704067200",
    "1735689600",
)
if err != nil {
    log.Fatalf("Failed to issue credential: %v", err)
}
```

**Errors:**
- `CREDENTIAL_ALREADY_EXISTS` - Credential ID already used
- `INVALID_TIMESTAMP` - expiresAt <= issuedAt
- `INVALID_POLICY` - Policy doesn't exist

---

### QueryCredential

Retrieves a credential from the ledger.

**Function Signature:**
```go
func (cc *CredentialContract) QueryCredential(
    ctx contractapi.TransactionContextInterface,
    credentialID string,
) (*Credential, error)
```

**Parameters:**
```typescript
{
  credentialID: string;      // Credential to query
}
```

**Response:**
```json
{
  "credentialID": "cred_abc123",
  "userID": "user_hash_456",
  "policyID": "age_over_18",
  "policyVersion": "1.0.0",
  "status": "active",
  "issuedAt": 1704067200,
  "expiresAt": 1735689600,
  "revokedAt": null
}
```

**Example (Node.js):**
```javascript
const result = await contract.evaluateTransaction(
  'QueryCredential',
  'cred_abc123'
);

const credential = JSON.parse(result.toString());
console.log('Credential status:', credential.status);
```

---

### RevokeCredential

Revokes a credential (marks as revoked on ledger).

**Function Signature:**
```go
func (cc *CredentialContract) RevokeCredential(
    ctx contractapi.TransactionContextInterface,
    credentialID string,
    reason string,
) error
```

**Parameters:**
```typescript
{
  credentialID: string;      // Credential to revoke
  reason: string;            // Revocation reason
}
```

**Example (Node.js):**
```javascript
await contract.submitTransaction(
  'RevokeCredential',
  'cred_abc123',
  'user_request'
);

console.log('Credential revoked');
```

**Errors:**
- `CREDENTIAL_NOT_FOUND` - Credential doesn't exist
- `CREDENTIAL_ALREADY_REVOKED` - Already revoked
- `CREDENTIAL_EXPIRED` - Cannot revoke expired credential

---

### QueryCredentialsByUser

Queries all credentials for a specific user.

**Function Signature:**
```go
func (cc *CredentialContract) QueryCredentialsByUser(
    ctx contractapi.TransactionContextInterface,
    userID string,
) ([]*Credential, error)
```

**Parameters:**
```typescript
{
  userID: string;           // User identifier (hashed)
}
```

**Response:**
```json
[
  {
    "credentialID": "cred_abc123",
    "policyID": "age_over_18",
    "status": "active",
    "expiresAt": 1735689600
  },
  {
    "credentialID": "cred_def456",
    "policyID": "drivers_license",
    "status": "active",
    "expiresAt": 1767225600
  }
]
```

**CouchDB Query (Internal):**
```json
{
  "selector": {
    "docType": "credential",
    "userID": "user_hash_456",
    "status": "active"
  },
  "sort": [{"issuedAt": "desc"}]
}
```

---

### QueryCredentialsByPolicy

Queries credentials by policy ID (for analytics).

**Function Signature:**
```go
func (cc *CredentialContract) QueryCredentialsByPolicy(
    ctx contractapi.TransactionContextInterface,
    policyID string,
    status string,
) ([]*Credential, error)
```

**Parameters:**
```typescript
{
  policyID: string;         // Policy identifier
  status: string;           // "active" | "revoked" | "expired"
}
```

**Example (Node.js):**
```javascript
const result = await contract.evaluateTransaction(
  'QueryCredentialsByPolicy',
  'age_over_18',
  'active'
);

const credentials = JSON.parse(result.toString());
console.log(`Active age_over_18 credentials: ${credentials.length}`);
```

---

### GetCredentialHistory

Retrieves full transaction history for a credential.

**Function Signature:**
```go
func (cc *CredentialContract) GetCredentialHistory(
    ctx contractapi.TransactionContextInterface,
    credentialID string,
) ([]HistoryRecord, error)
```

**Response:**
```json
[
  {
    "txId": "0x123abc",
    "timestamp": "2026-01-01T00:00:00Z",
    "isDelete": false,
    "value": {
      "credentialID": "cred_abc123",
      "status": "active",
      "issuedAt": 1704067200
    }
  },
  {
    "txId": "0x456def",
    "timestamp": "2026-06-01T00:00:00Z",
    "isDelete": false,
    "value": {
      "credentialID": "cred_abc123",
      "status": "revoked",
      "revokedAt": 1717200000
    }
  }
]
```

**Use Case:** Audit trail for compliance.

---

## Data Models

### Credential (CouchDB Document)

```go
type Credential struct {
    DocType        string `json:"docType"`        // Always "credential"
    CredentialID   string `json:"credentialID"`
    UserID         string `json:"userID"`         // Hashed, no PII
    PolicyID       string `json:"policyID"`
    PolicyVersion  string `json:"policyVersion"`
    Status         string `json:"status"`         // active|revoked|expired
    IssuedAt       int64  `json:"issuedAt"`       // Unix timestamp
    ExpiresAt      int64  `json:"expiresAt"`      // Unix timestamp
    RevokedAt      *int64 `json:"revokedAt"`      // Unix timestamp (if revoked)
    RevocationReason string `json:"revocationReason"`
}
```

### History Record

```go
type HistoryRecord struct {
    TxID      string      `json:"txId"`
    Timestamp string      `json:"timestamp"`
    IsDelete  bool        `json:"isDelete"`
    Value     *Credential `json:"value"`
}
```

---

## Network Configuration

### Connection Profile (connection.json)

```json
{
  "name": "zkp-credential-network",
  "version": "1.0.0",
  "client": {
    "organization": "TokenServiceOrg",
    "connection": {
      "timeout": {
        "peer": {
          "endorser": "300"
        }
      }
    }
  },
  "organizations": {
    "TokenServiceOrg": {
      "mspid": "TokenServiceMSP",
      "peers": ["peer0.tokenservice.zkp-wallet.dev"],
      "certificateAuthorities": ["ca.tokenservice.zkp-wallet.dev"]
    }
  },
  "peers": {
    "peer0.tokenservice.zkp-wallet.dev": {
      "url": "grpcs://peer0.tokenservice.zkp-wallet.dev:7051",
      "tlsCACerts": {
        "path": "/path/to/tls-ca-cert.pem"
      }
    }
  },
  "channels": {
    "credential-channel": {
      "orderers": ["orderer.zkp-wallet.dev"],
      "peers": {
        "peer0.tokenservice.zkp-wallet.dev": {
          "endorsingPeer": true,
          "chaincodeQuery": true,
          "ledgerQuery": true,
          "eventSource": true
        }
      }
    }
  }
}
```

---

## CouchDB Indexes

For efficient queries, create indexes:

```json
{
  "index": {
    "fields": ["docType", "userID", "status"]
  },
  "ddoc": "indexUserCredentials",
  "name": "indexUserCredentials",
  "type": "json"
}
```

```json
{
  "index": {
    "fields": ["docType", "policyID", "status"]
  },
  "ddoc": "indexPolicyCredentials",
  "name": "indexPolicyCredentials",
  "type": "json"
}
```

---

## Endorsement Policy

```yaml
# Endorsement policy: Requires majority of org endorsements
AND('TokenServiceMSP.peer', 'ValidationServiceMSP.peer')
```

**Explanation:** Both TokenService and ValidationService orgs must endorse credential transactions.

---

## Security Considerations

### No PII on Blockchain

**CRITICAL:** Never store PII on the blockchain.

```go
// ❌ BAD - Stores PII
userID := "john.doe@example.com"

// ✅ GOOD - Stores hash
userID := sha256Hash("john.doe@example.com")
```

### Access Control

Use Fabric's **Attribute-Based Access Control (ABAC)**:

```go
// Check if caller is TokenService
clientID, err := ctx.GetClientIdentity().GetID()
if !strings.Contains(clientID, "TokenService") {
    return fmt.Errorf("unauthorized: only TokenService can issue credentials")
}
```

---

## Performance

### Benchmarks

| Operation | Throughput | Latency (p50) | Latency (p99) |
|-----------|------------|---------------|---------------|
| IssueCredential | 500 TPS | 80ms | 200ms |
| QueryCredential | 5000 TPS | 10ms | 30ms |
| RevokeCredential | 500 TPS | 80ms | 200ms |

### Optimization

```go
// Use pagination for large result sets
const pageSize = 100

bookmark := ""
for {
    results, nextBookmark, err := queryWithPagination(
        ctx, query, pageSize, bookmark
    )
    if err != nil {
        return err
    }
    
    // Process results...
    
    if nextBookmark == "" {
        break // No more results
    }
    bookmark = nextBookmark
}
```

---

## Testing

### Local Fabric Network (test-network)

```bash
# Start test network
cd fabric-samples/test-network
./network.sh up createChannel -c credential-channel

# Deploy chaincode
./network.sh deployCC -ccn credential-contract \
  -ccp ../zkp-wallet/go-services/chaincode \
  -ccl go

# Test functions
peer chaincode invoke \
  -o localhost:7050 \
  -C credential-channel \
  -n credential-contract \
  -c '{"function":"IssueCredential","Args":["cred_test","user_test","age_over_18","1.0.0","1704067200","1735689600"]}'
```

---

## Error Handling

```go
// Chaincode error handling
if credentialExists {
    return fmt.Errorf("CREDENTIAL_ALREADY_EXISTS: %s", credentialID)
}

if expiresAt <= issuedAt {
    return fmt.Errorf("INVALID_TIMESTAMP: expiresAt must be > issuedAt")
}
```

**Client error handling:**
```javascript
try {
  await contract.submitTransaction('IssueCredential', ...);
} catch (error) {
  if (error.message.includes('CREDENTIAL_ALREADY_EXISTS')) {
    console.error('Credential already issued');
  } else {
    throw error;
  }
}
```

---

## Monitoring

### Fabric Metrics (Prometheus)

```yaml
# Expose metrics
CORE_METRICS_PROVIDER=prometheus
CORE_OPERATIONS_LISTENADDRESS=0.0.0.0:9443
```

**Key Metrics:**
- `chaincode_execute_timeouts`
- `ledger_blockchain_height`
- `gossip_membership_total_peers_known`

---

## Support

- **Fabric Docs:** https://hyperledger-fabric.readthedocs.io
- **Chaincode Source:** https://github.com/zkp-wallet/go-services
- **Email:** fabric-support@zkp-wallet.dev

---

## Changelog

### v1.0.0 (2026-02-11)
- Initial chaincode deployment
- Credential issuance/revocation
- CouchDB rich queries
- History tracking
