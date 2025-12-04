using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BuildingBlocks.Contracts.Messaging
{
    public static class Topics
    {
        public const string AccountCreated = "account-created";
        public const string MitIdVerified = "mitid-verified";
        public const string TokenIssued = "token-issued";
        public const string CredentialValidated = "credential-validated";
        public const string ValidationFailed = "validation-failed";
    }
}
