using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Models
{
    /// <summary>
    /// Represents a user account in the system.
    /// This is a Domain Entity that encapsulates core business logic and state.
    /// </summary>
    public class Account
    {
        public Guid Id { get; private set; }
        public string Email { get; private set; } = default!;
        public string? PasswordHash { get; private set; }

        public string? MitIdSubId { get; private set; }
        public bool IsAdult { get; private set; }
        public bool IsMitIdLinked { get; private set; }

        public DateTime CreatedAt { get; private set; }
        public bool IsActive { get; private set; }

        private Account() { }

        public Account(string email, string? passwordHash)
        {
            Id = Guid.NewGuid();
            Email = email ?? throw new ArgumentNullException(nameof(email));
            PasswordHash = passwordHash;
            CreatedAt = DateTime.UtcNow;
            IsActive = true;
        }

        public void Deactivate()
        {
            IsActive = false;
        }

        public void ChangePassword(string newPasswordHash)
        {
            PasswordHash = newPasswordHash ?? throw new ArgumentNullException(nameof(newPasswordHash));
        }

        public void ApplyMitIdVerification(string mitIdSubId, bool isAdult)
        {
            MitIdSubId = mitIdSubId;
            IsAdult = isAdult;
            IsMitIdLinked = true;
        }
        public static Account Reconstruct(
            Guid id,
            string email,
            string? passwordHash,
            string? mitIdSubId = null,
            bool isAdult = false,
            bool isMitIdLinked = false,
            DateTime? createdAt = null,
            bool isActive = true)
        {
            return new Account
            {
                Id = id,
                Email = email,
                PasswordHash = passwordHash,
                MitIdSubId = mitIdSubId,
                IsAdult = isAdult,
                IsMitIdLinked = isMitIdLinked,
                CreatedAt = createdAt ?? DateTime.UtcNow,
                IsActive = isActive
            };
        }
    }
}

