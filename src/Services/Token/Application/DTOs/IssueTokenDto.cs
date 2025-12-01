using Application.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Application.DTOs
{
    public class IssueTokenDto
    {
        public Guid AccountId { get; set; }
        // Evt. ekstra felter senere, fx requested lifetime
    }
}
