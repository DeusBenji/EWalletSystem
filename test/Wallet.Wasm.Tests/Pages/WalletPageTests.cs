using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Blazored.LocalStorage;
using Wallet.Wasm.Models;
using Wallet.Wasm.Pages;
using Wallet.Wasm.Services;
using Xunit;

namespace Wallet.Wasm.Tests.Pages;

public class WalletPageTests : IDisposable
{
    private readonly BunitContext ctx = new();
    private readonly Mock<HttpClient> _mockHttp;

    public WalletPageTests()
    {
        _mockHttp = new Mock<HttpClient>();

        // Register services
        // ctx.Services.AddBlazoredLocalStorage(); // Removed
        
        ctx.Services.AddSingleton<WalletStorage>(); // Singleton to ensure shared state in test
        ctx.Services.AddScoped<WalletService>(); 
        
        // Mock JSRuntime (needed for SecretManager)
        ctx.Services.AddSingleton(new Mock<Microsoft.JSInterop.IJSRuntime>().Object);
        
        // Add SecretManager (needed for ZkpProverService)
        ctx.Services.AddScoped<SecretManager>();

        ctx.Services.AddScoped<IZkpProverService, ZkpProverService>(); 
        ctx.Services.AddScoped(sp => _mockHttp.Object);
    }

    public void Dispose() => ctx.Dispose();

    [Fact]
    public void RendersLoading_WhenTokensAreNull()
    {
        // Arrange
        // In-memory storage starts empty, so GetTokensAsync returns empty list immediately.
        // The original test expected "Loading..." or similar? 
        // Actually the previous test asserted "Your wallet is empty". 
        // Since in-memory is empty by default, this matches.
        
        // Act
        var cut = ctx.Render<WalletPage>();

        // Assert
        cut.WaitForState(() => cut.FindAll(".alert-warning").Count > 0);
        Assert.Contains("Your wallet is empty", cut.Markup);
    }

    [Fact]
    public async Task RendersTokens_WhenTokensExist()
    {
        // Arrange
        var storage = ctx.Services.GetService<WalletStorage>();
        var token = new LocalWalletToken { Type = "AdultCredential", Issuer = "Gov", TokenId = "123" };
        await storage.SaveTokenAsync(token);

        // Act
        var cut = ctx.Render<WalletPage>();

        // Assert
        cut.WaitForState(() => cut.FindAll(".card").Count > 0);
        Assert.Contains("AdultCredential", cut.Markup);
        Assert.Contains("Gov", cut.Markup);
    }
}
