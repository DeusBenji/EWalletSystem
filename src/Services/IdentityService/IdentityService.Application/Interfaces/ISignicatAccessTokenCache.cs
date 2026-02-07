using System.Threading;
using System.Threading.Tasks;

namespace IdentityService.Application.Interfaces;

/// <summary>
/// Handles retrieval and caching of Signicat OAuth access tokens
/// </summary>
public interface ISignicatAccessTokenCache
{
    /// <summary>
    /// Get a valid access token.
    /// Returns cached token if available and not expired.
    /// Otherwise, fetches a new token from Signicat and caches it.
    /// </summary>
    Task<string> GetAccessTokenAsync(CancellationToken ct = default);
}
