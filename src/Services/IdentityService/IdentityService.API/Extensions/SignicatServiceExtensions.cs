using IdentityService.Application.Interfaces;
using IdentityService.Infrastructure.Caching;
using IdentityService.Infrastructure.Configuration;
using IdentityService.Infrastructure.Http;
using IdentityService.Infrastructure.Providers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;

namespace IdentityService.API.Extensions;

/// <summary>
/// Extension methods for configuring Signicat services
/// </summary>
public static class SignicatServiceExtensions
{
    public static IServiceCollection AddSignicatServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Bind and validate configuration
        var signicatConfig = configuration
            .GetSection(SignicatConfig.SectionName)
            .Get<SignicatConfig>();
        
        if (signicatConfig != null)
        {
            signicatConfig.Validate();
            services.Configure<SignicatConfig>(
                configuration.GetSection(SignicatConfig.SectionName));
        }
        
        // Register HttpClient
        services.AddHttpClient<SignicatHttpClient>();
        
        // Register session cache
        services.AddScoped<IMitIdSessionCache, MitIdSessionCache>();
        
        // Register MitID provider
        services.AddScoped<MitIdProvider>();
        
        // Register MitID auth service
        services.AddScoped<IMitIdAuthService, Infrastructure.Services.MitIdAuthService>();
        
        return services;
    }
}
