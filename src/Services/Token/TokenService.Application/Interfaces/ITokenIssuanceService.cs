using Application.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;




namespace Application.Interfaces
{
    public interface ITokenIssuanceService
    {
        /// <summary>
        /// Issue age-specific credential (legacy flow)
        /// </summary>
        Task<IssuedTokenDto> IssueTokenAsync(IssueTokenDto dto, CancellationToken ct = default);
        
        /// <summary>
        /// Issue policy-based credential (universal flow)
        /// </summary>
        /// <param name="dto">Policy credential request</param>
        /// <param name="ct">Cancellation token</param>
        /// <returns>Issued policy credential</returns>
        Task<IssuedTokenDto> IssuePolicyCredentialAsync(IssuePolicyCredentialDto dto, CancellationToken ct = default);
    }
}
