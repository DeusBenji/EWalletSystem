namespace IdentityService.Domain.ValueObjects;

/// <summary>
/// Represents a policy credential issued to a user.
/// Credentials are signed JWTs that can be used to generate ZKP proofs.
/// </summary>
public class PolicyCredential
{
    /// <summary>
    /// Unique identifier for this credential
    /// </summary>
    public required string CredentialId { get; init; }

    /// <summary>
    /// Policy this credential is for (e.g., "age_over_18")
    /// </summary>
    public required string PolicyId { get; init; }

    /// <summary>
    /// Subject identifier (pseudonymized user ID)
    /// </summary>
    public required string SubjectId { get; init; }

    /// <summary>
    /// Claims included in this credential
    /// Example: { "birthYear": 1990, "isAdult": true }
    /// </summary>
    public required Dictionary<string, object> Claims { get; init; }

    /// <summary>
    /// When this credential was issued
    /// </summary>
    public DateTime IssuedAt { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// When this credential expires
    /// </summary>
    public required DateTime ExpiresAt { get; init; }

    /// <summary>
    /// Not-before timestamp (credential not valid before this time)
    /// </summary>
    public DateTime? NotBefore { get; init; }

    /// <summary>
    /// Signing key ID used to sign this credential
    /// </summary>
    public required string IssuerSigningKeyId { get; init; }

    /// <summary>
    /// Device tag this credential is bound to
    /// Prevents credential sharing between devices
    /// </summary>
    public string? DeviceTag { get; init; }

    /// <summary>
    /// Hash of the policy definition used
    /// For tamper detection
    /// </summary>
    public required string PolicyHash { get; init; }

    /// <summary>
    /// Converts this credential to a JWT
    /// </summary>
    public string ToJwt(string signature)
    {
        // JWT header
        var header = new
        {
            alg = "ES256",
            typ = "JWT",
            kid = IssuerSigningKeyId
        };

        // JWT payload
        var payload = new
        {
            jti = CredentialId,
            sub = SubjectId,
            iat = new DateTimeOffset(IssuedAt).ToUnixTimeSeconds(),
            exp = new DateTimeOffset(ExpiresAt).ToUnixTimeSeconds(),
            nbf = NotBefore.HasValue ? new DateTimeOffset(NotBefore.Value).ToUnixTimeSeconds() : (long?)null,
            policyId = PolicyId,
            policyHash = PolicyHash,
            deviceTag = DeviceTag,
            claims = Claims
        };

        var headerBase64 = Base64UrlEncode(System.Text.Json.JsonSerializer.Serialize(header));
        var payloadBase64 = Base64UrlEncode(System.Text.Json.JsonSerializer.Serialize(payload));

        return $"{headerBase64}.{payloadBase64}.{signature}";
    }

    /// <summary>
    /// Computes the credential hash for ZKP binding
    /// </summary>
    public string ComputeCredentialHash()
    {
        var data = System.Text.Json.JsonSerializer.Serialize(new
        {
            CredentialId,
            PolicyId,
            SubjectId,
            IssuedAt,
            ExpiresAt,
            PolicyHash
        });

        var bytes = System.Text.Encoding.UTF8.GetBytes(data);
        var hash = System.Security.Cryptography.SHA256.HashData(bytes);
        return $"sha256:{Convert.ToHexString(hash).ToLowerInvariant()}";
    }

    private static string Base64UrlEncode(string input)
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes(input);
        return Convert.ToBase64String(bytes)
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');
    }
}
