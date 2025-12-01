using System;
using System.Collections.Generic;

namespace TokenService.Domain.Models
{
    public class AgeOver18Credential
    {
        public List<string> Context { get; set; } = new()
        {
            "https://www.w3.org/2018/credentials/v1",
            "https://bachelor-project.example/contexts/age/v1"
        };
        
        public List<string> Type { get; set; } = new()
        {
            "VerifiableCredential",
            "AgeOver18Credential"
        };
        
        public string Issuer { get; set; } = default!;
        public DateTime IssuanceDate { get; set; }
        public DateTime ExpirationDate { get; set; }
        
        public CredentialSubject CredentialSubject { get; set; } = default!;
    }
    
    public class CredentialSubject
    {
        public string Id { get; set; } = default!; // Subject DID
        public bool AgeOver18 { get; set; }
        public DateTime VerifiedAt { get; set; }
    }
}
