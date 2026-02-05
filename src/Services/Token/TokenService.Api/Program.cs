using Api.BackgroundServices;
using Application.BusinessLogic;
using Application.Interfaces;
using BuildingBlocks.Contracts.Messaging;
using Domain.Repositories;
using Infrastructure.Blockchain;
using Infrastructure.Persistence;
using Infrastructure.Redis;
using StackExchange.Redis;
using TokenService.Application.Interfaces;
using TokenService.Infrastructure.Signing;
using AutoMapper;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using OpenTelemetry.Metrics;

var builder = WebApplication.CreateBuilder(args);

// -----------------------------------------------------
// ASP.NET Core basics
// -----------------------------------------------------
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Metrics
builder.Services.AddOpenTelemetry()
    .WithMetrics(metrics =>
    {
        metrics.AddPrometheusExporter();
        metrics.AddMeter("Microsoft.AspNetCore.Hosting", "Microsoft.AspNetCore.Server.Kestrel");
    });

// -----------------------------------------------------
// AUTH (JWT Bearer)
// -----------------------------------------------------
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

// -----------------------------------------------------
// AutoMapper – manuel registration (som i IdentityService)
// -----------------------------------------------------
builder.Services.AddSingleton<IMapper>(sp =>
{
    var loggerFactory = sp.GetRequiredService<ILoggerFactory>();

    var config = new MapperConfiguration(cfg =>
    {
        // Finder alle Profile-klasser i alle loaded assemblies
        cfg.AddMaps(AppDomain.CurrentDomain.GetAssemblies());
    }, loggerFactory);

    return config.CreateMapper();
});

// -----------------------------------------------------
// Application layer
// -----------------------------------------------------
builder.Services.AddScoped<ITokenIssuanceService, TokenIssuanceService>();
builder.Services.AddScoped<IMitIdVerifiedService, MitIdVerifiedService>();

// Eligibility providers (policy plugins)
builder.Services.AddScoped<IEligibilityProvider, TokenService.Application.Providers.MitIdEligibilityProvider>();

// -----------------------------------------------------
// Domain repositories (interfaces i Domain, impl i Infrastructure)
// -----------------------------------------------------
builder.Services.AddSingleton<IAttestationRepository, AttestationRepository>();
builder.Services.AddSingleton<IAccountAgeStatusRepository, AccountAgeStatusRepository>();

// -----------------------------------------------------
// Infrastructure: Redis Cache
// -----------------------------------------------------
// Redis connection
builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
{
    var configuration = builder.Configuration.GetConnectionString("Redis")
                        ?? "localhost:6379";
    return ConnectionMultiplexer.Connect(configuration);
});

// Distributed Cache (bruges af AccountAgeStatusCache)
builder.Services.AddStackExchangeRedisCache(options =>
{
    var redisConnection = builder.Configuration.GetConnectionString("Redis")
                          ?? "localhost:6379";
    options.Configuration = redisConnection;
    options.InstanceName = "TokenService:";
});

// Cache implementation
builder.Services.AddSingleton<IAccountAgeStatusCache, AccountAgeStatusCache>();

// -----------------------------------------------------
// Infrastructure: blockchain (Fabric) + hashing
// -----------------------------------------------------
builder.Services.AddHttpClient<IFabricAnchorClient, FabricAnchorClient>(client =>
{
    var config = builder.Configuration;
    var baseUrl = config["Fabric:BaseUrl"] ?? "http://localhost:8080";
    client.BaseAddress = new Uri(baseUrl);
});

builder.Services.AddSingleton<ITokenHashCalculator, TokenHashCalculator>();

// -----------------------------------------------------
// Infrastructure: VC Signing
// -----------------------------------------------------
builder.Services.AddSingleton<IKeyProvider, FileKeyProvider>();
builder.Services.AddSingleton<IVcSigningService, VcSigningService>();

// -----------------------------------------------------
// Infrastructure: messaging (Kafka via BuildingBlocks)
// -----------------------------------------------------
builder.Services.AddSingleton<IKafkaProducer, BuildingBlocks.Kafka.KafkaProducer>();
builder.Services.AddSingleton<IKafkaConsumer, BuildingBlocks.Kafka.KafkaConsumer>();
builder.Services.AddHostedService<MitIdVerifiedConsumer>();

// -----------------------------------------------------
// CORS (kun development)
// -----------------------------------------------------
builder.Services.AddCors(options =>
{
    options.AddPolicy("DevCors", policy =>
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader());
});

// -----------------------------------------------------
// Logging
// -----------------------------------------------------
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

// -----------------------------------------------------
// Build + pipeline
// -----------------------------------------------------
var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.UseCors("DevCors");
}

//app.UseHttpsRedirection();

// ?? auth middleware order matters
app.UseAuthentication();
app.UseAuthorization();

app.MapPrometheusScrapingEndpoint();

app.MapControllers();
app.Run();
