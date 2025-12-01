using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BuildingBlocks.Contracts.Events
{
    public record CredentialVerified(
        Guid AccountId,
        bool IsValid,
        string? IssuerDid,
        string? FailureReason,
        DateTime VerifiedAt
    );
}
