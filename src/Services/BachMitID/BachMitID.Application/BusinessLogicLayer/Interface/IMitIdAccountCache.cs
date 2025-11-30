using BachMitID.Application.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BachMitID.Application.BusinessLogicLayer.Interface
{
    public interface IMitIdAccountCache
    {
        Task<MitIdAccountDto?> GetAsync(Guid accountId);
        Task SetAsync(MitIdAccountDto dto, TimeSpan ttl);
        Task RemoveAsync(Guid accountId);
    }
}
