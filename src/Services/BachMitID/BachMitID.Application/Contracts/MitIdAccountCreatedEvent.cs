using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BachMitID.Application.Contracts
{
    public class MitIdAccountCreatedEvent
    {
        public Guid Id { get; set; }
        public Guid AccountId { get; set; }
        public string SubId { get; set; } = string.Empty;
        public bool IsAdult { get; set; }

    }
}
