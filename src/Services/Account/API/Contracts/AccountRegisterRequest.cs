using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AccountService.API.Contracts
{
    public class AccountRegisterRequest
        [Required]
        [EmailAddress]
        [RegularExpression(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", ErrorMessage = "Email must be a valid format (e.g. user@domain.com)")]
        [StringLength(254)]
        public string Email { get; set; } = default!;

        [Required]
        [MinLength(8, ErrorMessage = "Password must be at least 8 characters long")]
        [StringLength(100, ErrorMessage = "Password is too long")]
        public string Password { get; set; } = default!;
    }
}
