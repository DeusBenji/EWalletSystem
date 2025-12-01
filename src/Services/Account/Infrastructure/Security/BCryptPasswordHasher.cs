using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BCrypt.Net;
using Application.Interfaces;
using System.Threading.Tasks;

namespace Infrastructure.Security
{
    public class BCryptPasswordHasher : IPasswordHasher
    {
        public string HashPassword(string password)
        {
            return BCrypt.Net.BCrypt.HashPassword(password, workFactor: 12);
        }

        public bool Verify(string password, string hash)
        {
            return BCrypt.Net.BCrypt.Verify(password, hash);
        }
    }
}

