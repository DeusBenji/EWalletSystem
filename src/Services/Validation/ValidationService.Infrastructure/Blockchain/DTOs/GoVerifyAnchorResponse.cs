using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Blockchain.DTOs
{
    internal record GoVerifyAnchorResponse(
        string Hash,
        bool Exists,
        bool Valid,
        DateTime? Timestamp
    );

}
