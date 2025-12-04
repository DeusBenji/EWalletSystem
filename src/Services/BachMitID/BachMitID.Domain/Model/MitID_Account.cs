using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BachMitID.Domain.Model
{
    public class MitID_Account
    {

        public Guid ID { get; set; }
        public Guid AccountID { get; set; }
        public string SubID { get; set; } = null!;
        public bool IsAdult { get; set; }
        //Empty constructor
        public MitID_Account() { }

        //Constructor with parameter expcept ID
        public MitID_Account(Guid accountID, string subID, bool isAdult)
        {
            AccountID = accountID;
            SubID = subID;
            IsAdult = isAdult;
        }
        //Constructor with all parameters
        public MitID_Account(Guid id, Guid accountID, string subID, bool isAdult) : this(accountID, subID, isAdult)
        {
            ID = id;
        }

    }
}
