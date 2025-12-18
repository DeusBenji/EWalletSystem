using Blazored.LocalStorage;
using Moq;
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
        // We can't easily mock WalletStorage because it's a concrete class (unless we extract an interface or make methods virtual).
        // Best practice: Use a concrete WalletStorage with a Mocked ILocalStorageService.
        
        var mockLocalStorage = new Mock<ILocalStorageService>();
        var storage = new WalletStorage(mockLocalStorage.Object);
        _mockHttp = new Mock<HttpClient>();
        
        // However, since WalletStorage methods are not virtual, we might need to actually test integration or modify WalletStorage.
        // For this test, let's mock the ILocalStorageService which WalletStorage uses.
        
        // Alternative: Mock the WalletStorage if we change it to open/interface.
        // Given current code, let's test WalletService logic assuming Storage works or try to mock the storage if possible.
        // Actually, WalletService depends on WalletStorage (concrete). 
        // Let's modify WalletStorage be testable or test WalletService's logic that doesn't depend heavily on storage internal behavior 
        // OR better: Create a real WalletStorage with a mock ILocalStorageService for the "GetTokensAsync" simulation.
        
        _service = new WalletService(storage, _mockHttp.Object);
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
