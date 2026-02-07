using IdentityService.Application.DTOs;

namespace IdentityService.Application.Interfaces;

public interface ISignicatHttpClient
{
    Task<SessionDataDto> CreateSessionAsync(SessionRequestDto request, CancellationToken ct = default);
    Task<SessionDataDto> GetSessionStatusAsync(string sessionId, CancellationToken ct = default);
}
