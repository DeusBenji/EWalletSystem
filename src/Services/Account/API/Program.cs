using AccountService.API.Mapping;
using AutoMapper;
using Application.BusinessLogic;
using Application.Interfaces;
using Application.Mapping;
using Domain.Repositories;
using Infrastructure.Caching;
using Infrastructure.Kafka;
using Infrastructure.Persistence;
using Infrastructure.Security;
using Confluent.Kafka;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using StackExchange.Redis;

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

// ---------- Dependency Injection ----------

// Repositories (infra)
builder.Services.AddSingleton<IAccountRepository, AccountRepository>();

// Business logic / appl
builder.Services.AddScoped<IAccountService, Application.BusinessLogic.AccountService>();

// AutoMapper – loader alle profiler i samme assembly som AccountMappingProfile
// AutoMapper – registrer både Application- og API-profiler
builder.Services.AddAutoMapper(cfg =>
{
    cfg.AddProfile<AccountApplicationProfile>();
    cfg.AddProfile<AccountApiProfile>();
});
// Cross-cutting services
builder.Services.AddSingleton<IPasswordHasher, BCryptPasswordHasher>();
builder.Services.AddSingleton<BuildingBlocks.Contracts.Messaging.IKafkaProducer, BuildingBlocks.Kafka.KafkaProducer>();
builder.Services.AddSingleton<IKafkaProducer, AccountCreatedProducer>();
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

// Kafka consumer til MitIdVerified (BackgroundService)


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

app.UseAuthorization();

app.MapControllers();

app.Run();

