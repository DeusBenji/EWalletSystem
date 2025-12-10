using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace BuildingBlocks.Contracts.Events
{
    public record MitIdVerified(
        Guid AccountId,
        bool IsAdult,
        DateTime VerifiedAt
);
}