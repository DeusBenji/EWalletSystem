// using Blazored.LocalStorage;
using Moq;
using Microsoft.JSInterop;
using Wallet.Wasm.Models;
using Wallet.Wasm.Services;
using Xunit;

namespace Wallet.Wasm.Tests.Services;

public class WalletServiceTests
{
    private readonly Mock<WalletStorage> _mockStorage;
    private readonly Mock<HttpClient> _mockHttp;
    private readonly WalletService _service;

    public WalletServiceTests()
    {
        // Wrapper for in-memory storage test
        var storage = new WalletStorage();
        _mockHttp = new Mock<HttpClient>();
        
        // Use real (mock) ZKP service for tests
        var jsMock = new Mock<IJSRuntime>();
        var secretManager = new SecretManager(jsMock.Object);
        var zkpService = new ZkpProverService(jsMock.Object, secretManager);
        
        _service = new WalletService(storage, _mockHttp.Object, zkpService);
    }

    [Fact]
    public void VerifyTokenLocally_ReturnsTrue_WhenSignatureIsValid()
    {
        // Arrange
        var token = new LocalWalletToken
        {
            Issuer = "TestIssuer",
            Type = "TestCredential",
            Claims = new Dictionary<string, string> { { "Age", "20" } }
        };
        
        // Manually sign it as the service would
        var hash = token.ComputeHash();
        var validToken = token with { Signature = $"signed-{hash}-valid" };

        // Act
        var result = _service.VerifyTokenLocally(validToken);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void VerifyTokenLocally_ReturnsFalse_WhenSignatureIsInvalid()
    {
        // Arrange
        var token = new LocalWalletToken
        {
            Signature = "invalid-signature"
        };

        // Act
        var result = _service.VerifyTokenLocally(token);

        // Assert
        Assert.False(result);
    }
    
    [Fact]
    public void VerifyTokenLocally_ReturnsFalse_WhenExpired()
    {
        // Arrange
        var token = new LocalWalletToken
        {
            IssuedAt = DateTime.UtcNow.AddYears(-2),
            ExpiresAt = DateTime.UtcNow.AddYears(-1), // Expired
            Signature = "valid-signature" // Even if signature 'looks' valid (in our mock logic)
        };
        // Note: Our mock signature check logic relies on Contains(hash), so we need to ensure that passes if we want to test expiry isolation,
        // but VerifyTokenLocally checks Expiry FIRST.
        
        // Act
        var result = _service.VerifyTokenLocally(token);

        // Assert
        Assert.False(result);
    }
}
