using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Models
{
    public class AgeAttestation
    {
        public Guid Id { get; private set; }
        public Guid AccountId { get; private set; }
        public string SubjectId { get; private set; } = default!;
        public bool IsAdult { get; private set; }
        public DateTime IssuedAt { get; private set; }
        public DateTime ExpiresAt { get; private set; }
        public string Token { get; private set; } = default!;
        public string Hash { get; private set; } = default!;
        public string? Commitment { get; private set; }

        private AgeAttestation() { }

        // Constructor used when READING from database
        public AgeAttestation(
            Guid id,
            Guid accountId,
            string subjectId,
            bool isAdult,
            DateTime issuedAt,
            DateTime expiresAt,
            string token,
            string hash,
            string? commitment)
        {
            Id = id;
            AccountId = accountId;
            SubjectId = subjectId;
            IsAdult = isAdult;
            IssuedAt = issuedAt;
            ExpiresAt = expiresAt;
            Token = token;
            Hash = hash;
            Commitment = commitment;
        }

        // Constructor used when CREATING a NEW attestation
        public AgeAttestation(
            Guid accountId,
            string subjectId,
            bool isAdult,
            DateTime issuedAt,
            DateTime expiresAt,
            string token,
            string hash,
            string? commitment = null)
        {
            Id = Guid.NewGuid();
            AccountId = accountId;
            SubjectId = subjectId;
            IsAdult = isAdult;
            IssuedAt = issuedAt;
            ExpiresAt = expiresAt;
            Token = token;
            Hash = hash;
            Commitment = commitment;
        }
    }
}
