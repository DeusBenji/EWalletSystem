using Application.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Application.DTOs
{
    public record IssueTokenDto
    {
        public Guid AccountId { get; init; }
        public string? Commitment { get; init; }
        // Evt. ekstra felter senere, fx requested lifetime
    }
}
