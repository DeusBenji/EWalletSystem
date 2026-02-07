<#
.SYNOPSIS
    EWalletSystem IdentityService E2E Verification Script
    
.DESCRIPTION
    Verifies critical endpoints and flows for the IdentityService.
    Usage: ./e2e_verify.ps1 -BaseUrl "https://localhost:5001"
#>

param (
    [string]$BaseUrl = "https://localhost:5001"
)

$ErrorActionPreference = "Stop"

function Assert-Status {
    param(
        [int]$Actual,
        [int]$Expected,
        [string]$Context
    )
    if ($Actual -eq $Expected) {
        Write-Host "[OK] $Context (Status: $Actual)" -ForegroundColor Green
    } else {
        Write-Host "[FAIL] $Context (Expected: $Expected, Got: $Actual)" -ForegroundColor Red
    }
}

Write-Host "Starting E2E Verification against $BaseUrl..."
Write-Host "--------------------------------------------"

# 1. Health Check
try {
    $resp = Invoke-WebRequest -Uri "$BaseUrl/health" -Method Get -SkipCertificateCheck
    Assert-Status $resp.StatusCode 200 "Health Check"
} catch {
    Write-Host "[FAIL] Health Check failed: $_" -ForegroundColor Red
}

# 2. Auth Start (Happy Path Sanity)
$providers = @("mitid", "sbid", "nbid")
foreach ($p in $providers) {
    try {
        $resp = Invoke-RestMethod -Uri "$BaseUrl/api/auth/$p/start" -Method Post -SkipCertificateCheck
        if ($resp.sessionId -and $resp.authUrl) {
            Write-Host "[OK] Start Auth ($p) -> Session: $($resp.sessionId)" -ForegroundColor Green
        } else {
            Write-Host "[FAIL] Start Auth ($p) -> Invalid Response" -ForegroundColor Red
        }
    } catch {
        Write-Host "[FAIL] Start Auth ($p): $_" -ForegroundColor Red
    }
}

# 3. Security Checks (Negative Tests)

# 3.1 Invalid Session Status
try {
    Invoke-WebRequest -Uri "$BaseUrl/api/auth/session/INVALID-SESSION-ID/status" -Method Get -SkipCertificateCheck
    Write-Host "[FAIL] Status check with invalid ID should fail" -ForegroundColor Red
} catch {
    # Expecting 400 or 404
    $code = $_.Exception.Response.StatusCode.value__
    if ($code -ge 400) {
        Write-Host "[OK] Invalid Session Status -> Rejected ($code)" -ForegroundColor Green
    } else {
        Write-Host "[FAIL] Invalid Session Status -> Unexpected Code ($code)" -ForegroundColor Red
    }
}

# 3.2 Callback without Session
foreach ($p in $providers) {
    try {
        Invoke-WebRequest -Uri "$BaseUrl/api/auth/$p/callback" -Method Get -SkipCertificateCheck
        Write-Host "[FAIL] Callback without sessionId should fail ($p)" -ForegroundColor Red
    } catch {
         $code = $_.Exception.Response.StatusCode.value__
         if ($code -ge 400) {
            Write-Host "[OK] Callback missing params ($p) -> Rejected ($code)" -ForegroundColor Green
         }
    }
}

Write-Host "--------------------------------------------"
Write-Host "E2E Verification Complete."
