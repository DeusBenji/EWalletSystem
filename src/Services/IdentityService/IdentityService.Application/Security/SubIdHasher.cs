using System.Security.Cryptography;
using System.Text;

namespace IdentityService.Application.Security
{
    public static class SubIdHasher
    {
        public static string Hash(string subId)
        {
            using var sha = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(subId);
            var hashBytes = sha.ComputeHash(bytes);
            return Convert.ToHexString(hashBytes);   // returnerer en hex string
        }
    }
}
