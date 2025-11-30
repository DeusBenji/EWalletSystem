using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BachMitID.Domain.Model
{

    public class Account
    {
        public Guid ID { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }

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
