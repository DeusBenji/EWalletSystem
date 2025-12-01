using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System;

namespace Application.DTOs
{
    public class AuthenticateAccountResult
    {
        public bool Success { get; }
        public Guid? AccountId { get; }
        public string? Failure { get; }

        public AuthenticateAccountResult(bool success, Guid? accountId, string? failure)
        {
            Success = success;
            AccountId = accountId;
            Failure = failure;
        }
    }
}
