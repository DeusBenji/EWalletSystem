using AccountService.API.Contracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace AccountService.API.Contracts
{
    public class AccountResponse
    {
        public Guid Id { get; set; }
        public string Email { get; set; } = default!;

    }
}