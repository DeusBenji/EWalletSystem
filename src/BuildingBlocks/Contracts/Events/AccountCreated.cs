using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BuildingBlocks.Contracts.Events
{
    public record AccountCreated(
        Guid AccountId,
        string Email,
        DateTime CreatedAt
);
}