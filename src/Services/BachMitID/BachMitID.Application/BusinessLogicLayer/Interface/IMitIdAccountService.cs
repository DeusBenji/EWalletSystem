using BachMitID.Application.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace BachMitID.Application.BusinessLogicLayer.Interface
{
    public interface IMitIdAccountService
    {
        Task<MitIdAccountResult?> CreateFromClaimsAsync(ClaimsPrincipal user);
        Task<MitIdAccountDto?> GetByAccountIdAsync(Guid accountId);
        Task<List<MitIdAccountDto>> GetAllAsync();
        Task<bool> UpdateAsync(Guid id, MitIdAccountDto dto);
        Task<bool> DeleteAsync(Guid id);
    }
}
