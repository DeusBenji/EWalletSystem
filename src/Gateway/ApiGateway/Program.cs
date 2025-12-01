using Yarp.ReverseProxy;

var builder = WebApplication.CreateBuilder(args);

// YARP: læs proxy-konfiguration fra appsettings.json -> "ReverseProxy"
builder.Services
    .AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

// CORS – så din Blazor wallet og andre klienter kan kalde gatewayen
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowWallet", policy =>
    {
        policy
            .WithOrigins(
                "https://localhost:7160", // Blazor WASM (https) – ret til dine reelle porte
                "http://localhost:5160"   // Blazor WASM (http)
            )
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var app = builder.Build();

app.UseHttpsRedirection();

// senere kan du smide auth her:
// app.UseAuthentication();
// app.UseAuthorization();

app.UseCors("AllowWallet");

// Her tager YARP over og håndterer alle routes, vi definerer i appsettings.json
app.MapReverseProxy();

app.Run();
