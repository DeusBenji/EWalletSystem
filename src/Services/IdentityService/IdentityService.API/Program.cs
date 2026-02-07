using AutoMapper;
using IdentityService.API.Extensions;
using IdentityService;
using IdentityService.Domain.Interfaces;

using BuildingBlocks.Contracts.Messaging;
using BuildingBlocks.Kafka;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using StackExchange.Redis;
using System.Text;
using OpenTelemetry.Metrics;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

// Swagger / OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "IdentityService API",
        Version = "v1",
        Description = "API til MitID-integration"
    });
});

// Metrics
builder.Services.AddOpenTelemetry()
    .WithMetrics(metrics =>
    {
        metrics.AddPrometheusExporter();
        metrics.AddMeter("Microsoft.AspNetCore.Hosting", "Microsoft.AspNetCore.Server.Kestrel");
        metrics.AddMeter(IdentityService.Infrastructure.Metrics.IdentityServiceMetrics.MeterName);
    });

var redisConn = builder.Configuration.GetConnectionString("RedisConnection")
    ?? throw new InvalidOperationException("RedisConnection is missing from appsettings.json");

var authSection = builder.Configuration.GetSection("Authentication");
var authority = authSection["Authority"];
var clientId = authSection["ClientId"];
var clientSecret = authSection["ClientSecret"];
var callbackPath = authSection["CallbackPath"] ?? "/signin-oidc";

// ? Public origin (til gateway prefix) � fx "http://localhost:7005/mitid"
var publicOrigin = builder.Configuration["PublicOrigin"];

// ?? JWT settings til gateway-tokens
var jwtSection = builder.Configuration.GetSection("Jwt");
var jwtIssuer = jwtSection["Issuer"];
var jwtAudience = jwtSection["Audience"];
var jwtKey = jwtSection["Key"];

if (string.IsNullOrWhiteSpace(jwtKey))
{
    throw new InvalidOperationException("Jwt:Key is missing in configuration.");
}

// Redis
builder.Services.AddSingleton<IConnectionMultiplexer>(_ =>
    ConnectionMultiplexer.Connect(redisConn));

builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = redisConn;
});

// Database + services
// AccountDatabaseAccess removed - using AgeVerificationRepository now

// Identity Providers and Signicat Services are registered via extension
builder.Services.AddSignicatServices(builder.Configuration);

// Health Checks
builder.Services.AddHealthChecks()
    .AddSqlServer(builder.Configuration.GetConnectionString("CompanyConnection")!)
    .AddRedis(redisConn);

// ?? KAFKA  BuildingBlocks
builder.Services.AddSingleton<IKafkaProducer, KafkaProducer>();
builder.Services.AddSingleton<IKafkaConsumer, KafkaConsumer>();

// ?? Authentication: JWT Bearer (internally from ApiGateway)
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtKey!)),

            ValidateIssuer = false, // Internal service, trust gateway
            ValidateAudience = false,
            ValidateLifetime = false // Life time managed by gateway/token service
        };

        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                 // Optional debug logging
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();

var app = builder.Build();

// Swagger middleware  typisk kun i Development
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "IdentityService API v1");
    });
}

app.UseAuthentication();
app.UseAuthorization();

app.MapPrometheusScrapingEndpoint();
app.MapHealthChecks("/health");

// Initialize DB (Development Only)
if (app.Environment.IsDevelopment())
{
    using (var scope = app.Services.CreateScope())
    {
        var initializer = scope.ServiceProvider.GetService<IdentityService.Infrastructure.Persistence.DbInitializer>();
        if (initializer != null) 
        {
            await initializer.InitializeAsync();
        }
    }
}

// Initialize DB
using (var scope = app.Services.CreateScope())
{
    var initializer = scope.ServiceProvider.GetRequiredService<IdentityService.Infrastructure.Persistence.DbInitializer>();
    await initializer.InitializeAsync();
}

app.MapControllers();

app.Run();
