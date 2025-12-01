using AutoMapper;
using Confluent.Kafka;
using StackExchange.Redis;
using Application.BusinessLogic;
using Application.Interfaces;
using Domain.Repositories;
using Infrastructure.Blockchain;
using Infrastructure.Kafka;
using Infrastructure.Redis;
using Infrastructure.Persistence;
using TokenService.Application.Interfaces;
using TokenService.Infrastructure.Signing;

var builder = WebApplication.CreateBuilder(args);

// -----------------------------------------------------
// ASP.NET Core basics
// -----------------------------------------------------
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// -----------------------------------------------------
// Application layer
// -----------------------------------------------------
builder.Services.AddScoped<ITokenIssuanceService, TokenIssuanceService>();

// -----------------------------------------------------
// Domain repositories (interfaces i Domain, impl i Infrastructure)
// -----------------------------------------------------
builder.Services.AddSingleton<IAttestationRepository, AttestationRepository>();
builder.Services.AddSingleton<IAccountAgeStatusRepository, AccountAgeStatusRepository>();

// -----------------------------------------------------
// Infrastructure: Redis Cache (MANGLEDE!)
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
// Infrastructure: messaging (Kafka)
// -----------------------------------------------------
builder.Services.AddSingleton<Shared.Infrastructure.Kafka.IKafkaProducer, Shared.Infrastructure.Kafka.KafkaProducer>();
builder.Services.AddSingleton<IKafkaEventProducer, KafkaEventProducer>();
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

app.UseHttpsRedirection();
app.MapControllers();
app.Run();

