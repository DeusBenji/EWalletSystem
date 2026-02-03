using Application.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Application.Interfaces
{
    public interface ICredentialValidationService
    {
        Task<VerifyCredentialResultDto> VerifyAsync(VerifyCredentialDto request);
    }
}
