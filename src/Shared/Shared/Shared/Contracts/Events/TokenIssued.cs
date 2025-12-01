using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Contracts.Events
{
    public record TokenIssued(
        Guid AttestationId,
        Guid AccountId,
        string AttestationHash,
        DateTime IssuedAt,
        DateTime ExpiresAt
        );
}
