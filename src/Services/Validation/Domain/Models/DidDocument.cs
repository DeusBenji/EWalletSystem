using System.Collections.Generic;

namespace Domain.Models
{
    public class DidDocument
    {
        public string Id { get; set; } = default!;

        // eksisterende (beholdes)
        public List<string> VerificationMethods { get; set; } = new();
        public List<string> AssertionMethods { get; set; } = new();

        // ✅ ny: detaljer til JWT-validering
        public List<DidVerificationMethod> VerificationMethodDetails { get; set; } = new();
    }

    public class DidVerificationMethod
    {
        public string Id { get; set; } = default!;
        public string? Type { get; set; }
        public string? Controller { get; set; }

        public string? PublicKeyJwk { get; set; }

        // ✅ her bruger vi Base58 som “multibase-holder” (minimal ændring)
        public string? PublicKeyMultibase { get; set; }
    }
}
