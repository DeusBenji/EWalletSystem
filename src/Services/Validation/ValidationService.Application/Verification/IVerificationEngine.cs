using System.Threading;
using System.Threading.Tasks;
using BuildingBlocks.Contracts.Verification;

namespace ValidationService.Application.Verification
{
    public interface IVerificationEngine
    {
        Task<VerificationResult> VerifyAsync(VerificationRequest request, CancellationToken ct);
    }
}
