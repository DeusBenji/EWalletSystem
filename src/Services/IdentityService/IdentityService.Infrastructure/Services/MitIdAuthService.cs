using IdentityService.Application.DTOs;
using IdentityService.Application.Interfaces;
using IdentityService.Domain.Interfaces;
using IdentityService.Infrastructure.Configuration;
using IdentityService.Infrastructure.Providers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace IdentityService.Infrastructure.Services;

/// <summary>
/// Service for handling MitID authentication via Signicat session flow
/// Implements CSRF protection and one-time-use session enforcement
/// </summary>
public class MitIdAuthService : IMitIdAuthService
{
    private readonly MitIdProvider _provider;
    private readonly IMitIdSessionCache _sessionCache;
    private readonly IMitIdDbAccess _dbAccess;
    private readonly SignicatConfig _config;
    private readonly ILogger<MitIdAuthService> _logger;
    
    public MitIdAuthService(
        MitIdProvider provider,
        IMitIdSessionCache sessionCache,
        IMitIdDbAccess dbAccess,
        IOptions<SignicatConfig> config,
        ILogger<MitIdAuthService> logger)
    {
        _provider = provider;
        _sessionCache = sessionCache;
        _dbAccess = dbAccess;
        _config = config.Value;
        _logger = logger;
    }
    
    public async Task<string> StartAuthenticationAsync(Guid accountId, CancellationToken ct = default)
    {
        // Generate external reference for correlation
        var externalReference = Guid.NewGuid().ToString();
        
        _logger.LogInformation(
            "Starting MitID authentication for account {AccountId}, externalRef={ExternalRef}",
            accountId,
            externalReference);
        
        // Create session via Signicat
        var sessionResponse = await _provider.CreateSessionAsync(externalReference, ct);
        
        // Store session in cache with TTL (CSRF protection)
        var ttl = TimeSpan.FromSeconds(_config.SessionLifetimeSeconds);
        await _sessionCache.SetSessionAsync(
            sessionResponse.Id, 
            $"{accountId}:{externalReference}", 
            ttl);
        
        _logger.LogInformation(
            "Created MitID session for account {AccountId}, redirecting to Signicat",
            accountId);
        
        return sessionResponse.AuthenticationUrl;
    }
    
    public async Task<MitIdAccountResult?> HandleCallbackAsync(string sessionId, CancellationToken ct = default)
    {
        _logger.LogInformation("Handling MitID callback for session");
        
        // CSRF validation: Check if session exists in cache
        var sessionExists = await _sessionCache.ExistsAsync(sessionId);
        if (!sessionExists)
        {
            _logger.LogWarning("Session not found in cache - possible CSRF attack or expired session");
            return null;
        }
        
        // Get external reference from cache
        var cachedValue = await _sessionCache.GetExternalReferenceAsync(sessionId);
        if (string.IsNullOrEmpty(cachedValue))
        {
            _logger.LogWarning("Session exists but has no cached value");
            return null;
        }
        
        // Parse accountId from cached value
        var parts = cachedValue.Split(':');
        if (parts.Length != 2 || !Guid.TryParse(parts[0], out var accountId))
        {
            _logger.LogError("Invalid cached session value format: {Value}", cachedValue);
            return null;
        }
        
        try
        {
            // Get session status from Signicat
            var session = await _provider.GetSessionStatusAsync(sessionId, ct);
            
            if (session.Status != "SUCCESS")
            {
                _logger.LogWarning(
                    "Authentication not successful: status={Status}, error={Error}",
                    session.Status,
                    session.Error?.Message ?? "none");
                return null;
            }
            
            // Get identity data from session
            var identityData = await _provider.GetIdentityDataFromSessionAsync(sessionId, ct);
            
            // Check if MitID account already exists
            var existingEntity = await _dbAccess.GetByNationalId(identityData.NationalId!);
            
            MitIdAccountDto mitIdAccount;
            bool isNew;
            
            if (existingEntity != null)
            {
                // Update existing account
                _logger.LogInformation(
                    "Updating existing MitID account for CPR (masked), accountId={AccountId}",
                    accountId);
                
                existingEntity.SubID = identityData.NationalId!;
                existingEntity.IsAdult = identityData.DateOfBirth!.Value.AddYears(18) <= DateTime.UtcNow;
                
                await _dbAccess.UpdateMitIdAccount(existingEntity);
                
                mitIdAccount = new MitIdAccountDto
                {
                    Id = existingEntity.ID,
                    AccountId = existingEntity.AccountID,
                    NationalId = existingEntity.SubID,
                    Name = identityData.Name,
                    DateOfBirth = identityData.DateOfBirth!.Value,
                    Provider = identityData.Provider ?? "mitid",
                    Loa = identityData.Loa,
                    SubId = existingEntity.SubID,
                    IsAdult = existingEntity.IsAdult,
                    CreatedAt = DateTime.UtcNow,
                    LastAuthenticated = DateTime.UtcNow
                };
                isNew = false;
            }
            else
            {
                // Create new account
                _logger.LogInformation(
                    "Creating new MitID account for CPR (masked), accountId={AccountId}",
                    accountId);
                
                var newEntity = new Domain.Model.MitID_Account
                {
                    ID = Guid.NewGuid(),
                    AccountID = accountId,
                    SubID = identityData.NationalId!,
                    IsAdult = identityData.DateOfBirth!.Value.AddYears(18) <= DateTime.UtcNow
                };
                
                await _dbAccess.CreateMitIdAccount(newEntity);
                
                mitIdAccount = new MitIdAccountDto
                {
                    Id = newEntity.ID,
                    AccountId = newEntity.AccountID,
                    NationalId = newEntity.SubID,
                    Name = identityData.Name,
                    DateOfBirth = identityData.DateOfBirth!.Value,
                    Provider = identityData.Provider ?? "mitid",
                    Loa = identityData.Loa,
                    SubId = newEntity.SubID,
                    IsAdult = newEntity.IsAdult,
                    CreatedAt = DateTime.UtcNow,
                    LastAuthenticated = DateTime.UtcNow
                };
                isNew = true;
            }
            
            // One-time-use: Remove session from cache
            await _sessionCache.RemoveSessionAsync(sessionId);
            
            _logger.LogInformation(
                "Successfully processed MitID authentication for account {AccountId}, isNew={IsNew}",
                accountId,
                isNew);
            
            return new MitIdAccountResult
            {
                Account = mitIdAccount,
                IsNew = isNew
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling MitID callback for session");
            
            // Clean up session on error
            await _sessionCache.RemoveSessionAsync(sessionId);
            
            throw;
        }
    }
    
    public async Task<GetSessionResponse> GetSessionStatusAsync(string sessionId, CancellationToken ct = default)
    {
        // CSRF validation
        var sessionExists = await _sessionCache.ExistsAsync(sessionId);
        if (!sessionExists)
        {
            throw new InvalidOperationException("Session not found or expired");
        }
        
        return await _provider.GetSessionStatusAsync(sessionId, ct);
    }
}
