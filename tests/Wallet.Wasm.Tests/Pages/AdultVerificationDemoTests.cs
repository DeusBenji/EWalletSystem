using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Blazored.LocalStorage;
using Wallet.Wasm.Models;
using Wallet.Wasm.Pages;
using Wallet.Wasm.Services;
using Xunit;

namespace Wallet.Wasm.Tests.Pages;

public class AdultVerificationDemoTests : TestContext
{
    private readonly Mock<ILocalStorageService> _mockLocalStorage;
    private readonly Mock<HttpClient> _mockHttp;

    public AdultVerificationDemoTests()
    {
        _mockLocalStorage = new Mock<ILocalStorageService>();
        _mockHttp = new Mock<HttpClient>();

        Services.AddSingleton(_mockLocalStorage.Object);
        Services.AddScoped<WalletStorage>();
        Services.AddScoped<WalletService>();
        Services.AddScoped<AdultClient>(); // Component uses AdultClient
        Services.AddScoped(sp => _mockHttp.Object);
    }

    [Fact]
    public void RendersWarning_WhenNoTokenFound()
    {
        // Arrange
        _mockLocalStorage.Setup(x => x.GetItemAsync<List<LocalWalletToken>>("my_wallet_tokens", CancellationToken.None))
            .ReturnsAsync(new List<LocalWalletToken>()); // Empty

        // Act
        var cut = RenderComponent<AdultVerificationDemo>();

        // Assert
        cut.WaitForState(() => cut.FindAll(".alert-warning").Count > 0);
        Assert.Contains("Ingen token fundet", cut.Markup);
    }

    [Fact]
    public void RendersDemo_WhenTokenExists()
    {
        // Arrange
        var token = new LocalWalletToken 
        { 
            Type = "AdultCredential", 
            TokenId = "123",
            // We need signature logic to pass for "ClientSideAdult" to be true, 
            // but just rendering the page (not the Success alert) is enough for this test.
        };
        
        _mockLocalStorage.Setup(x => x.GetItemAsync<List<LocalWalletToken>>("my_wallet_tokens", CancellationToken.None))
            .ReturnsAsync(new List<LocalWalletToken> { token });

        // Act
        var cut = RenderComponent<AdultVerificationDemo>();

        // Assert
        cut.WaitForState(() => cut.FindAll(".card").Count > 0);
        Assert.Contains("Client-side verification", cut.Markup);
    }
}
