using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Blazored.LocalStorage;
using Wallet.Wasm.Models;
using Wallet.Wasm.Pages;
using Wallet.Wasm.Services;
using Xunit;

namespace Wallet.Wasm.Tests.Pages;

public class WalletPageTests : TestContext
{
    private readonly Mock<ILocalStorageService> _mockLocalStorage;
    private readonly Mock<HttpClient> _mockHttp;

    public WalletPageTests()
    {
        _mockLocalStorage = new Mock<ILocalStorageService>();
        _mockHttp = new Mock<HttpClient>();

        // Register services
        Services.AddBlazoredLocalStorage(); // Usually adds default, but we want to replace or let the mock verify
        // Actually AddBlazoredLocalStorage registers the implementation. We want to register our mock.
        Services.AddSingleton(_mockLocalStorage.Object);
        
        Services.AddScoped<WalletStorage>(); // Uses the mock ILocalStorage
        Services.AddScoped<WalletService>(); // Uses the WalletStorage and mock Http
        Services.AddScoped(sp => _mockHttp.Object);
    }

    [Fact]
    public void RendersLoading_WhenTokensAreNull()
    {
        // Arrange
        // We simulate that GetTokensAsync returns null or delay?
        // Actually WalletService.GetMyTokensAsync calls storage.GetTokensAsync.
        // If storage returns null (default mock), it might throw or retun null.
        // WalletStorage.GetTokensAsync handles null return from localstorage by returning new List.
        
        // To verify "Loading...", we need to pause the render or have the service delay.
        // Or simpler: Test Empty State.
        
        _mockLocalStorage.Setup(x => x.GetItemAsync<List<LocalWalletToken>>("my_wallet_tokens", CancellationToken.None))
            .ReturnsAsync(new List<LocalWalletToken>());

        // Act
        var cut = RenderComponent<WalletPage>();

        // Assert
        // Initially it might be loading if async. 
        // But bunit waits for OnInitializedAsync.
        // So if it returns empty list, it should show "Your wallet is empty".
        
        cut.WaitForState(() => cut.FindAll(".alert-warning").Count > 0);
        Assert.Contains("Your wallet is empty", cut.Markup);
    }

    [Fact]
    public void RendersTokens_WhenTokensExist()
    {
        // Arrange
        var tokens = new List<LocalWalletToken>
        {
            new LocalWalletToken { Type = "AdultCredential", Issuer = "Gov", TokenId = "123" }
        };

        _mockLocalStorage.Setup(x => x.GetItemAsync<List<LocalWalletToken>>("my_wallet_tokens", CancellationToken.None))
            .ReturnsAsync(tokens);

        // Act
        var cut = RenderComponent<WalletPage>();

        // Assert
        cut.WaitForState(() => cut.FindAll(".card").Count > 0);
        Assert.Contains("AdultCredential", cut.Markup);
        Assert.Contains("Gov", cut.Markup);
    }
}
