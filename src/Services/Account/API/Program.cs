using AccountService.API.BackgroundServices;
using AccountService.API.Mapping;
using AccountService.API.Security;
using Application.BusinessLogic;
using Application.Interfaces;
using Application.Mapping;
using BuildingBlocks.Kafka;
using Domain.Repositories;
using Infrastructure.Caching;
using Infrastructure.Persistence;
using Infrastructure.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using OpenTelemetry.Metrics;
using StackExchange.Redis;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<HostOptions>(o =>
{
    o.BackgroundServiceExceptionBehavior = BackgroundServiceExceptionBehavior.Ignore;
});

// Controllers + ProblemDetails (pæne RFC7807-fejl)
builder.Services.AddControllers();
builder.Services.AddProblemDetails();

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "Account Service API", Version = "v1" });
});

// Metrics
builder.Services.AddOpenTelemetry()
    .WithMetrics(metrics =>
    {
        metrics.AddPrometheusExporter();
        metrics.AddMeter("Microsoft.AspNetCore.Hosting", "Microsoft.AspNetCore.Server.Kestrel");
    });

// ----------------- AUTH (JWT Bearer) -----------------
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

// ---------- Dependency Injection ----------

// Repositories (infra)
builder.Services.AddSingleton<IAccountRepository, AccountRepository>();
builder.Services.AddSingleton<DbInitializer>();

// Business logic / application services
builder.Services.AddScoped<IAccountService, Application.BusinessLogic.AccountService>();

// → Tilføj MitIdVerifiedService
builder.Services.AddScoped<IMitIdVerifiedService, MitIdVerifiedService>();

// AutoMapper profiler
builder.Services.AddAutoMapper(cfg =>
{
    cfg.AddProfile<AccountApplicationProfile>();
    cfg.AddProfile<AccountApiProfile>();
});

// Cross-cutting services
builder.Services.AddSingleton<IPasswordHasher, BCryptPasswordHasher>();
builder.Services.AddSingleton<JwtTokenService>();
builder.Services.AddSingleton<BuildingBlocks.Contracts.Messaging.IKafkaProducer, BuildingBlocks.Kafka.KafkaProducer>();
builder.Services.AddSingleton<BuildingBlocks.Contracts.Messaging.IKafkaConsumer, BuildingBlocks.Kafka.KafkaConsumer>();

// Redis connection
builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
{
    var configuration = builder.Configuration.GetConnectionString("Redis")
                        ?? "localhost:6379"; // dev-default

    return ConnectionMultiplexer.Connect(configuration);
});

// Cache
builder.Services.AddSingleton<IAccountCache, AccountCache>();

// Kafka Background Service → MitIdVerifiedConsumer
builder.Services.AddHostedService<MitIdVerifiedConsumer>();

// CORS (åben kun i Development)
builder.Services.AddCors(options =>
{
    options.AddPolicy("DevCors", policy =>
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader());
});

// Logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

var app = builder.Build();

// ---------- Pipeline ----------
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.UseCors("DevCors");
}

app.UseHttpsRedirection();

// 🔐 auth middleware order matters
app.UseAuthentication();
app.UseAuthorization();

app.MapPrometheusScrapingEndpoint();


// Initialize DB
using (var scope = app.Services.CreateScope())
{
    var initializer = scope.ServiceProvider.GetRequiredService<DbInitializer>();
    await initializer.InitializeAsync();
}

app.MapControllers();

app.Run();
