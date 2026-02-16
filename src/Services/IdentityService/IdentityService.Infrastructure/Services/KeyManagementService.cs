using IdentityService.Application.Interfaces;
using IdentityService.Domain.Entities;
using IdentityService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace IdentityService.Infrastructure.Services;

/// <summary>
/// Implementation of key management service.
/// Handles key rotation, retirement, and JWKS endpoint.
/// </summary>
public class KeyManagementService : IKeyManagementService
{
    private readonly IdentityDbContext _context;
    private readonly ILogger<KeyManagementService> _logger;

    public KeyManagementService(
        IdentityDbContext context,
        ILogger<KeyManagementService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<IssuerSigningKey?> GetCurrentSigningKeyAsync(
        CancellationToken cancellationToken = default)
    {
        return await _context.IssuerSigningKeys
            .Where(k => k.Status == KeyStatus.Current)
            .OrderByDescending(k => k.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IssuerSigningKey?> GetKeyByIdAsync(
        string keyId,
        CancellationToken cancellationToken = default)
    {
        return await _context.IssuerSigningKeys
            .FirstOrDefaultAsync(k => k.KeyId == keyId, cancellationToken);
    }

    public async Task<IReadOnlyList<IssuerSigningKey>> GetVerificationKeysAsync(
        CancellationToken cancellationToken = default)
    {
        var keys = await _context.IssuerSigningKeys
            .Where(k => k.Status != KeyStatus.Retired)
            .ToListAsync(cancellationToken);

        // Filter keys that can verify (current + deprecated in grace period)
        return keys.Where(k => k.CanVerify()).ToList();
    }

    public async Task<IssuerSigningKey> RotateKeyAsync(
        string algorithm = "ES256",
        CancellationToken cancellationToken = default)
    {
        // Deprecate existing current key
        var currentKey = await GetCurrentSigningKeyAsync(cancellationToken);
        if (currentKey != null)
        {
            currentKey.Status = KeyStatus.Deprecated;
            currentKey.DeprecatedAt = DateTime.UtcNow;
            _context.IssuerSigningKeys.Update(currentKey);

            _logger.LogInformation(
                "Deprecated key {KeyId}, grace period until {GraceEnd}",
                currentKey.KeyId,
                currentKey.DeprecatedAt.Value + currentKey.GracePeriod);
        }

        // Generate new key pair
        var (publicKeyJwk, encryptedPrivateKey) = GenerateKeyPair(algorithm);

        var newKey = new IssuerSigningKey
        {
            KeyId = $"key-{DateTime.UtcNow:yyyy-MM-dd-HHmmss}",
            Algorithm = algorithm,
            PublicKeyJwk = publicKeyJwk,
            EncryptedPrivateKey = encryptedPrivateKey,
            Status = KeyStatus.Current,
            CreatedAt = DateTime.UtcNow
        };

        _context.IssuerSigningKeys.Add(newKey);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogWarning(
            "Key rotation completed. New key: {KeyId}, Old key: {OldKeyId}",
            newKey.KeyId,
            currentKey?.KeyId ?? "none");

        return newKey;
    }

    public async Task DeprecateKeyAsync(
        string keyId,
        CancellationToken cancellationToken = default)
    {
        var key = await GetKeyByIdAsync(keyId, cancellationToken);
        if (key == null)
        {
            throw new InvalidOperationException($"Key {keyId} not found");
        }

        key.Status = KeyStatus.Deprecated;
        key.DeprecatedAt = DateTime.UtcNow;

        _context.IssuerSigningKeys.Update(key);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogWarning(
            "Key {KeyId} deprecated, grace period until {GraceEnd}",
            keyId,
            key.DeprecatedAt.Value + key.GracePeriod);
    }

    public async Task RetireKeyAsync(
        string keyId,
        string reason,
        string actor,
        CancellationToken cancellationToken = default)
    {
        var key = await GetKeyByIdAsync(keyId, cancellationToken);
        if (key == null)
        {
            throw new InvalidOperationException($"Key {keyId} not found");
        }

        var oldStatus = key.Status;
        key.Status = KeyStatus.Retired;
        key.RetiredAt = DateTime.UtcNow;

        // Log audit entry
        var auditEntry = new KeyRetirementAudit
        {
            KeyId = keyId,
            OldStatus = oldStatus.ToString(),
            Reason = reason,
            Actor = actor,
            RetiredAt = DateTime.UtcNow
        };

        _context.KeyRetirementAudits.Add(auditEntry);
        _context.IssuerSigningKeys.Update(key);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogCritical(
            "Key {KeyId} RETIRED by {Actor}. Reason: {Reason}. All credentials from this key are now invalid.",
            keyId,
            actor,
            reason);
    }

    public async Task<object> GetJwksAsync(
        CancellationToken cancellationToken = default)
    {
        var verificationKeys = await GetVerificationKeysAsync(cancellationToken);

        var keys = verificationKeys.Select(k => JsonSerializer.Deserialize<object>(k.PublicKeyJwk)).ToList();

        return new
        {
            keys
        };
    }

    public async Task<int> AutoRetireExpiredKeysAsync(
        CancellationToken cancellationToken = default)
    {
        var deprecatedKeys = await _context.IssuerSigningKeys
            .Where(k => k.Status == KeyStatus.Deprecated)
            .ToListAsync(cancellationToken);

        var expiredKeys = deprecatedKeys.Where(k => k.ShouldBeRetired()).ToList();

        foreach (var key in expiredKeys)
        {
            await RetireKeyAsync(
                key.KeyId,
                "Grace period expired",
                "AutoRetireJob",
                cancellationToken);
        }

        if (expiredKeys.Any())
        {
            _logger.LogWarning(
                "Auto-retired {Count} expired keys: {KeyIds}",
                expiredKeys.Count,
                string.Join(", ", expiredKeys.Select(k => k.KeyId)));
        }

        return expiredKeys.Count;
    }

    private (string publicKeyJwk, string encryptedPrivateKey) GenerateKeyPair(string algorithm)
    {
        // TODO: Implement actual key generation using System.Security.Cryptography
        // For now, return placeholder
        // In production, use ECDSA (ES256) or RSA (RS256)
        
        var publicKeyJwk = JsonSerializer.Serialize(new
        {
            kty = "EC",
            crv = "P-256",
            x = "placeholder-x-coordinate",
            y = "placeholder-y-coordinate",
            use = "sig",
            alg = algorithm
        });

        var encryptedPrivateKey = "encrypted-placeholder-private-key";

        return (publicKeyJwk, encryptedPrivateKey);
    }
}


