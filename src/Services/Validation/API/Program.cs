using Application.BusinessLogic;
using Application.Interfaces;
using AutoMapper;
using Domain.Repositories;
using Infrastructure.Blockchain;
using Infrastructure.Caching;
using Infrastructure.Jwt;
using Infrastructure.Persistence;
using Infrastructure.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using StackExchange.Redis;
using System.Reflection;
using System.Text;
using ValidationService.Application.Interfaces;
using ValidationService.Infrastructure.Jwt;
using OpenTelemetry.Metrics;

var builder = WebApplication.CreateBuilder(args);

// -------------------------------------------------------
// Basic ASP.NET setup
// -------------------------------------------------------
builder.Services.AddControllers();
// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    var xmlFilename = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFilename);
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath, includeControllerXmlComments: true);
    }
});

// Metrics
builder.Services.AddOpenTelemetry()
    .WithMetrics(metrics =>
    {
        metrics.AddPrometheusExporter();
        metrics.AddMeter("Microsoft.AspNetCore.Hosting", "Microsoft.AspNetCore.Server.Kestrel");
    });

// -------------------------------------------------------
// AUTH (JWT Bearer)
// -------------------------------------------------------
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        var issuer = builder.Configuration["Jwt:Issuer"];
        var audience = builder.Configuration["Jwt:Audience"];
        var key = builder.Configuration["Jwt:Key"];

        if (string.IsNullOrWhiteSpace(key))
            throw new InvalidOperationException("Jwt:Key is missing (check appsettings / env vars).");

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = issuer,

            ValidateAudience = true,
            ValidAudience = audience,

            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),

            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(1)
        };
    });

builder.Services.AddAuthorization();

// -------------------------------------------------------
// AutoMapper ✅ samme stil som BachMitID (manuelt)
// -------------------------------------------------------
builder.Services.AddSingleton<IMapper>(sp =>
{
    var loggerFactory = sp.GetRequiredService<ILoggerFactory>();

    var config = new MapperConfiguration(cfg =>
    {
        // ✅ VIGTIGT:
        // Ret denne til den profil-klasse DU har i dit Validation-projekt.
        // Jeg giver to typiske muligheder afhængigt af dit namespace.
        //
        // Hvis du allerede har en MappingProfile i API eller Application, så brug den.
        // Eksempler:
        // cfg.AddProfile<Api.Mapping.MappingProfile>();
        // cfg.AddProfile<Application.Mapping.MappingProfile>();

        // ---- Standard forsøg ----
        // Hvis din profil hedder MappingProfile og ligger i API:
        // cfg.AddProfile<Api.MappingProfile>();

        // Hvis din profil hedder ValidationMappingProfile:
        // cfg.AddProfile<ValidationMappingProfile>();

        // ✅ Her vælger jeg den mest sandsynlige:
        cfg.AddMaps(AppDomain.CurrentDomain.GetAssemblies()); // fallback, men indenfor MapperConfiguration

        // Hvis du vil være 100% eksplicit, så kommentér linjen over ud
        // og brug én konkret cfg.AddProfile<...>();
    }, loggerFactory);

    return config.CreateMapper();
});

// -------------------------------------------------------
// Application services
// -------------------------------------------------------
builder.Services.AddScoped<ICredentialValidationService, CredentialValidationService>();

// Hashing & claim parsing
builder.Services.AddScoped<ICredentialFingerprintService, CredentialFingerprintService>();
builder.Services.AddScoped<ICredentialClaimParser, CredentialClaimParser>();

// -------------------------------------------------------
// Infrastructure – Persistence (Dapper)
// -------------------------------------------------------
builder.Services.AddScoped<IVerificationLogRepository, VerificationLogRepository>();

// -------------------------------------------------------
// Infrastructure – Redis cache
// -------------------------------------------------------
builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
{
    var config = builder.Configuration.GetConnectionString("Redis");
    if (string.IsNullOrWhiteSpace(config))
        throw new InvalidOperationException("Missing Redis connection string 'Redis'.");

    return ConnectionMultiplexer.Connect(config);
});

builder.Services.AddScoped<ICacheService, RedisCacheService>();

// -------------------------------------------------------
// Infrastructure – Fabric lookup client (Go-service)
// -------------------------------------------------------
builder.Services.AddHttpClient<IFabricLookupClient, FabricLookupClient>((serviceProvider, client) =>
{
    var config = serviceProvider.GetRequiredService<IConfiguration>();
    var baseUrl = config["Fabric:BaseUrl"] ?? "http://localhost:8080";

    client.BaseAddress = new Uri(baseUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
})
.ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
{
    ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
});

// -------------------------------------------------------
// Kafka producer (BuildingBlocks ONLY)
// -------------------------------------------------------
builder.Services.AddSingleton<BuildingBlocks.Contracts.Messaging.IKafkaProducer, BuildingBlocks.Kafka.KafkaProducer>();

// -------------------------------------------------------
// Infrastructure – VC Validation
// -------------------------------------------------------
builder.Services.AddScoped<IDidKeyResolver, DidKeyResolver>();
builder.Services.AddScoped<IJwtValidator, VcJwtValidator>();

// -------------------------------------------------------
// Build & pipeline
// -------------------------------------------------------
var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "ValidationService API v1");
        options.RoutePrefix = string.Empty;
    });

    app.UseHttpsRedirection();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapPrometheusScrapingEndpoint();

app.MapControllers();

app.Run();
