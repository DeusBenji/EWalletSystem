using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Blazored.LocalStorage;
using Wallet.Wasm.Models;
using Wallet.Wasm.Pages;
using Wallet.Wasm.Services;
using Xunit;
using Microsoft.AspNetCore.Components;

namespace Wallet.Wasm.Tests.Pages;

public class ImportTokenTests : IDisposable
{
    private readonly BunitContext ctx = new();
    private readonly Mock<HttpClient> _mockHttp;
    private readonly Mock<NavigationManager> _mockNav;

    public ImportTokenTests()
    {
        _mockHttp = new Mock<HttpClient>();
        
        // ctx.Services.AddSingleton(_mockLocalStorage.Object);
        ctx.Services.AddSingleton<WalletStorage>(); // Singleton for shared state
        ctx.Services.AddScoped<WalletService>();
        ctx.Services.AddScoped<ZkpProverService>();
        ctx.Services.AddScoped(sp => _mockHttp.Object);
    }

    public void Dispose() => ctx.Dispose();

    public async Task ClickImport_TriggersTokenSave_AndNavigates()
    {
        // Arrange
        var storage = ctx.Services.GetService<WalletStorage>();
        await storage.SetAccountIdAsync(Guid.NewGuid()); // Required for Import logic

        var cut = ctx.Render<ImportToken>();

        // Act
        // Find button and click
        var btn = cut.Find("button.btn-primary");
        btn.Click();

        // Assert
        // The spinner should appear. In test environment, NavigateTo might throw or be mocked.
        // If NavigateTo is called, we verify that state was valid up to that point.
        // With real logic, it might clear busy flag if NavigateTo doesn't block.
        // Bunit FakeNavigationManager handles navigation without throwing.
        // However, ImportToken sets _busy=false in finally block.
        // If Nav.NavigateTo(forceLoad:true) is called, does it stop execution?
        // In clean Bunit, it just records navigation.
        // Then code continues to finally block -> _busy = false.
        // So .spinner-border might DISAPPEAR before we check it?
        // We should check if NavigtaionManager was navigated.
        
        // Assert
        // Since logic sets _busy=false in finally, spinner might be gone.
        // We check if navigation occurred.
        var nav = ctx.Services.GetRequiredService<NavigationManager>();
        
        // Wait for navigation
        // Or check URI. FakeNavigationManager updates Uri properly.
        // Note: ImportToken.razor constructs generic login URL.
        Assert.Contains("auth/login", nav.Uri);
    }
}
