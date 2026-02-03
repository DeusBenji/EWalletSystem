using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Models
{
    public class AccountAgeStatus
    {
        public Guid AccountId { get; set; }
        public bool IsAdult { get; set; }
        public DateTime VerifiedAt { get; set; }

        private AccountAgeStatus() { } // For Dapper

        public AccountAgeStatus(Guid accountId, bool isAdult, DateTime verifiedAt)
        {
            AccountId = accountId;
            IsAdult = isAdult;
            VerifiedAt = verifiedAt;
        }
    }
}
