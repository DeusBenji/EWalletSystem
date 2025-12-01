using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Models
{
    public class DidDocument
    {
        public string Id { get; set; } = default!;
        public List<string> VerificationMethods { get; set; } = new();
        public List<string> AssertionMethods { get; set; } = new();
    }
}
