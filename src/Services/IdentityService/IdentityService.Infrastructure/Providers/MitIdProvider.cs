using IdentityService.Application.DTOs;
using IdentityService.Domain.Interfaces;
using IdentityService.Domain.Models;
using IdentityService.Infrastructure.Configuration;
using IdentityService.Infrastructure.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace IdentityService.Infrastructure.Providers;

/// <summary>
/// MitID identity provider implementation for Denmark.
/// Uses Signicat EID Hub REST API (session-based flow).
/// </summary>
public class MitIdProvider : IIdentityProvider
{
    private readonly SignicatHttpClient _signicatClient;
    private readonly SignicatConfig _config;
    private readonly ILogger<MitIdProvider> _logger;
    
    public string ProviderId => "mitid";
    public string Country => "DK";
    public string DisplayName => "MitID (Denmark)";
    
    public MitIdProvider(
        SignicatHttpClient signicatClient,
        IOptions<SignicatConfig> config,
        ILogger<MitIdProvider> logger)
    {
        _signicatClient = signicatClient;
        _config = config.Value;
        _logger = logger;
    }
    
    public ProviderCapabilities GetCapabilities() => new()
    {
        CanProvideAge = true,
        CanProvideName = true,
        CanProvideNationalId = true,
        CanProvideAddress = false,
        CanProvideEmail = false,
        CanProvidePhone = false
    };
    
    /// <summary>
    /// Create a new authentication session
    /// </summary>
    /// <param name="externalReference">Internal correlation ID (GUID)</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Session response with authenticationUrl</returns>
    public async Task<CreateSessionResponse> CreateSessionAsync(
        string externalReference, 
        CancellationToken ct = default)
    {
        _logger.LogInformation(
            "Creating MitID session with externalReference={ExternalReference}", 
            externalReference);
        
        var callbackBaseUrl = _config.DefaultCallbackBaseUrl.TrimEnd('/');
        
        var request = new CreateSessionRequest
        {
            Flow = "redirect",
            CallbackUrls = new CallbackUrls
            {
                Success = $"{callbackBaseUrl}?status=success",
                Abort = $"{callbackBaseUrl}?status=abort",
                Error = $"{callbackBaseUrl}?status=error"
            },
            RequestedAttributes = _config.RequestedAttributes,
            AllowedProviders = _config.AllowedProviders,
            RequestedLoa = _config.RequestedLoa,
            UsageReference = _config.UsageReference,
            SessionLifetime = _config.SessionLifetimeSeconds,
            ExternalReference = externalReference
        };
        
        var response = await _signicatClient.CreateSessionAsync(request, ct);
        
        _logger.LogInformation(
            "Created MitID session, redirecting to authenticationUrl");
        
        return response;
    }
    
    /// <summary>
    /// Get session status
    /// </summary>
    /// <param name="sessionId">Signicat session ID</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Session status with subject claims</returns>
    public async Task<GetSessionResponse> GetSessionStatusAsync(
        string sessionId, 
        CancellationToken ct = default)
    {
        _logger.LogDebug("Getting MitID session status for sessionId");
        
        var response = await _signicatClient.GetSessionStatusAsync(sessionId, ct);
        
        _logger.LogInformation(
            "MitID session status: {Status}, provider: {Provider}", 
            response.Status, 
            response.Provider ?? "unknown");
        
        return response;
    }
    
    /// <summary>
    /// Get identity data from session (after successful authentication)
    /// </summary>
    /// <param name="sessionId">Signicat session ID</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Identity data mapped from session subject</returns>
    public async Task<IdentityData> GetIdentityDataFromSessionAsync(
        string sessionId, 
        CancellationToken ct = default)
    {
        var session = await GetSessionStatusAsync(sessionId, ct);
        
        if (session.Status != "SUCCESS")
        {
            throw new InvalidOperationException(
                $"Cannot get identity data from session with status: {session.Status}");
        }
        
        if (session.Subject == null)
        {
            throw new InvalidOperationException(
                "Session succeeded but subject is null");
        }
        
        var subject = session.Subject;
        
        // Parse date of birth
        DateTime dateOfBirth;
        if (!string.IsNullOrEmpty(subject.DateOfBirth))
        {
            // Signicat returns YYYY-MM-DD format
            dateOfBirth = DateTime.Parse(subject.DateOfBirth);
        }
        else if (!string.IsNullOrEmpty(subject.NationalId))
        {
            // Fallback: parse from CPR
            dateOfBirth = ParseCprToBirthDate(subject.NationalId);
        }
        else
        {
            throw new InvalidOperationException(
                "Cannot determine date of birth from session subject");
        }
        
        var identityData = new IdentityData
        {
            ProviderId = ProviderId,
            Subject = subject.Uuid ?? $"mitid:{subject.NationalId}",
            DateOfBirth = dateOfBirth,
            Name = subject.Name ?? $"{subject.FirstName} {subject.LastName}".Trim(),
            NationalId = subject.NationalId,
            Provider = session.Provider,
            Loa = session.Loa,
            AuthenticatedAt = session.Authenticated
        };
        
        _logger.LogInformation(
            "Mapped identity data from MitID session: subject={Subject}, dob={DateOfBirth:yyyy-MM-dd}", 
            identityData.Subject, 
            identityData.DateOfBirth);
        
        return identityData;
    }
    
    /// <summary>
    /// Legacy method - not used in session flow
    /// </summary>
    [Obsolete("Use CreateSessionAsync instead")]
    public Task<string> GetAuthorizationUrlAsync(string redirectUri, string state)
    {
        throw new NotSupportedException(
            "GetAuthorizationUrlAsync is not supported in session-based flow. Use CreateSessionAsync instead.");
    }
    
    /// <summary>
    /// Legacy method - not used in session flow
    /// </summary>
    [Obsolete("Use GetIdentityDataFromSessionAsync instead")]
    public Task<IdentityData> GetIdentityDataAsync(string authCode, CancellationToken ct = default)
    {
        throw new NotSupportedException(
            "GetIdentityDataAsync is not supported in session-based flow. Use GetIdentityDataFromSessionAsync instead.");
    }
    
    private DateTime ParseCprToBirthDate(string cpr)
    {
        if (string.IsNullOrEmpty(cpr) || cpr.Length < 6)
        {
            throw new ArgumentException("Invalid CPR format", nameof(cpr));
        }
        
        var day = int.Parse(cpr.Substring(0, 2));
        var month = int.Parse(cpr.Substring(2, 2));
        var year = int.Parse(cpr.Substring(4, 2));
        
        // Determine century based on 7th digit
        var centuryDigit = cpr.Length > 6 ? int.Parse(cpr.Substring(6, 1)) : 0;
        
        if (centuryDigit >= 0 && centuryDigit <= 3)
        {
            year += 1900;
        }
        else if (centuryDigit >= 4 && centuryDigit <= 9)
        {
            if (year >= 0 && year <= 36)
            {
                year += 2000;
            }
            else
            {
                year += 1900;
            }
        }
        
        return new DateTime(year, month, day);
    }
}
