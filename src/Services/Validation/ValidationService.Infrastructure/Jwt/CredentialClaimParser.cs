using System;
using System.IdentityModel.Tokens.Jwt;
using Application.Interfaces;

namespace Infrastructure.Jwt
{
    public class CredentialClaimParser : ICredentialClaimParser
    {
        public Guid? ExtractAccountId(JwtSecurityToken token)
        {
            // Først subject
            if (Guid.TryParse(token.Subject, out var subGuid))
                return subGuid;

            // Fallback: custom "accountId" claim
            if (token.Payload.TryGetValue("accountId", out var raw) &&
                Guid.TryParse(raw?.ToString(), out var id2))
                return id2;

            return null;
        }
    }
}
