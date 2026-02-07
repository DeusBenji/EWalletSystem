using System;

namespace IdentityService.Application.DTOs;

/// <summary>
/// Minimal response DTO for Signicat session data.
/// Privacy-First: Maps ONLY non-sensitive fields and strict subject identifiers.
/// PII FIREWALL: Does NOT map extensive personal data fields (name, address, etc.)
/// </summary>
public sealed record SessionDataDto(
    string? Id,
    string? Status,
    string? Provider,
    string? AuthenticationUrl,
    string? Loa,
    SubjectDto? Subject,
    DateTimeOffset? ExpiresAt
);

public sealed record SubjectDto(
    string? Id,
    string? DateOfBirth
);
