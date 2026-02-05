using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IdentityService.Domain.Model
{

    public class Account
    {
        public Guid ID { get; set; }
        public string Email { get; set; }
        public string Password { get; set; } = null!;
        
        // Identity Provider fields
        public string ProviderId { get; set; } = "mitid"; // Default to MitID for backward compatibility
        public string? ProviderSubject { get; set; } // Unique ID from provider
        public string? Name { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string? NationalId { get; set; } // CPR, SSN, etc.
        
        // Timestamps
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? LastLoginAt { get; set; }

        //Empty constructor
        public Account(){}


        //Constructor with parameter expcept ID
        public Account(string email, string password)
        {
            Email = email;
            Password = password;
        }

        //Constructor with all parameters
        public Account(Guid id, string email, string password): this(email, password)
        {
            ID = id;

        }


        public Account(Guid id, string email)
        {
            ID = id;
            Email = email;

        }

    }
}
