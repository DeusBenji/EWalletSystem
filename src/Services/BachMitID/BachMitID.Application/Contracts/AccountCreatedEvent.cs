using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BachMitID.Application.Contracts
{
    public class AccountCreatedEvent
    {
        public int Id { get; set; } 
        public string Email { get; set; } = string.Empty;

    }
}
