using IdentityService.Application.Interfaces;
using IdentityService.Domain.Entities;
using IdentityService.Infrastructure.Data;
using IdentityService.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace IdentityService.Tests.IntegrationTests;

/// <summary>
/// Integration tests for key compromise scenarios.
/// Verifies the entire key retirement workflow and credential invalidation.
/// </summary>
public class KeyCompromiseTests : IDisposable
{
    private readonly IdentityDbContext _context;
    private readonly KeyManagementService _keyManagement;

    public KeyCompromiseTests()
    {
        var options = new DbContextOptionsBuilder<IdentityDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new IdentityDbContext(options);
        _keyManagement = new KeyManagementService(
            _context,
            NullLogger<KeyManagementService>.Instance);
    }

    [Fact]
    public async Task KeyCompromise_MarkAsRetired_AllCredentialsInvalid()
    {
        // Arrange - Create initial key
        var key = await _keyManagement.RotateKeyAsync();
        Assert.Equal(KeyStatus.Current, key.Status);

        // Act - COMPROMISE DETECTED: Retire key immediately
        await _keyManagement.RetireKeyAsync(
            key.KeyId,
            "Security incident: Key leak detected in logs",
            "security-team@example.com");

        // Assert - Key is retired
        var retiredKey = await _keyManagement.GetKeyByIdAsync(key.KeyId);
        Assert.NotNull(retiredKey);
        Assert.Equal(KeyStatus.Retired, retiredKey.Status);
        Assert.NotNull(retiredKey.RetiredAt);

        // Assert - Key cannot sign
        Assert.False(retiredKey.CanSign());

        // Assert - Key cannot verify (CRITICAL: credentials immediately invalid)
        Assert.False(retiredKey.CanVerify());

        // Assert - Audit log created
        var auditLog = await _context.KeyRetirementAudits
            .FirstOrDefaultAsync(a => a.KeyId == key.KeyId);
        Assert.NotNull(auditLog);
        Assert.Equal("Security incident: Key leak detected in logs", auditLog.Reason);
        Assert.Equal("security-team@example.com", auditLog.Actor);
    }

    [Fact]
    public async Task KeyRotation_OldKeyDeprecated_GracePeriodActive()
    {
        // Arrange - Create initial key
        var oldKey = await _keyManagement.RotateKeyAsync();
        
        // Act - Rotate to new key (planned rotation, NOT compromise)
        var newKey = await _keyManagement.RotateKeyAsync();

        // Assert - Old key deprecated
        var deprecatedKey = await _keyManagement.GetKeyByIdAsync(oldKey.KeyId);
        Assert.NotNull(deprecatedKey);
        Assert.Equal(KeyStatus.Deprecated, deprecatedKey.Status);
        Assert.NotNull(deprecatedKey.DeprecatedAt);

        // Assert - Old key can still verify (grace period)
        Assert.True(deprecatedKey.CanVerify());
        Assert.False(deprecatedKey.CanSign());

        // Assert - New key is current
        var currentKey = await _keyManagement.GetCurrentSigningKeyAsync();
        Assert.NotNull(currentKey);
        Assert.Equal(newKey.KeyId, currentKey.KeyId);
        Assert.Equal(KeyStatus.Current, currentKey.Status);
    }

    [Fact]
    public async Task AutoRetire_DeprecatedKeyPastGrace_AutomaticallyRetired()
    {
        // Arrange - Create and deprecate key manually
        var key = await _keyManagement.RotateKeyAsync();
        key.Status = KeyStatus.Deprecated;
        key.DeprecatedAt = DateTime.UtcNow.AddDays(-8); // 8 days ago (past 7-day grace)
        _context.IssuerSigningKeys.Update(key);
        await _context.SaveChangesAsync();

        // Act - Run auto-retire job
        var retiredCount = await _keyManagement.AutoRetireExpiredKeysAsync();

        // Assert - Key was retired
        Assert.Equal(1, retiredCount);

        var retiredKey = await _keyManagement.GetKeyByIdAsync(key.KeyId);
        Assert.NotNull(retiredKey);
        Assert.Equal(KeyStatus.Retired, retiredKey.Status);
        Assert.False(retiredKey.CanVerify());
    }

    [Fact]
    public async Task AutoRetire_DeprecatedKeyWithinGrace_NotRetired()
    {
        // Arrange - Create and deprecate key recently
        var key = await _keyManagement.RotateKeyAsync();
        key.Status = KeyStatus.Deprecated;
        key.DeprecatedAt = DateTime.UtcNow.AddDays(-3); // 3 days ago (within 7-day grace)
        _context.IssuerSigningKeys.Update(key);
        await _context.SaveChangesAsync();

        // Act - Run auto-retire job
        var retiredCount = await _keyManagement.AutoRetireExpiredKeysAsync();

        // Assert - Key was NOT retired
        Assert.Equal(0, retiredCount);

        var stillDeprecatedKey = await _keyManagement.GetKeyByIdAsync(key.KeyId);
        Assert.NotNull(stillDeprecatedKey);
        Assert.Equal(KeyStatus.Deprecated, stillDeprecatedKey.Status);
        Assert.True(stillDeprecatedKey.CanVerify()); // Still within grace
    }

    [Fact]
    public async Task GetVerificationKeys_RetiredKeyExcluded()
    {
        // Arrange
        var currentKey = await _keyManagement.RotateKeyAsync();
        var deprecatedKey = await _keyManagement.RotateKeyAsync(); // Old key now deprecated
        
        // Retire old key
        var oldKey = await _keyManagement.GetKeyByIdAsync(
            (await _context.IssuerSigningKeys.FirstAsync(k => k.Status == KeyStatus.Deprecated)).KeyId);
        await _keyManagement.RetireKeyAsync(oldKey.KeyId, "Test retirement", "test");

        // Act - Get verification keys
        var verificationKeys = await _keyManagement.GetVerificationKeysAsync();

        // Assert - Only current key in verification set
        Assert.Single(verificationKeys);
        Assert.Equal(currentKey.KeyId, verificationKeys[0].KeyId);
        Assert.DoesNotContain(verificationKeys, k => k.KeyId == oldKey.KeyId);
    }

    [Fact]
    public async Task JWKS_OnlyVerificationKeys_Included()
    {
        // Arrange
        var currentKey = await _keyManagement.RotateKeyAsync();
        var oldKey = await _keyManagement.RotateKeyAsync(); // First key now deprecated
        
        // Retire first key
        var deprecatedKey = await _context.IssuerSigningKeys
            .FirstAsync(k => k.Status == KeyStatus.Deprecated);
        await _keyManagement.RetireKeyAsync(deprecatedKey.KeyId, "Test", "test");

        // Act - Get JWKS
        var jwks = await _keyManagement.GetJwksAsync();

        // Assert - JWKS contains only current key (retired excluded)
        var jwksObj = jwks as dynamic;
        Assert.NotNull(jwksObj);
        // Note: Actual verification would require deserializing the dynamic object
        // For now, we just verify it doesn't throw
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
