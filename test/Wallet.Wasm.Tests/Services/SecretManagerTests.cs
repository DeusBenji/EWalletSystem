using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using Moq;
using Wallet.Wasm.Services;
using Xunit;

namespace Wallet.Wasm.Tests.Services;

public class SecretManagerTests : TestContext
{
    private readonly Mock<IJSRuntime> _mockJs;
    private readonly SecretManager _secretManager;

    public SecretManagerTests()
    {
        _mockJs = new Mock<IJSRuntime>();
        Services.AddSingleton(_mockJs.Object);
        _secretManager = new SecretManager(_mockJs.Object);
    }

    [Fact]
    public async Task GetOrCreateSecretAsync_WhenSecretExists_ReturnsExistingSecret()
    {
        // Arrange
        var existingSecret = "a1b2c3d4e5f6789012345678901234567890123456789012345678901234abcd";
        _mockJs.Setup(js => js.InvokeAsync<string>("secretManager.getOrCreateSecret", It.IsAny<object[]>()))
            .ReturnsAsync(existingSecret);

        // Act
        var result = await _secretManager.GetOrCreateSecretAsync();

        // Assert
        Assert.Equal(existingSecret, result);
        _mockJs.Verify(js => js.InvokeAsync<string>("secretManager.getOrCreateSecret", It.IsAny<object[]>()), Times.Once);
    }

    [Fact]
    public async Task GetOrCreateSecretAsync_CachesSecret_OnlyCallsJsOnce()
    {
        // Arrange
        var secret = "a1b2c3d4e5f6789012345678901234567890123456789012345678901234abcd";
        _mockJs.Setup(js => js.InvokeAsync<string>("secretManager.getOrCreateSecret", It.IsAny<object[]>()))
            .ReturnsAsync(secret);

        // Act
        var result1 = await _secretManager.GetOrCreateSecretAsync();
        var result2 = await _secretManager.GetOrCreateSecretAsync();

        // Assert
        Assert.Equal(secret, result1);
        Assert.Equal(secret, result2);
        _mockJs.Verify(js => js.InvokeAsync<string>("secretManager.getOrCreateSecret", It.IsAny<object[]>()), Times.Once);
    }

    [Fact]
    public async Task GetSecretAsync_WhenNoSecret_ReturnsNull()
    {
        // Arrange
        _mockJs.Setup(js => js.InvokeAsync<string?>("secretManager.getSecret", It.IsAny<object[]>()))
            .ReturnsAsync((string?)null);

        // Act
        var result = await _secretManager.GetSecretAsync();

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task ComputeCommitmentAsync_CallsPoseidonHash()
    {
        // Arrange
        var secret = "a1b2c3d4e5f6789012345678901234567890123456789012345678901234abcd";
        var expectedCommitment = "12345678901234567890123456789012345678901234567890123456789012345";
        
        _mockJs.Setup(js => js.InvokeAsync<string>("secretManager.getOrCreateSecret", It.IsAny<object[]>()))
            .ReturnsAsync(secret);
        _mockJs.Setup(js => js.InvokeAsync<string>("poseidon.computeCommitment", It.Is<object[]>(args => args[0].ToString() == secret)))
            .ReturnsAsync(expectedCommitment);

        // Act
        var result = await _secretManager.ComputeCommitmentAsync();

        // Assert
        Assert.Equal(expectedCommitment, result);
        _mockJs.Verify(js => js.InvokeAsync<string>("poseidon.computeCommitment", It.Is<object[]>(args => args[0].ToString() == secret)), Times.Once);
    }

    [Fact]
    public async Task ComputeChallengeHashAsync_CallsPoseidonHash()
    {
        // Arrange
        var challenge = "test-challenge-123";
        var expectedHash = "98765432109876543210987654321098765432109876543210987654321098765";
        
        _mockJs.Setup(js => js.InvokeAsync<string>("poseidon.computeChallengeHash", It.Is<object[]>(args => args[0].ToString() == challenge)))
            .ReturnsAsync(expectedHash);

        // Act
        var result = await _secretManager.ComputeChallengeHashAsync(challenge);

        // Assert
        Assert.Equal(expectedHash, result);
    }

    [Fact]
    public async Task ComputePolicyHashAsync_CallsPoseidonHash()
    {
        // Arrange
        var policyId = "age_over_18";
        var expectedHash = "11111111111111111111111111111111111111111111111111111111111111111";
        
        _mockJs.Setup(js => js.InvokeAsync<string>("poseidon.computePolicyHash", It.Is<object[]>(args => args[0].ToString() == policyId)))
            .ReturnsAsync(expectedHash);

        // Act
        var result = await _secretManager.ComputePolicyHashAsync(policyId);

        // Assert
        Assert.Equal(expectedHash, result);
    }

    [Fact]
    public async Task DeleteSecretAsync_ClearsCacheAndCallsJs()
    {
        // Arrange
        var secret = "a1b2c3d4e5f6789012345678901234567890123456789012345678901234abcd";
        _mockJs.Setup(js => js.InvokeAsync<string>("secretManager.getOrCreateSecret", It.IsAny<object[]>()))
            .ReturnsAsync(secret);
        
        await _secretManager.GetOrCreateSecretAsync(); // Cache the secret

        _mockJs.Setup(js => js.InvokeAsync<Microsoft.JSInterop.Infrastructure.IJSVoidResult>("secretManager.deleteSecret", It.IsAny<object[]>()))
            .Returns(new ValueTask<Microsoft.JSInterop.Infrastructure.IJSVoidResult>(default(Microsoft.JSInterop.Infrastructure.IJSVoidResult)!));

        // Act
        await _secretManager.DeleteSecretAsync();

        // Assert
        _mockJs.Verify(js => js.InvokeAsync<Microsoft.JSInterop.Infrastructure.IJSVoidResult>("secretManager.deleteSecret", It.IsAny<object[]>()), Times.Once);
    }

    [Fact]
    public void ClearCache_ClearsInMemoryCache()
    {
        // Arrange
        var secret = "a1b2c3d4e5f6789012345678901234567890123456789012345678901234abcd";
        _mockJs.Setup(js => js.InvokeAsync<string>("secretManager.getOrCreateSecret", It.IsAny<object[]>()))
            .ReturnsAsync(secret);
        
        _ = _secretManager.GetOrCreateSecretAsync().Result; // Cache the secret

        // Act
        _secretManager.ClearCache();

        // Assert - Next call should invoke JS again
        _ = _secretManager.GetOrCreateSecretAsync().Result;
        _mockJs.Verify(js => js.InvokeAsync<string>("secretManager.getOrCreateSecret", It.IsAny<object[]>()), Times.Exactly(2));
    }
}
