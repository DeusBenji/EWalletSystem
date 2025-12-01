using Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Repositories
{
    public interface IAttestationRepository
    {
        Task SaveAsync(AgeAttestation attestation, CancellationToken ct = default);
        Task<AgeAttestation?> GetByIdAsync(Guid id, CancellationToken ct = default);
    }
}
