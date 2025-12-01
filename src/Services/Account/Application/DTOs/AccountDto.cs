using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DTOs
{
    public class AccountDto
    {
        public Guid Id { get; set; }
        public string Email { get; set; } = default!;
        public bool IsAdult { get; set; }
        public bool IsMitIdLinked { get; set; }
    }
}
