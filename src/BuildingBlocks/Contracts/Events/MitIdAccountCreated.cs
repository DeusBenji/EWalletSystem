using System;

namespace BuildingBlocks.Contracts.Events
{
    public record MitIdAccountCreated(
        Guid Id,
        Guid AccountId,
        string SubId,
        bool IsAdult,
        DateTime CreatedAt
    );
}
