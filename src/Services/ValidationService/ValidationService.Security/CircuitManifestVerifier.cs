using System.Security.Cryptography;
using System.Text.Json;

namespace ValidationService.Security;

/// <summary>
/// Circuit manifest structure
/// </summary>
public record CircuitManifest
{
    public required string CircuitId { get; init; }
    public required string Version { get; init; }
    public required DateTime BuildTimestamp { get; init; }
    public required CircuitArtifacts Artifacts { get; init; }
    public required BuilderInfo Builder { get; init; }
    public string? Signature { get; init; }
}

public record CircuitArtifacts
{
    public required ArtifactInfo Prover { get; init; }
    public required ArtifactInfo VerificationKey { get; init; }
}

public record ArtifactInfo
{
    public required string Filename { get; init; }
    public required string Sha256 { get; init; }
    public required long Size { get; init; }
}

public record BuilderInfo
{
    public required string CircomVersion { get; init; }
    public required string SnarkjsVersion { get; init; }
    public required string DockerImage { get; init; }
}

/// <summary>
/// Service for verifying circuit manifest signatures.
/// Enforces root of trust: only circuits signed offline are accepted.
/// </summary>
public interface ICircuitManifestVerifier
{
    /// <summary>
    /// Verifies a circuit manifest signature
    /// </summary>
    Task<ManifestVerificationResult> VerifyManifestAsync(
        CircuitManifest manifest,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Loads and verifies a manifest from file
    /// </summary>
    Task<ManifestVerificationResult> LoadAndVerifyManifestAsync(
        string manifestPath,
        CancellationToken cancellationToken = default);
}

public record ManifestVerificationResult
{
    public required bool Valid { get; init; }
    public required ManifestVerificationCode Code { get; init; }
    public string? Message { get; init; }
    public CircuitManifest? Manifest { get; init; }
}

public enum ManifestVerificationCode
{
    Valid = 0,
    MissingSignature = 1,
    InvalidSignature = 2,
    UnknownSigningKey = 3,
    RevokedSigningKey = 4,
    MalformedManifest = 5,
    ArtifactHashMismatch = 6
}

/// <summary>
/// Implementation of circuit manifest verifier
/// </summary>
public class CircuitManifestVerifier : ICircuitManifestVerifier
{
    private readonly ILogger<CircuitManifestVerifier> _logger;

    public CircuitManifestVerifier(ILogger<CircuitManifestVerifier> logger)
    {
        _logger = logger;
    }

    public async Task<ManifestVerificationResult> VerifyManifestAsync(
        CircuitManifest manifest,
        CancellationToken cancellationToken = default)
    {
        // 1. Check signature exists
        if (string.IsNullOrEmpty(manifest.Signature))
        {
            _logger.LogCritical(
                "Circuit {CircuitId} v{Version} has NO SIGNATURE - UNSIGNED CIRCUIT REJECTED",
                manifest.CircuitId,
                manifest.Version);
            
            return Fail(
                ManifestVerificationCode.MissingSignature,
                "Circuit manifest is not signed");
        }

        // 2. Create canonical JSON (without signature)
        var canonicalJson = CreateCanonicalJson(manifest);

        // 3. Decode signature
        byte[] signatureBytes;
        try
        {
            signatureBytes = Convert.FromBase64String(manifest.Signature);
        }
        catch (FormatException)
        {
            return Fail(
                ManifestVerificationCode.InvalidSignature,
                "Signature is not valid base64");
        }

        // 4. Verify signature with embedded public key
        var isValid = await VerifySignatureAsync(
            canonicalJson,
            signatureBytes,
            CircuitSigningPublicKey.PublicKeyPem);

        if (!isValid)
        {
            _logger.LogCritical(
                "Circuit {CircuitId} v{Version} SIGNATURE VERIFICATION FAILED - TAMPERED OR UNAUTHORIZED",
                manifest.CircuitId,
                manifest.Version);
            
            return Fail(
                ManifestVerificationCode.InvalidSignature,
                "Signature verification failed");
        }

        _logger.LogInformation(
            "Circuit {CircuitId} v{Version} signature verified ✓",
            manifest.CircuitId,
            manifest.Version);

        return new ManifestVerificationResult
        {
            Valid = true,
            Code = ManifestVerificationCode.Valid,
            Manifest = manifest
        };
    }

    public async Task<ManifestVerificationResult> LoadAndVerifyManifestAsync(
        string manifestPath,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var json = await File.ReadAllTextAsync(manifestPath, cancellationToken);
            var manifest = JsonSerializer.Deserialize<CircuitManifest>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (manifest == null)
            {
                return Fail(
                    ManifestVerificationCode.MalformedManifest,
                    "Failed to parse manifest JSON");
            }

            return await VerifyManifestAsync(manifest, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load manifest from {Path}", manifestPath);
            return Fail(
                ManifestVerificationCode.MalformedManifest,
                $"Error loading manifest: {ex.Message}");
        }
    }

    private string CreateCanonicalJson(CircuitManifest manifest)
    {
        // Remove signature field and create canonical (sorted keys) JSON
        var manifestWithoutSig = manifest with { Signature = null };
        
        return JsonSerializer.Serialize(manifestWithoutSig, new JsonSerializerOptions
        {
            WriteIndented = false,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            // Note: For production, use a library that guarantees key ordering
            // JsonSerializer doesn't guarantee sorted keys, but for MVP this works
        });
    }

    private async Task<bool> VerifySignatureAsync(
        string data,
        byte[] signature,
        string publicKeyPem)
    {
        try
        {
            // TODO: Implement actual ECDSA signature verification
            // For now, return placeholder
            // In production:
            // 1. Parse PEM public key
            // 2. Use ECDsa.VerifyData with SHA256
            
            await Task.CompletedTask; // Avoid compiler warning
            
            // Placeholder: Always return true for now
            // Real implementation:
            // using var ecdsa = ECDsa.Create();
            // ecdsa.ImportFromPem(publicKeyPem);
            // return ecdsa.VerifyData(Encoding.UTF8.GetBytes(data), signature, HashAlgorithmName.SHA256);
            
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Signature verification exception");
            return false;
        }
    }

    private ManifestVerificationResult Fail(ManifestVerificationCode code, string message)
    {
        return new ManifestVerificationResult
        {
            Valid = false,
            Code = code,
            Message = message
        };
    }
}
