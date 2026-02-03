using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Blazored.LocalStorage;
using Wallet.Wasm.Models;
using Wallet.Wasm.Pages;
using Wallet.Wasm.Services;
using System.Net.Http.Json;
using Moq.Protected;
using Xunit;

namespace Wallet.Wasm.Tests.Pages;

public class AdultVerificationDemoTests : IDisposable
{
    private readonly BunitContext ctx = new();
    private readonly Mock<HttpMessageHandler> _mockHandler;
    private readonly HttpClient _httpClient;

    public AdultVerificationDemoTests()
    {
        _mockHandler = new Mock<HttpMessageHandler>();
        _httpClient = new HttpClient(_mockHandler.Object) { BaseAddress = new Uri("http://localhost") };

        ctx.Services.AddSingleton<WalletStorage>();
        ctx.Services.AddScoped<WalletService>();
        ctx.Services.AddScoped<ZkpProverService>();
        ctx.Services.AddScoped(sp => _httpClient);
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
            Issuer = "MitID"
        };
        // Valid signature for local verify
        var hash = token.ComputeHash();
        token = token with { Signature = $"signed-{hash}-valid" };
        
        var storage = ctx.Services.GetService<WalletStorage>();
        await storage.SaveTokenAsync(token);

        // Act
        var cut = ctx.Render<AdultVerificationDemo>();

        // Assert
        cut.WaitForState(() => cut.FindAll(".card").Count > 0);
        // Based on recent Razor view (Step 1575), looking for text present in the card
        Assert.Contains("voksenindhold", cut.Markup.ToLower()); 
        Assert.Contains("client-side verification", cut.Markup.ToLower());
    }

    [Fact]
    public async Task ClickServerVerify_CallsZkp_AndUpdatesUi()
    {
        // Arrange
        var token = new LocalWalletToken { Type = "AdultCredential", TokenId = "zkp-test" };
        var storage = ctx.Services.GetService<WalletStorage>();
        await storage.SaveTokenAsync(token);

        // Mock API response for Verify
        var verifyResponse = new VerifyCredentialResponse { Success = true };
        _mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(r => r.Method == HttpMethod.Post && r.RequestUri.ToString().Contains("verify")),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = System.Net.HttpStatusCode.OK,
                Content = JsonContent.Create(verifyResponse)
            });

        var cut = ctx.Render<AdultVerificationDemo>();
        cut.WaitForState(() => cut.FindAll(".card").Count > 0);

        // Act
        var btn = cut.Find("button.btn-secondary"); // "Kør server-side check"
        btn.Click();

        // Assert
        cut.WaitForState(() => cut.Markup.Contains("alert-success"));
        Assert.Contains("Verifikation succes", cut.Markup);
        Assert.Contains("bevist via ZKP", cut.Markup);
    }
}
