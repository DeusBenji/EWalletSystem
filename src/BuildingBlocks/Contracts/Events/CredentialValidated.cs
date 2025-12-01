using System;

namespace BuildingBlocks.Contracts.Events
{
    public record CredentialValidated(
        Guid AccountId,
        DateTime ValidatedAt,
        bool IsValid,
        string? FailureReason
    );
}
