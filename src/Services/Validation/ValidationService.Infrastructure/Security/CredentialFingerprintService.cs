using System.Security.Cryptography;
using System.Text;
using Application.Interfaces;

namespace Infrastructure.Security
{
    public class CredentialFingerprintService : ICredentialFingerprintService
    {
        public string Hash(string rawValue)
        {
            if (string.IsNullOrWhiteSpace(rawValue))
                throw new ArgumentException("Value cannot be empty", nameof(rawValue));

            using var sha256 = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(rawValue);
            var hashBytes = sha256.ComputeHash(bytes);

            // Return hex string
            return Convert.ToHexString(hashBytes).ToLowerInvariant();
        }
    }
}