using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Blockchain.DTOs
{
    internal record GoVerificationMethodResponse(
         string Id,
         string Type,
         string Controller,
         string? PublicKeyJwk,
         string? PublicKeyBase58
     );
}