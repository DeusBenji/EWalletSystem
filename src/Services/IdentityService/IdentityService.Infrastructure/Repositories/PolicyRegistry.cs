using IdentityService.Application.Interfaces;
using IdentityService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace IdentityService.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for managing policy definitions.
/// Includes audit logging for all policy status changes.
/// </summary>
public class PolicyRegistry : IPolicyRegistry
{
    private readonly IdentityDbContext _context;
    private readonly IPolicySigningService _signingService;
    private readonly ILogger<PolicyRegistry> _logger;

    public PolicyRegistry(
        IdentityDbContext context,
        IPolicySigningService signingService,
        ILogger<PolicyRegistry> logger)
    {
        _context = context;
        _signingService = signingService;
        _logger = logger;
    }

    public async Task<PolicyDefinition?> GetPolicyAsync(
        string policyId,
        string? version = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.PolicyDefinitions
            .Where(p => p.PolicyId == policyId);

        if (!string.IsNullOrEmpty(version))
        {
            query = query.Where(p => p.Version == version);
        }
        else
        {
            // Get latest active version
            query = query
                .Where(p => p.Status == PolicyStatus.Active)
                .OrderByDescending(p => p.Version);
        }

        return await query.FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<PolicyDefinition>> GetActivePoliciesAsync(
        CancellationToken cancellationToken = default)
    {
        return await _context.PolicyDefinitions
            .Where(p => p.Status == PolicyStatus.Active)
            .OrderBy(p => p.PolicyId)
            .ThenByDescending(p => p.Version)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<PolicyDefinition>> GetPolicyVersionsAsync(
        string policyId,
        CancellationToken cancellationToken = default)
    {
        return await _context.PolicyDefinitions
            .Where(p => p.PolicyId == policyId)
            .OrderByDescending(p => p.Version)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> IsCompatibleVersionAsync(
        string policyId,
        string version,
        string compatibleRange,
        CancellationToken cancellationToken = default)
    {
        // Simple semver compatibility check
        // For production, use a proper semver library like Semver.Net
        
        if (compatibleRange.StartsWith("^"))
        {
            // Caret range: ^1.2.0 means >=1.2.0 <2.0.0
            var baseVersion = compatibleRange.TrimStart('^');
            var baseMajor = int.Parse(baseVersion.Split('.')[0]);
            var versionMajor = int.Parse(version.Split('.')[0]);
            
            return versionMajor == baseMajor;
        }
        else if (compatibleRange.Contains('x'))
        {
            // Wildcard range: 1.x means >=1.0.0 <2.0.0
            var parts = compatibleRange.Split('.');
            var versionParts = version.Split('.');
            
            for (int i = 0; i < parts.Length; i++)
            {
                if (parts[i] == "x") break;
                if (versionParts[i] != parts[i]) return false;
            }
            
            return true;
        }
        else
        {
            // Exact match
            return version == compatibleRange;
        }
    }

    public async Task<PolicyDefinition> CreatePolicyAsync(
        PolicyDefinition policy,
        CancellationToken cancellationToken = default)
    {
        // Check for duplicate
        var existing = await GetPolicyAsync(policy.PolicyId, policy.Version, cancellationToken);
        if (existing != null)
        {
            throw new InvalidOperationException(
                $"Policy {policy.PolicyId} version {policy.Version} already exists");
        }

        _context.PolicyDefinitions.Add(policy);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Created policy {PolicyId} version {Version}",
            policy.PolicyId,
            policy.Version);

        return policy;
    }

    public async Task<PolicyDefinition> UpdatePolicyStatusAsync(
        string policyId,
        string version,
        PolicyStatus newStatus,
        string reason,
        string actor,
        CancellationToken cancellationToken = default)
    {
        var policy = await GetPolicyAsync(policyId, version, cancellationToken);
        if (policy == null)
        {
            throw new InvalidOperationException(
                $"Policy {policyId} version {version} not found");
        }

        var oldStatus = policy.Status;
        policy.Status = newStatus;
        policy.UpdatedAt = DateTime.UtcNow;

        if (newStatus == PolicyStatus.Deprecated)
        {
            policy.DeprecatedAt = DateTime.UtcNow;
        }

        // Log audit trail (signed for immutability)
        await LogPolicyStatusChangeAsync(
            policy,
            oldStatus,
            newStatus,
            reason,
            actor,
            cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogWarning(
            "Policy {PolicyId} version {Version} status changed from {OldStatus} to {NewStatus} by {Actor}. Reason: {Reason}",
            policyId,
            version,
            oldStatus,
            newStatus,
            actor,
            reason);

        return policy;
    }

    public async Task<PolicyDefinition> SignPolicyAsync(
        PolicyDefinition policy,
        CancellationToken cancellationToken = default)
    {
        var signature = await _signingService.SignPolicyAsync(policy, cancellationToken);
        policy.Signature = signature;
        
        _context.PolicyDefinitions.Update(policy);
        await _context.SaveChangesAsync(cancellationToken);

        return policy;
    }

    public async Task<bool> VerifyPolicySignatureAsync(
        PolicyDefinition policy,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(policy.Signature))
        {
            return false;
        }

        return await _signingService.VerifyPolicySignatureAsync(policy, cancellationToken);
    }

    private async Task LogPolicyStatusChangeAsync(
        PolicyDefinition policy,
        PolicyStatus oldStatus,
        PolicyStatus newStatus,
        string reason,
        string actor,
        CancellationToken cancellationToken)
    {
        var auditEntry = new PolicyAuditLog
        {
            PolicyId = policy.PolicyId,
            Version = policy.Version,
            OldStatus = oldStatus.ToString(),
            NewStatus = newStatus.ToString(),
            Reason = reason,
            Actor = actor,
            Timestamp = DateTime.UtcNow
        };

        // Sign audit entry for immutability
        var auditData = JsonSerializer.Serialize(new
        {
            auditEntry.PolicyId,
            auditEntry.Version,
            auditEntry.OldStatus,
            auditEntry.NewStatus,
            auditEntry.Reason,
            auditEntry.Actor,
            auditEntry.Timestamp
        });

        auditEntry.Signature = await _signingService.SignDataAsync(auditData, cancellationToken);

        _context.PolicyAuditLogs.Add(auditEntry);
    }
}

/// <summary>
/// Audit log for policy status changes.
/// Entries are signed to ensure immutability.
/// </summary>
public class PolicyAuditLog
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public required string PolicyId { get; init; }
    public required string Version { get; init; }
    public required string OldStatus { get; init; }
    public required string NewStatus { get; init; }
    public required string Reason { get; init; }
    public required string Actor { get; init; }
    public DateTime Timestamp { get; init; }
    public string? Signature { get; set; }
}
