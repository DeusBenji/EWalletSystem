using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Kafka
{
    public sealed class AccountRegisteredEvent
    {
        public Guid AccountId { get; init; }
        public string Email { get; init; } = default!;
    }

}
