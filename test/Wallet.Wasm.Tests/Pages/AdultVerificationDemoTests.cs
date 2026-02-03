using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Blazored.LocalStorage;
using Wallet.Wasm.Models;
using Wallet.Wasm.Pages;
using Wallet.Wasm.Services;
using Xunit;

namespace Wallet.Wasm.Tests.Pages;

public class AdultVerificationDemoTests : IDisposable
{
    private readonly BunitContext ctx = new();
    private readonly Mock<HttpClient> _mockHttp;

    public AdultVerificationDemoTests()
    {
        _mockHttp = new Mock<HttpClient>();

        // ctx.Services.AddSingleton(_mockLocalStorage.Object); // Removed
        ctx.Services.AddSingleton<WalletStorage>(); // Singleton for shared state
        ctx.Services.AddScoped<WalletService>();
        ctx.Services.AddScoped<AdultClient>();
        ctx.Services.AddScoped<ZkpProverService>();
        ctx.Services.AddScoped(sp => _mockHttp.Object);
    }

    public void Dispose() => ctx.Dispose();

    [Fact]
    public void RendersWarning_WhenNoTokenFound()
    {
        // Arrange
        // Empty storage by default.

        // Act
        var cut = ctx.Render<AdultVerificationDemo>();

        // Assert
        cut.WaitForState(() => cut.FindAll(".alert-warning").Count > 0);
        Assert.Contains("Ingen token fundet", cut.Markup);
    }

    [Fact]
    public async Task RendersDemo_WhenTokenExists()
    {
        // Arrange
        var token = new LocalWalletToken 
        { 
            Type = "AdultCredential", 
            TokenId = "123",
        };
        // Ensure signature is valid for VerifyTokenLocally
        var hash = token.ComputeHash();
        token = token with { Signature = $"signed-{hash}-valid" };
        
        var storage = ctx.Services.GetService<WalletStorage>();
        await storage.SaveTokenAsync(token);

        // Act
        var cut = ctx.Render<AdultVerificationDemo>();

        // Assert
        cut.WaitForState(() => cut.FindAll(".card").Count > 0);
        Assert.Contains("Client-side verification", cut.Markup);
    }
}
