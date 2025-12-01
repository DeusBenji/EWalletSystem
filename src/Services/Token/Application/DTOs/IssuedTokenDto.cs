using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DTOs
{
    public class IssuedTokenDto
    {
        public string Token { get; set; } = default!;
        public DateTime IssuedAt { get; set; }
        public DateTime ExpiresAt { get; set; }
    }
}
