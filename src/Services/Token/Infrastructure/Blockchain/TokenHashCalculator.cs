using System.Security.Cryptography;
using System.Text;
using Application.Interfaces;

namespace Infrastructure.Blockchain
{
    public class TokenHashCalculator : ITokenHashCalculator
    {
        public string ComputeHash(string payload)
        {
            using var sha256 = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(payload);
            var hash = sha256.ComputeHash(bytes);
            return Convert.ToHexString(hash);
        }
    }
}