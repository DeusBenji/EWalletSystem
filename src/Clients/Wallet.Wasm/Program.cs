using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Wallet.Wasm;
using Wallet.Wasm.Services;
using Blazored.LocalStorage;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

builder.Services.AddBlazoredLocalStorage();
builder.Services.AddScoped<WalletStorage>();
builder.Services.AddScoped<WalletService>();
builder.Services.AddScoped<AdultClient>();
builder.Services.AddScoped<AccountClient>();

await builder.Build().RunAsync();
