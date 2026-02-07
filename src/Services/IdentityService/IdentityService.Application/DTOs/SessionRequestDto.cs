using System;
using System.Collections.Generic;

namespace IdentityService.Application.DTOs;

/// <summary>
/// Minimal request DTO for creating a Signicat session.
/// Privacy-First: Contains ONLY fields necessary for age verification flow.
/// </summary>
public sealed record SessionRequestDto(
    string Flow,
    CallbackUrls CallbackUrls,
    IReadOnlyList<string> AllowedProviders,
    IReadOnlyList<string> RequestedAttributes,
    int? SessionLifetime = null,
    string? Language = null,
    string? ExternalReference = null
);
