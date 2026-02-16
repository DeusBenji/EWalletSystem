using IdentityService.Application.Interfaces;
using IdentityService.Domain.Entities;
using IdentityService.Domain.ValueObjects;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace IdentityService.Infrastructure.Services;

/// <summary>
/// Implementation of credential validation with clock skew tolerance and key retirement checks.
/// Enforces security invariants from docs/security-invariants.md
/// </summary>
public class CredentialValidationService : ICredentialValidationService
{
    private readonly IKeyManagementService _keyManagement;
    private readonly IPolicyRegistry _policyRegistry;
    private readonly ILogger<CredentialValidationService> _logger;

    /// <summary>
    /// Maximum allowed clock skew in minutes (±5 minutes)
    /// </summary>
    private const int MaxClockSkewMinutes = 5;

    public CredentialValidationService(
        IKeyManagementService keyManagement,
        IPolicyRegistry policyRegistry,
        ILogger<CredentialValidationService> logger)
    {
        _keyManagement = keyManagement;
        _policyRegistry = policyRegistry;
        _logger = logger;
    }

    public async Task<CredentialValidationResult> ValidateCredentialAsync(
        string credentialJwt,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // 1. Parse JWT
            var (header, payload) = ParseJwt(credentialJwt);
            if (header == null || payload == null)
            {
                return Fail(ValidationReasonCode.MalformedJwt, "Invalid JWT format");
            }

            // 2. Extract key ID
            if (!header.TryGetProperty("kid", out var kidElement))
            {
                return Fail(ValidationReasonCode.MissingRequiredClaim, "Missing 'kid' in header");
            }
            var keyId = kidElement.GetString()!;

            // 3. Check key status (CRITICAL: Retired keys immediately invalid)
            var signingKey = await _keyManagement.GetKeyByIdAsync(keyId, cancellationToken);
            if (signingKey == null)
            {
                _logger.LogWarning("Credential uses unknown key {KeyId}", keyId);
                return Fail(ValidationReasonCode.UnknownKey, $"Unknown key: {keyId}");
            }

            if (signingKey.Status == KeyStatus.Retired)
            {
                _logger.LogCritical(
                    "Credential from RETIRED key {KeyId} rejected (Security invariant enforced)",
                    keyId);
                return Fail(ValidationReasonCode.RetiredKeyUsed, $"Key {keyId} is retired");
            }

            // 4. Verify signature (placeholder - TODO: implement actual verification)
            var isValidSignature = VerifySignature(credentialJwt, signingKey);
            if (!isValidSignature)
            {
                _logger.LogWarning("Invalid signature for credential with key {KeyId}", keyId);
                return Fail(ValidationReasonCode.InvalidSignature, "Signature verification failed");
            }

            // 5. Extract timestamps
            if (!payload.TryGetProperty("iat", out var iatElement) ||
                !payload.TryGetProperty("exp", out var expElement))
            {
                return Fail(ValidationReasonCode.MissingRequiredClaim, "Missing iat/exp");
            }

            var issuedAt = DateTimeOffset.FromUnixTimeSeconds(iatElement.GetInt64()).UtcDateTime;
            var expiresAt = DateTimeOffset.FromUnixTimeSeconds(expElement.GetInt64()).UtcDateTime;
            
            DateTime? notBefore = null;
            if (payload.TryGetProperty("nbf", out var nbfElement))
            {
                notBefore = DateTimeOffset.FromUnixTimeSeconds(nbfElement.GetInt64()).UtcDateTime;
            }

            // 6. Clock skew validation (±5 minutes)
            var now = DateTime.UtcNow;
            var skew = Math.Abs((now - issuedAt).TotalMinutes);
            if (skew > MaxClockSkewMinutes)
            {
                _logger.LogWarning(
                    "Clock skew exceeded: {Skew} minutes (limit: {Limit})",
                    skew,
                    MaxClockSkewMinutes);
                return Fail(
                    ValidationReasonCode.ClockSkewExceeded,
                    $"Clock skew {skew:F1} min exceeds {MaxClockSkewMinutes} min limit");
            }

            // 7. Not-before validation
            if (notBefore.HasValue && now < notBefore.Value)
            {
                return Fail(
                    ValidationReasonCode.NotYetValid,
                    $"Credential not valid until {notBefore.Value:yyyy-MM-dd HH:mm:ss} UTC");
            }

            // 8. Expiry validation
            if (now >= expiresAt)
            {
                return Fail(
                    ValidationReasonCode.Expired,
                    $"Credential expired at {expiresAt:yyyy-MM-dd HH:mm:ss} UTC");
            }

            // 9. Build credential object
            var credential = new PolicyCredential
            {
                CredentialId = payload.GetProperty("jti").GetString()!,
                PolicyId = payload.GetProperty("policyId").GetString()!,
                SubjectId = payload.GetProperty("sub").GetString()!,
                Claims = JsonSerializer.Deserialize<Dictionary<string, object>>(
                    payload.GetProperty("claims").GetRawText())!,
                IssuedAt = issuedAt,
                ExpiresAt = expiresAt,
                NotBefore = notBefore,
                IssuerSigningKeyId = keyId,
                DeviceTag = payload.TryGetProperty("deviceTag", out var dtElement) 
                    ? dtElement.GetString() 
                    : null,
                PolicyHash = payload.GetProperty("policyHash").GetString()!
            };

            _logger.LogInformation(
                "Credential {CredentialId} validated successfully (key {KeyId}, expires {ExpiresAt})",
                credential.CredentialId,
                keyId,
                expiresAt);

            return new CredentialValidationResult
            {
                Valid = true,
                ReasonCode = ValidationReasonCode.Valid,
                Credential = credential
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception during credential validation");
            return Fail(ValidationReasonCode.MalformedJwt, $"Validation error: {ex.Message}");
        }
    }

    private (JsonDocument? header, JsonElement? payload) ParseJwt(string jwt)
    {
        try
        {
            var parts = jwt.Split('.');
            if (parts.Length != 3)
            {
                return (null, null);
            }

            var headerJson = DecodeBase64Url(parts[0]);
            var payloadJson = DecodeBase64Url(parts[1]);

            var header = JsonDocument.Parse(headerJson);
            var payload = JsonDocument.Parse(payloadJson);

            return (header, payload.RootElement);
        }
        catch
        {
            return (null, null);
        }
    }

    private string DecodeBase64Url(string base64Url)
    {
        var base64 = base64Url.Replace('-', '+').Replace('_', '/');
        var padding = (4 - base64.Length % 4) % 4;
        base64 += new string('=', padding);
        
        var bytes = Convert.FromBase64String(base64);
        return System.Text.Encoding.UTF8.GetString(bytes);
    }

    private bool VerifySignature(string jwt, IssuerSigningKey key)
    {
        // TODO: Implement actual signature verification using public key
        // For now, return true (placeholder)
        // In production, use ECDSA or RSA verification
        return true;
    }

    private CredentialValidationResult Fail(ValidationReasonCode reasonCode, string message)
    {
        return new CredentialValidationResult
        {
            Valid = false,
            ReasonCode = reasonCode,
            Message = message
        };
    }
}
