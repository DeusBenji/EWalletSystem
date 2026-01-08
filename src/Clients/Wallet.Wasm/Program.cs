using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Wallet.Wasm;
using Wallet.Wasm.Services;
using Blazored.LocalStorage;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp =>
{
    var gatewayBase = builder.Configuration["Gateway:BaseUrl"] ?? "";
    var frontendBase = new Uri(builder.HostEnvironment.BaseAddress);

    // Absolut URL (dev) fx "http://localhost:7005"
    if (Uri.TryCreate(gatewayBase, UriKind.Absolute, out var absolute))
    {
        return new HttpClient { BaseAddress = absolute };
    }

    // Relativ (docker/nginx) fx "/api"
    if (!string.IsNullOrWhiteSpace(gatewayBase))
    {
        var apiBase = new Uri(frontendBase, gatewayBase.TrimStart('/') + "/");
        return new HttpClient { BaseAddress = apiBase };
    }

    // Fallback
    return new HttpClient { BaseAddress = frontendBase };
});

builder.Services.AddBlazoredLocalStorage();
builder.Services.AddScoped<WalletStorage>();
builder.Services.AddScoped<WalletService>();
builder.Services.AddScoped<AdultClient>();
builder.Services.AddScoped<AccountClient>();

await builder.Build().RunAsync();
