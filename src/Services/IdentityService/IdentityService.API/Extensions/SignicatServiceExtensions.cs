using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using IdentityService.Application.Interfaces;
using IdentityService.Infrastructure.Services;
using IdentityService.Domain.Interfaces;
using IdentityService.Infrastructure.Providers;
using IdentityService.Infrastructure.Persistence;
using IdentityService.Infrastructure.Http;
using IdentityService.Infrastructure.Mapping;

namespace IdentityService.API.Extensions;

public static class SignicatServiceExtensions
{
    public static IServiceCollection AddSignicatServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Auth service
        services.AddScoped<ISignicatAuthService, SignicatAuthService>();

        // HTTP Client
        services.AddHttpClient<ISignicatHttpClient, SignicatHttpClient>()
            .AddStandardResilienceHandler();

        // Access Token Cache (Singleton as it uses IMemoryCache internally)
        services.AddHttpClient<ISignicatAccessTokenCache, SignicatAccessTokenCache>()
            .AddStandardResilienceHandler();

        // Providers
        services.AddScoped<IIdentityProvider, MitIdProvider>();
        services.AddScoped<IIdentityProvider, SwedishBankIdProvider>();
        services.AddScoped<IIdentityProvider, BankIdNoProvider>();

        // Claims Mappers
        services.AddScoped<IClaimsMapper, MitIdClaimsMapper>();
        services.AddScoped<IClaimsMapper, BankIdSeClaimsMapper>();
        services.AddScoped<IClaimsMapper, BankIdNoClaimsMapper>();

        // PII redaction
        services.AddScoped<IPiiRedactionService, PiiRedactionService>();

        // Repository
        services.AddScoped<IAgeVerificationRepository, AgeVerificationRepository>();

        // Logging (SafeLogger)
        services.AddScoped(typeof(ISafeLogger<>), typeof(IdentityService.Infrastructure.Logging.SafeLogger<>));
        
        return services;
    }
}
