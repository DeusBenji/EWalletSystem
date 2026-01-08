using AutoMapper;
using BachMitID;
using BachMitID.Application.BusinessLogicLayer;
using BachMitID.Application.BusinessLogicLayer.Interface;
using BachMitID.Domain.Interfaces;
using BachMitID.Infrastructure.Cache;
using BachMitID.Infrastructure.Databaselayer;

using BuildingBlocks.Contracts.Messaging;
using BuildingBlocks.Kafka;
using BachMitID.BackgroundServices;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
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
        Title = "BachMitID API",
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
    });

var redisConn = builder.Configuration.GetConnectionString("RedisConnection")
    ?? throw new InvalidOperationException("RedisConnection is missing from appsettings.json");

var authSection = builder.Configuration.GetSection("Authentication");
var authority = authSection["Authority"];
var clientId = authSection["ClientId"];
var clientSecret = authSection["ClientSecret"];
var callbackPath = authSection["CallbackPath"] ?? "/signin-oidc";

// ✅ Public origin (til gateway prefix) — fx "http://localhost:7005/mitid"
var publicOrigin = builder.Configuration["PublicOrigin"];

// 🔐 JWT settings til gateway-tokens
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

// Database + services
builder.Services.AddScoped<IAccDbAccess, AccountDatabaseAccess>();
builder.Services.AddScoped<IMitIdDbAccess, MitIdAccountDatabaseAccess>();
builder.Services.AddScoped<IMitIdAccountService, MitIdAccountService>();
builder.Services.AddSingleton<BachMitID.Infrastructure.Persistence.DbInitializer>();

// Account sync service (til consumer)
builder.Services.AddScoped<IAccountSyncService, AccountSyncService>();

// Cache
builder.Services.AddSingleton<IMitIdAccountCache, MitIdAccountCache>();

// AutoMapper
builder.Services.AddSingleton<IMapper>(sp =>
{
    var loggerFactory = sp.GetRequiredService<ILoggerFactory>();

    var config = new MapperConfiguration(cfg =>
    {
        cfg.AddProfile<MappingProfile>();
    }, loggerFactory);

    return config.CreateMapper();
});

// 🔥 KAFKA — BuildingBlocks
builder.Services.AddSingleton<IKafkaProducer, KafkaProducer>();
builder.Services.AddSingleton<IKafkaConsumer, KafkaConsumer>();

// 🧵 Background Kafka consumer(s)
builder.Services.AddHostedService<AccountCreatedConsumer>();

// 🔐 Authentication: Cookies + OIDC + JWT Bearer (fra gateway)
builder.Services
    .AddAuthentication(options =>
    {
        // Standard til web-login via MitID (cookies + OIDC)
        options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = "oidc";
    })
    .AddCookie()
    .AddOpenIdConnect("oidc", options =>
    {
        options.Authority = authority;
        options.ClientId = clientId;
        options.ClientSecret = clientSecret;
        options.ResponseType = OpenIdConnectResponseType.Code;
        options.SaveTokens = true;

        options.Scope.Clear();
        options.Scope.Add("openid");
        options.Scope.Add("profile");
        options.Scope.Add("eighteen-or-older");

        options.ClaimActions.MapJsonKey("eighteen_or_older", "eighteen_or_older");
        options.ClaimActions.MapJsonKey("eighteen-or-older", "eighteen-or-older");

        options.CallbackPath = callbackPath;
        options.GetClaimsFromUserInfoEndpoint = true;
        options.RequireHttpsMetadata = true;

       
    })
    // 🔽 JWT Bearer til interne kald fra ApiGateway
    .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtKey!)),

            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = false
        };

        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var authHeader = context.Request.Headers["Authorization"].ToString();
                Console.WriteLine($"[BachMitID][JWT] Authorization header: {authHeader}");
                return Task.CompletedTask;
            },
            OnAuthenticationFailed = context =>
            {
                Console.WriteLine($"[BachMitID][JWT] Authentication failed: {context.Exception.Message}");
                return Task.CompletedTask;
            },
            // 🚫 Forhindrer redirect til OIDC på API-kald (returnerer ren 401 JSON)
            OnChallenge = context =>
            {
                context.HandleResponse();

                if (!context.Response.HasStarted)
                {
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    context.Response.ContentType = "application/json";
                    return context.Response.WriteAsync("{\"error\":\"Unauthorized - invalid or missing token\"}");
                }

                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();

var app = builder.Build();

// Swagger middleware – typisk kun i Development
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "BachMitID API v1");
    });
}

app.UseAuthentication();
app.UseAuthorization();

app.MapPrometheusScrapingEndpoint();

// Initialize DB
using (var scope = app.Services.CreateScope())
{
    var initializer = scope.ServiceProvider.GetRequiredService<BachMitID.Infrastructure.Persistence.DbInitializer>();
    await initializer.InitializeAsync();
}

app.MapControllers();

app.Run();
