using IdentityService.Application.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IdentityService.Application.BusinessLogicLayer.Interface
{
    public interface IMitIdAccountCache
    {
        Task<MitIdAccountDto?> GetAsync(Guid accountId);
        Task SetAsync(MitIdAccountDto dto, TimeSpan ttl);
        Task RemoveAsync(Guid accountId);
    }
}
