using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// DTOs/VerifyCredentialDto.cs
namespace Application.DTOs
{
    public class VerifyCredentialDto
    {
        public string VcJwt { get; set; } = default!;
    }
}
