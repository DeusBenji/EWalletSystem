using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Api.Contracts
{
    public class VerifyCredentialResponse
    {
        public bool Success { get; set; }
        public string? FailureReason { get; set; }
        public DateTime VerifiedAt { get; set; }
        public string? IssuerDid { get; set; }
    }
}
