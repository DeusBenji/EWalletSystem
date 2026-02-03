using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System;
using System.IdentityModel.Tokens.Jwt;

namespace Application.Interfaces
{
    public interface ICredentialClaimParser
    {
        Guid? ExtractAccountId(JwtSecurityToken token);
    }
}
