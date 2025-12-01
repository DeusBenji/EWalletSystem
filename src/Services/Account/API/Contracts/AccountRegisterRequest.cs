using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AccountService.API.Contracts
{
    public class AccountRegisterRequest
    {
      
        public string Email { get; set; } = default!;

            
        public string Password { get; set; } = default!;
    }
}
