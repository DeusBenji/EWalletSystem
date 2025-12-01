using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Shared.Contracts.Events
{
    public record MitIdVerified(
        Guid AccountId,
        string MitIdSubId,
        bool IsAdult,
        DateTime VerifiedAt
);
}