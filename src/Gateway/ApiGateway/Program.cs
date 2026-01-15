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
        var rawPath = context.Request.Path.Value ?? "";
        var path = rawPath.ToLowerInvariant();

        // ✅ Support både "/api/xxx" og "/xxx"
        // Så vi kan matche stabilt uanset routes
        var normalized = path.StartsWith("/api/")
            ? path.Substring(4)   // fjerner "/api"
            : path;

        var tokenService = context.RequestServices.GetRequiredService<IJwtTokenService>();

        // MitID
        if (normalized.StartsWith("/mitid"))
        {
            var jwt = tokenService.CreateServiceToken(
                audience: "BachMitID",
                scope: "mitid.read"
            );

            context.Request.Headers["Authorization"] = $"Bearer {jwt}";
        }
        // Account
        else if (normalized.StartsWith("/account"))
        {
            var jwt = tokenService.CreateServiceToken(
                audience: "AccountService",
                scope: "account.read"
            );

            context.Request.Headers["Authorization"] = $"Bearer {jwt}";
        }
        // Token
        else if (normalized.StartsWith("/token"))
        {
            var jwt = tokenService.CreateServiceToken(
                audience: "TokenService",
                scope: "token.issue"
            );

            context.Request.Headers["Authorization"] = $"Bearer {jwt}";
        }
        // ✅ Validation
        else if (normalized.StartsWith("/validation"))
        {
            var jwt = tokenService.CreateServiceToken(
                audience: "ValidationService",
                scope: "validation.verify"
            );

            context.Request.Headers["Authorization"] = $"Bearer {jwt}";
        }

        await next();
    });
});

app.Run();
