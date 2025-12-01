using StackExchange.Redis;
using System.Reflection;
using Application.BusinessLogic;
using Application.Interfaces;
using Domain.Repositories;
using Infrastructure.Blockchain;
using Infrastructure.Caching;
using Infrastructure.Jwt;
using Infrastructure.Kafka;
using Infrastructure.Persistence;
using Infrastructure.Persistence;
using Infrastructure.Security;
using ValidationService.Application.Interfaces;
using ValidationService.Infrastructure.Jwt;

var builder = WebApplication.CreateBuilder(args);

// -------------------------------------------------------
// Basic ASP.NET setup
// -------------------------------------------------------
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    // Hvis du vil have XML-kommentarer med:
    var xmlFilename = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFilename);
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath, includeControllerXmlComments: true);
    }
});

// AutoMapper – scan Application/Mapping (og API hvis du vil)
builder.Services.AddAutoMapper(cfg =>
{
    cfg.AddMaps(new[]
    {
        "ValidationService.Application",
        "ValidationService.API"
    });
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
// Infrastructure – Kafka producer
// -------------------------------------------------------
builder.Services.AddSingleton<Shared.Infrastructure.Kafka.IKafkaProducer, Shared.Infrastructure.Kafka.KafkaProducer>();
builder.Services.AddSingleton<IKafkaEventProducer, KafkaEventProducer>();

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
        options.RoutePrefix = string.Empty; // swagger på roden
    });
}

app.UseHttpsRedirection();

app.MapControllers();

app.Run();

