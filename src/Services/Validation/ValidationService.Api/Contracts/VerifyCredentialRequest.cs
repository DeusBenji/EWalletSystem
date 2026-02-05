using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Api.Contracts
{
    public class VerifyCredentialRequest
    {
        public string VcJwt { get; set; } = default!;
    }
}