using System;

namespace IdentityService.Domain.Entities;

/// <summary>
/// Audit log for key retirements
/// </summary>
public class KeyRetirementAudit
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public required string KeyId { get; init; }
    public required string OldStatus { get; init; }
    public required string Reason { get; init; }
    public required string Actor { get; init; }
    public DateTime RetiredAt { get; init; }
}
