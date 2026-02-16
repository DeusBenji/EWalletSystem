using IdentityService.Application.Interfaces;
using IdentityService.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace IdentityService.Application.Handlers;

/// <summary>
/// Request to issue a policy credential to a user
/// </summary>
public record IssuePolicyCredentialRequest
{
    public required string PolicyId { get; init; }
    public required string SubjectId { get; init; }
    public required Dictionary<string, object> Claims { get; init; }
    public string? DeviceTag { get; init; }
    public TimeSpan? CustomExpiry { get; init; }
}

/// <summary>
/// Response containing the issued credential
/// </summary>
public record IssuePolicyCredentialResponse
{
    public required string CredentialJwt { get; init; }
    public required string CredentialHash { get; init; }
    public required DateTime IssuedAt { get; init; }
    public required DateTime ExpiresAt { get; init; }
    public required string PolicyHash { get; init; }
}

/// <summary>
/// Handler for issuing policy credentials.
/// Creates signed JWT credentials that can be used to generate ZKP proofs.
/// </summary>
public class IssuePolicyCredentialHandler
{
    private readonly IPolicyRegistry _policyRegistry;
    private readonly IKeyManagementService _keyManagement;
    private readonly ILogger<IssuePolicyCredentialHandler> _logger;

    public IssuePolicyCredentialHandler(
        IPolicyRegistry policyRegistry,
        IKeyManagementService keyManagement,
        ILogger<IssuePolicyCredentialHandler> logger)
    {
        _policyRegistry = policyRegistry;
        _keyManagement = keyManagement;
        _logger = logger;
    }

    public async Task<IssuePolicyCredentialResponse> HandleAsync(
        IssuePolicyCredentialRequest request,
        CancellationToken cancellationToken = default)
    {
        // 1. Validate policy exists and is active
        var policy = await _policyRegistry.GetPolicyAsync(
            request.PolicyId,
            version: null, // Get latest active version
            cancellationToken);

        if (policy == null)
        {
            throw new InvalidOperationException($"Policy {request.PolicyId} not found or not active");
        }

        if (policy.Status != Domain.Entities.PolicyStatus.Active)
        {
            throw new InvalidOperationException($"Policy {request.PolicyId} is not active (status: {policy.Status})");
        }

        // 2. Get current signing key
        var signingKey = await _keyManagement.GetCurrentSigningKeyAsync(cancellationToken);
        if (signingKey == null)
        {
            throw new InvalidOperationException("No current signing key available");
        }

        // 3. Calculate expiry
        var expiry = request.CustomExpiry.HasValue
            ? DateTime.UtcNow.Add(request.CustomExpiry.Value)
            : DateTime.UtcNow.Add(ParseIsoDuration(policy.DefaultExpiry));

        // 4. Create credential
        var credential = new PolicyCredential
        {
            CredentialId = Guid.NewGuid().ToString(),
            PolicyId = request.PolicyId,
            SubjectId = request.SubjectId,
            Claims = request.Claims,
            IssuedAt = DateTime.UtcNow,
            ExpiresAt = expiry,
            NotBefore = DateTime.UtcNow, // Credential valid immediately
            IssuerSigningKeyId = signingKey.KeyId,
            DeviceTag = request.DeviceTag,
            PolicyHash = policy.ComputePolicyHash()
        };

        // 5. Sign credential
        var signature = SignCredential(credential, signingKey);
        var credentialJwt = credential.ToJwt(signature);

        // 6. Log issuance (NO PII)
        _logger.LogInformation(
            "Issued credential {CredentialId} for policy {PolicyId} to subject {SubjectId} (expires {ExpiresAt})",
            credential.CredentialId,
            request.PolicyId,
            credential.SubjectId, // SubjectId is pseudonymized, OK to log
            credential.ExpiresAt);

        return new IssuePolicyCredentialResponse
        {
            CredentialJwt = credentialJwt,
            CredentialHash = credential.ComputeCredentialHash(),
            IssuedAt = credential.IssuedAt,
            ExpiresAt = credential.ExpiresAt,
            PolicyHash = credential.PolicyHash
        };
    }

    private string SignCredential(PolicyCredential credential, Domain.Entities.IssuerSigningKey key)
    {
        // TODO: Implement actual signing using private key
        // For now, return placeholder
        // In production, use ECDSA (ES256) or RSA (RS256)
        
        var dataToSign = $"{credential.CredentialId}:{credential.PolicyId}:{credential.SubjectId}";
        return Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes($"SIGNATURE[{dataToSign}]"));
    }

    private TimeSpan ParseIsoDuration(string duration)
    {
        // Simple ISO 8601 duration parser
        // PT72H -> 72 hours
        // PT24H -> 24 hours
        
        if (duration.StartsWith("PT") && duration.EndsWith("H"))
        {
            var hours = int.Parse(duration[2..^1]);
            return TimeSpan.FromHours(hours);
        }
        else if (duration.StartsWith("PT") && duration.EndsWith("M"))
        {
            var minutes = int.Parse(duration[2..^1]);
            return TimeSpan.FromMinutes(minutes);
        }
        
        throw new FormatException($"Unsupported duration format: {duration}");
    }
}
