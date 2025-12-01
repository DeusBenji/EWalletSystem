using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using ApiGateway.Helpers;
using ApiGateway.Services;
using Microsoft.IdentityModel.Tokens;
using Yarp.ReverseProxy;

var builder = WebApplication.CreateBuilder(args);

// Bind JWT settings from appsettings.json
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("Jwt"));

// Add token generator
builder.Services.AddSingleton<IJwtTokenService, JwtTokenService>();

// YARP proxy
builder.Services
    .AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

// CORS for Blazor wallet
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowWallet", policy =>
    {
        policy
            .WithOrigins(
                "https://localhost:7160",
                "http://localhost:5160"
            )
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var app = builder.Build();

app.UseHttpsRedirection();
app.UseCors("AllowWallet");

//
// 🔥 YARP middleware der automatisk tilføjer JWT fra gateway til MitID-service
//
app.MapReverseProxy(proxyPipeline =>
{
    proxyPipeline.Use(async (context, next) =>
    {
        // Gælder kun for ruten /mitid/**
        var path = context.Request.Path.Value?.ToLower();

        if (path != null && path.StartsWith("/mitid"))
        {
            var tokenService = context.RequestServices.GetRequiredService<IJwtTokenService>();

            // Lav token rettet mod BachMitID
            string jwt = tokenService.CreateServiceToken(
                audience: "BachMitID",
                scope: "mitid.read"
            );

            // Sæt Authorization-header for intern servicekommunikation
            context.Request.Headers["Authorization"] = $"Bearer {jwt}";
        }

        await next();
    });
});

app.Run();
