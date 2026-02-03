using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Blockchain.DTOs
{
    internal record GoDidDocumentResponse(
        string Id,
        List<string>? Context,
        List<GoVerificationMethodResponse>? VerificationMethod,
        List<string>? Authentication,
        List<string>? AssertionMethod,
        DateTime Created,
        DateTime Updated
    );
}
