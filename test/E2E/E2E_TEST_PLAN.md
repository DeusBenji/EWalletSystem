# E2E Test Plan

## Overview

End-to-end tests for the ZKP Credential Platform covering the full lifecycle from credential issuance to proof validation.

## Test Scenarios

### 1. Happy Path - Age Verification

**Flow:**
1. User visits website requiring age verification
2. Website requests proof via SDK: `ZKPWalletSDK.verifyPolicy({ policyId: 'age_over_18' })`
3. Extension checks for existing credential
4. If no credential: Initiate MitID authentication
5. IdentityService authenticates user via MitID
6. TokenService issues age credential
7. Extension stores encrypted credential
8. Extension generates ZKP proof
9. Website receives proof envelope
10. Website sends proof to ValidationService
11. ValidationService validates proof and returns success

**Expected Result:** Access granted

### 2. Version Matrix Tests

#### Test 2.1: Compatible Versions
- **Validator:** v1.0
- **Extension:** v1.2
- **SDK:** v1.0
- **Expected:** Success

#### Test 2.2: Breaking Change Rejection
- **Validator:** v1.0
- **Extension:** v2.0 (breaking changes)
- **Expected:** Graceful failure with version mismatch error

#### Test 2.3: Downgrade Protection
- **Policy minimum version:** v1.2
- **Extension attempting:** v1.1
- **Expected:** Rejection with downgrade error

### 3. Compromise Scenarios

#### Test 3.1: Panic Button
1. User activates panic button
2. All credentials wiped
3. Device secret wiped
4. Cached circuits wiped
5. Audit log created
6. User notification shown
7. User re-authenticates with MitID
8. New credential issued
9. Proof generation works with new credential

**Expected:** Complete wipe and successful recovery

#### Test 3.2: Stolen Credential (Device Binding)
1. Attacker extracts encrypted credential from storage
2. Attacker tries to use credential on different device
3. Device tag mismatch detected
4. Proof generation fails

**Expected:** Device binding prevents credential use

### 4. Cross-Policy/Domain Replay Tests

#### Test 4.1: Cross-Domain Replay
1. User generates proof for `https://site-a.com`
2. Attacker intercepts proof
3. Attacker tries to replay proof on `https://site-b.com`
4. ValidationService checks origin binding
5. Validation fails

**Expected:** Origin binding prevents cross-domain replay

#### Test 4.2: Cross-Policy Replay
1. User generates proof for `age_over_18` policy
2. Attacker tries to use proof for `drivers_license` policy
3. Public signals validation fails

**Expected:** Policy binding prevents cross-policy replay

### 5. Observability Validation

#### Test 5.1: Audit Logging
1. Perform various operations (issue, prove, panic)
2. Check audit logs for all events
3. Verify no PII in logs
4. Verify timestamps are accurate

**Expected:** Complete audit trail without PII

#### Test 5.2: Metrics Collection
1. Perform operations
2. Check ValidationService metrics
3. Verify proof validation counts
4. Verify error counts
5. Verify latency metrics

**Expected:** All operations properly tracked

## Test Environment

### Services
- **IdentityService:** `https://localhost:5001`
- **TokenService:** `https://localhost:5002`
- **ValidationService:** `https://localhost:5003`
- **PolicyRegistry:** `https://localhost:5004`

### Test Website
- **URL:** `https://localhost:8080`
- **Purpose:** Test SDK integration

### Browser Extension
- **Installed from:** `browser-extension/manifest.json`
- **Test mode:** Development mode

## Test Data

### Test Users
```json
{
  "user1": {
    "mitId": "test-user-1",
    "birthdate": "1990-01-01",
    "hasDriversLicense": true
  },
  "user2": {
    "mitId": "test-user-2",
    "birthdate": "2010-01-01",
    "hasDriversLicense": false
  }
}
```

### Test Policies
```json
{
  "age_over_18": {
    "version": "1.0.0",
    "circuit": "age_verification",
    "minimumVersion": "1.0.0"
  },
  "drivers_license": {
    "version": "1.0.0",
    "circuit": "license_verification",
    "minimumVersion": "1.0.0"
  }
}
```

## Running E2E Tests

### Setup
```bash
# Start all services
docker-compose up -d

# Build extension
cd browser-extension
npm run build

# Load extension in Chrome
# chrome://extensions -> Load unpacked -> dist/

# Start test website
cd test-website
npm run serve
```

### Execute Tests
```bash
# Run all E2E tests
npm run test:e2e

# Run specific test suite
npm run test:e2e -- --suite=version-matrix
npm run test:e2e -- --suite=compromise
npm run test:e2e -- --suite=replay

# Run with browser UI (for debugging)
npm run test:e2e:headed
```

### Generate Report
```bash
npm run test:e2e:report
```

## Success Criteria

- ✅ All happy path scenarios pass
- ✅ Version matrix tests validate compatibility
- ✅ Downgrade protection works correctly
- ✅ Panic button completely wipes data
- ✅ Device binding prevents stolen credential use
- ✅ Origin binding prevents cross-domain replay
- ✅ Policy binding prevents cross-policy replay
- ✅ Audit logs capture all events without PII
- ✅ Metrics accurately track operations
- ✅ All tests pass in CI/CD pipeline

## Test Coverage Goals

- **Backend Services:** 80%+ code coverage
- **Extension:** 75%+ code coverage
- **SDK:** 90%+ code coverage
- **E2E Scenarios:** 100% of critical paths

## Known Limitations

1. **MitID Mock:** E2E tests use mocked MitID (real MitID requires test accounts)
2. **ZKP Circuits:** Tests use simplified test circuits (not production circuits)
3. **Browser Extension:** Manual loading required (not packaged .crx)

## Maintenance

E2E tests should be run:
- On every PR
- Before every release
- After any protocol changes
- After any dependency updates
