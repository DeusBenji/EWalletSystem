using ApiGateway.Helpers;
using ApiGateway.Services;

var builder = WebApplication.CreateBuilder(args);

// Bind JWT settings from appsettings.json
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("Jwt"));

// Add token generator
builder.Services.AddSingleton<IJwtTokenService, JwtTokenService>();

// YARP proxy
builder.Services
    .AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

// CORS for Wallet (dev-friendly: allow any localhost port)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowWallet", policy =>
    {
        policy
            .SetIsOriginAllowed(origin =>
                origin.StartsWith("http://localhost:") ||
                origin.StartsWith("https://localhost:"))
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseCors("AllowWallet");

//
// 🔥 YARP middleware der automatisk tilføjer JWT fra gateway til interne services
//
app.MapReverseProxy(proxyPipeline =>
{
    proxyPipeline.Use(async (context, next) =>
    {
        var path = context.Request.Path.Value?.ToLowerInvariant();

        if (!string.IsNullOrWhiteSpace(path))
        {
            var tokenService = context.RequestServices.GetRequiredService<IJwtTokenService>();

            // MitID
            if (path.StartsWith("/mitid"))
            {
                string jwt = tokenService.CreateServiceToken(
                    audience: "BachMitID",
                    scope: "mitid.read"
                );

                context.Request.Headers["Authorization"] = $"Bearer {jwt}";
            }
            // Account
            else if (path.StartsWith("/account"))
            {
                string jwt = tokenService.CreateServiceToken(
                    audience: "AccountService",
                    scope: "account.read"
                );

                context.Request.Headers["Authorization"] = $"Bearer {jwt}";
            }
            // Token
            else if (path.StartsWith("/token"))
            {
                string jwt = tokenService.CreateServiceToken(
                    audience: "TokenService",
                    scope: "token.issue"
                );

                context.Request.Headers["Authorization"] = $"Bearer {jwt}";
            }
            // Validation
            else if (path.StartsWith("/validation"))
            {
                string jwt = tokenService.CreateServiceToken(
                    audience: "ValidationService",
                    scope: "validation.verify"
                );

                context.Request.Headers["Authorization"] = $"Bearer {jwt}";
            }
        }

        await next();
    });
});

app.Run();
