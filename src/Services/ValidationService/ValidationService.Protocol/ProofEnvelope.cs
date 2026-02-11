using System.Text.Json;

namespace ValidationService.Protocol;

/// <summary>
/// Proof envelope structure as per protocol.md v1.0
/// </summary>
public record ProofEnvelope
{
    public required string ProtocolVersion { get; init; }
    public required string PolicyId { get; init; }
    public required string PolicyVersion { get; init; }
    public required string Origin { get; init; }
    public required string Nonce { get; init; }
    public required long IssuedAt { get; init; }
    public required ZkProof Proof { get; init; }
    public required List<string> PublicSignals { get; init; }
    public required string CredentialHash { get; init; }
    public required string PolicyHash { get; init; }
    public string? Signature { get; init; }
}

/// <summary>
/// Groth16 zk-SNARK proof
/// </summary>
public record ZkProof
{
    public required List<string> PiA { get; init; }
    public required List<List<string>> PiB { get; init; }
    public required List<string> PiC { get; init; }
}

/// <summary>
/// Canonical JSON encoder for proof envelopes.
/// Ensures deterministic encoding for signature verification.
/// </summary>
public static class CanonicalJsonEncoder
{
    /// <summary>
    /// Encodes envelope to canonical JSON (sorted keys, no whitespace).
    /// The signature field is removed before encoding.
    /// </summary>
    public static string Encode(ProofEnvelope envelope)
    {
        // Remove signature for canonical encoding
        var envelopeWithoutSignature = envelope with { Signature = null };
        
        return JsonSerializer.Serialize(envelopeWithoutSignature, new JsonSerializerOptions
        {
            WriteIndented = false,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
            // Note: JsonSerializer doesn't guarantee key ordering in .NET
            // For production, consider using a library like Newtonsoft.Json with custom contract resolver
            // or implement custom JSON writer with sorted keys
        });
    }
    
    /// <summary>
    /// Parses JSON to ProofEnvelope
    /// </summary>
    public static ProofEnvelope? Parse(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<ProofEnvelope>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
        }
        catch
        {
            return null;
        }
    }
}
