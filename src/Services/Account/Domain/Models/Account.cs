using System;

namespace Domain.Models
{
    public class Account
    {
        public Guid Id { get; private set; }
        public string Email { get; private set; } = default!;
        public string? PasswordHash { get; private set; }

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

        public static Account Reconstruct(
            Guid id,
            string email,
            string? passwordHash,
            DateTime? createdAt = null,
            bool isActive = true)
        {
            return new Account
            {
                Id = id,
                Email = email,
                PasswordHash = passwordHash,
                CreatedAt = createdAt ?? DateTime.UtcNow,
                IsActive = isActive
            };
        }
    }
}
