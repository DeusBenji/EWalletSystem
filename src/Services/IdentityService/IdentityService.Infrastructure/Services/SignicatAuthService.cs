using IdentityService.Application.DTOs;
using IdentityService.Application.Interfaces;
using IdentityService.Domain.Interfaces;
using IdentityService.Domain.Model;
using IdentityService.Infrastructure.Configuration;
using IdentityService.Infrastructure.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace IdentityService.Infrastructure.Services;

/// <summary>
/// Generic authentication service for Signicat EID Hub.
/// Supports multiple providers (MitID, BankID SE, BankID NO) via configuration and mappers.
/// Implements Privacy-by-Design by using strictly typed DTOs and claims mappers that discard PII.
/// </summary>
public class SignicatAuthService : ISignicatAuthService
{
    private readonly ISignicatHttpClient _httpClient;
    private readonly ISessionCache _sessionCache;
    private readonly IAgeVerificationRepository _repository;
    private readonly IEnumerable<IClaimsMapper> _mappers;
    private readonly SignicatConfig _config;
    private readonly ISafeLogger<SignicatAuthService> _logger;

    public SignicatAuthService(
        ISignicatHttpClient httpClient,
        ISessionCache sessionCache,
        IAgeVerificationRepository repository,
        IEnumerable<IClaimsMapper> mappers,
        IOptions<SignicatConfig> config,
        ISafeLogger<SignicatAuthService> logger)
    {
        _httpClient = httpClient;
        _sessionCache = sessionCache;
        _repository = repository;
        _mappers = mappers;
        _config = config.Value;
        _logger = logger;
    }

    public async Task<(string authUrl, string sessionId)> StartAuthenticationAsync(
        string providerId, 
        Guid? accountId = null, 
        CancellationToken ct = default)
    {
        // Metric: Start
        Infrastructure.Metrics.IdentityServiceMetrics.AuthStartCounter.Add(1, 
            new KeyValuePair<string, object?>("provider", providerId));

        // validate provider
        if (!_config.Providers.TryGetValue(providerId, out var providerOptions))
        {
            throw new ArgumentException($"Provider '{providerId}' is not configured or allowed.", nameof(providerId));
        }

        if (!_config.AllowedProviders.Contains(providerId))
        {
             throw new ArgumentException($"Provider '{providerId}' is not in the allowed providers list.", nameof(providerId));
        }

        var externalReference = Guid.NewGuid().ToString();

        _logger.LogInformation("Starting authentication for provider {ProviderId}, accountId {AccountId}", providerId, accountId?.ToString() ?? "<null>");

        var request = new SessionRequestDto(
            Flow: "redirect",
            AllowedProviders: new[] { providerId },
            CallbackUrls: new CallbackUrls(
                Success: $"{providerOptions.GlobalCallbackUrl.TrimEnd('/')}/{providerId}/callback",
                Abort: $"{providerOptions.GlobalCallbackUrl.TrimEnd('/')}/{providerId}/abort",
                Error: $"{providerOptions.GlobalCallbackUrl.TrimEnd('/')}/{providerId}/error"
            ),
            RequestedAttributes: providerOptions.RequestedAttributes,
            SessionLifetime: _config.SessionLifetimeSeconds,
            ExternalReference: externalReference
        );

        var session = await _httpClient.CreateSessionAsync(request, ct);

        if (session.Id == null || session.AuthenticationUrl == null)
        {
            throw new InvalidOperationException("Failed to create session: Missing Id or AuthenticationUrl from Signicat response.");
        }

        // Cache session
        var ttl = TimeSpan.FromSeconds(_config.SessionLifetimeSeconds);
        // We include startTime in the cache if we want accurate latency. 
        // For now, calculating latency based on "VerifiedAt" vs "CreatedAt" (if available from session) or current time.
        await _sessionCache.SetSessionAsync(session.Id, externalReference, providerId, ttl);

        return (session.AuthenticationUrl, session.Id);
    }

    public async Task<AgeVerificationDto> HandleCallbackAsync(
        string providerId, 
        string sessionId, 
        CancellationToken ct = default)
    {
        _logger.LogInformation("Handling callback for provider {ProviderId}, session {SessionId}", providerId, MaskSessionId(sessionId));

        try 
        {
            // CSRF Check
            if (!await _sessionCache.ExistsAsync(sessionId))
            {
                 _logger.LogWarning("Session {SessionId} not found or expired.", MaskSessionId(sessionId));
                 throw new InvalidOperationException("Session invalid or expired (CSRF check failed).");
            }

            // Get session data
            var sessionData = await _httpClient.GetSessionStatusAsync(sessionId, ct);

            if (sessionData.Status != "SUCCESS")
            {
                _logger.LogWarning("Session {SessionId} status is {Status}, not SUCCESS.", MaskSessionId(sessionId), sessionData.Status ?? "<unknown>");
                throw new InvalidOperationException($"Authentication failed with status: {sessionData.Status}");
            }

            // Provider Mismatch Check
            if (!string.Equals(sessionData.Provider, providerId, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogError(
                    "Provider mismatch detected! Expected: {Expected}, Actual: {Actual}. SessionId: {SessionId}", 
                    providerId, 
                    sessionData.Provider, 
                    MaskSessionId(sessionId));
                
                throw new InvalidOperationException("Provider mismatch detected. Session validation failed.");
            }

            // Find mapper
            var mapper = _mappers.FirstOrDefault(m => m.ProviderId.Equals(providerId, StringComparison.OrdinalIgnoreCase));
            if (mapper == null)
            {
                throw new InvalidOperationException($"No claims mapper found for provider '{providerId}'.");
            }

            // Map to AgeVerificationDto (Privacy-First extraction)
            var ageVerification = mapper.MapToAgeVerification(sessionData);

            // One-time use: Remove from cache
            await _sessionCache.RemoveSessionAsync(sessionId);

            // Persist to database
            var entity = new AgeVerification
            {
                ProviderId = ageVerification.ProviderId,
                SubjectId = ageVerification.SubjectId,
                IsAdult = ageVerification.IsAdult,
                VerifiedAt = ageVerification.VerifiedAt,
                AssuranceLevel = ageVerification.AssuranceLevel,
                ExpiresAt = ageVerification.ExpiresAt,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _repository.UpsertVerificationAsync(entity);
            
            // Metric: Success
            Infrastructure.Metrics.IdentityServiceMetrics.AuthCallbackSuccessCounter.Add(1,
                new KeyValuePair<string, object?>("provider", providerId));

            return ageVerification; 
        }
        catch (Exception)
        {
            // Metric: Failure
            Infrastructure.Metrics.IdentityServiceMetrics.AuthCallbackFailureCounter.Add(1,
                new KeyValuePair<string, object?>("provider", providerId));
            throw;
        }
    }

    public async Task<string> GetSessionStatusAsync(string sessionId, CancellationToken ct = default)
    {
        if (!await _sessionCache.ExistsAsync(sessionId))
        {
             throw new InvalidOperationException("Session invalid or expired.");
        }
        var session = await _httpClient.GetSessionStatusAsync(sessionId, ct);
        return session.Status ?? "UNKNOWN";
    }

    private static string MaskSessionId(string sessionId)
    {
        if (string.IsNullOrEmpty(sessionId) || sessionId.Length <= 8) return "***";
        return $"{sessionId[..4]}***{sessionId[^4..]}";
    }
}
