using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BachMitID.Application.DTOs
{
    public class MitIdAccountResult
    {
        public MitIdAccountDto Account { get; set; } = null!;
        public bool IsNew { get; set; }
    }
}
