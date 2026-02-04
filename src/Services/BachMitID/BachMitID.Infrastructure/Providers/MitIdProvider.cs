using BachMitID.Domain.Interfaces;
using BachMitID.Domain.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace BachMitID.Infrastructure.Providers;

/// <summary>
/// MitID identity provider implementation for Denmark.
/// Uses Signicat for MitID authentication.
/// </summary>
public class MitIdProvider : IIdentityProvider
{
    private readonly IConfiguration _config;
    private readonly ILogger<MitIdProvider> _logger;
    
    public string ProviderId => "mitid";
    public string Country => "DK";
    public string DisplayName => "MitID (Denmark)";
    
    public MitIdProvider(IConfiguration config, ILogger<MitIdProvider> logger)
    {
        _config = config;
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
    
    public Task<string> GetAuthorizationUrlAsync(string redirectUri, string state)
    {
        // TODO: Implement Signicat authorization URL generation
        // For now, return placeholder
        var baseUrl = _config["Signicat:BaseUrl"] ?? "https://preprod.signicat.com";
        var clientId = _config["Signicat:ClientId"];
        
        var authUrl = $"{baseUrl}/oidc/authorize" +
            $"?client_id={clientId}" +
            $"&redirect_uri={Uri.EscapeDataString(redirectUri)}" +
            $"&response_type=code" +
            $"&scope=openid profile mitid" +
            $"&state={state}";
        
        return Task.FromResult(authUrl);
    }
    
    public async Task<IdentityData> GetIdentityDataAsync(string authCode, CancellationToken ct = default)
    {
        _logger.LogInformation("Getting identity data from MitID for auth code");
        
        // TODO: Implement actual Signicat token exchange and userinfo call
        // For now, return mock data
        
        // In production, this would:
        // 1. Exchange authCode for access token
        // 2. Call Signicat userinfo endpoint
        // 3. Parse MitID claims (CPR, name, etc.)
        
        // Mock implementation
        await Task.Delay(100, ct); // Simulate API call
        
        var mockCpr = "0101901234"; // Mock CPR
        var mockName = "Test Testesen";
        
        return new IdentityData
        {
            ProviderId = ProviderId,
            Subject = $"mitid:{mockCpr}",
            DateOfBirth = ParseCprToBirthDate(mockCpr),
            Name = mockName,
            NationalId = mockCpr
        };
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
