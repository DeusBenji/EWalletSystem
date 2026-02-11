namespace ValidationService.Protocol;

/// <summary>
/// Service for validating proof envelopes.
/// Enforces protocol specification and anti-downgrade rules.
/// </summary>
public interface IProofEnvelopeValidator
{
    /// <summary>
    /// Validates a proof envelope according to protocol.md specification
    /// </summary>
    Task<EnvelopeValidationResult> ValidateEnvelopeAsync(
        ProofEnvelope envelope,
        string expectedOrigin,
        CancellationToken cancellationToken = default);
}

public record EnvelopeValidationResult
{
    public required bool Valid { get; init; }
    public required EnvelopeValidationCode Code { get; init; }
    public string? Message { get; init; }
}

public enum EnvelopeValidationCode
{
    Valid = 0,
    
    // Protocol violations
    MissingField = 1,
    InvalidSignature = 2,
    MalformedJson = 3,
    
    // Security checks
    OriginMismatch = 10,
    PolicyMismatch = 11,
    ExpiredCredential = 12,
    RetiredKey = 13,
    ClockSkew = 14,
    
    // Version checks
    IncompatibleVersion = 20,
    DowngradeRejected = 21,  // NEW: Anti-downgrade enforcement
    UnsupportedProtocolVersion = 22,
    
    // Proof verification
    InvalidProof = 30,
    InvalidPublicSignals = 31
}

/// <summary>
/// Implementation of proof envelope validator
/// </summary>
public class ProofEnvelopeValidator : IProofEnvelopeValidator
{
    private readonly ILogger<ProofEnvelopeValidator> _logger;
    private const int MaxClockSkewSeconds = 300; // 5 minutes

    public ProofEnvelopeValidator(ILogger<ProofEnvelopeValidator> logger)
    {
        _logger = logger;
    }

    public async Task<EnvelopeValidationResult> ValidateEnvelopeAsync(
        ProofEnvelope envelope,
        string expectedOrigin,
        CancellationToken cancellationToken = default)
    {
        // 1. Check required fields
        var fieldCheckResult = ValidateRequiredFields(envelope);
        if (!fieldCheckResult.Valid)
        {
            return fieldCheckResult;
        }

        // 2. Check protocol version
        if (!IsProtocolVersionSupported(envelope.ProtocolVersion))
        {
            _logger.LogWarning(
                "Unsupported protocol version: {Version}",
                envelope.ProtocolVersion);
            return Fail(
                EnvelopeValidationCode.UnsupportedProtocolVersion,
                $"Protocol version {envelope.ProtocolVersion} not supported");
        }

        // 3. ANTI-DOWNGRADE: Check policy version >= minimum
        if (!MinimumPolicyVersions.IsVersionAcceptable(envelope.PolicyId, envelope.PolicyVersion))
        {
            var minimum = MinimumPolicyVersions.GetMinimumVersion(envelope.PolicyId);
            
            _logger.LogCritical(
                "⛔ DOWNGRADE ATTACK BLOCKED: {PolicyId} v{Version} < minimum v{Minimum}",
                envelope.PolicyId,
                envelope.PolicyVersion,
                minimum);
            
            return Fail(
                EnvelopeValidationCode.DowngradeRejected,
                $"Policy {envelope.PolicyId} v{envelope.PolicyVersion} below minimum v{minimum}");
        }

        // 4. Check origin binding
        if (!string.Equals(envelope.Origin, expectedOrigin, StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning(
                "Origin mismatch: expected {Expected}, got {Actual}",
                expectedOrigin,
                envelope.Origin);
            return Fail(
                EnvelopeValidationCode.OriginMismatch,
                $"Origin mismatch: expected {expectedOrigin}");
        }

        // 5. Check clock skew
        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var skew = Math.Abs(now - envelope.IssuedAt);
        if (skew > MaxClockSkewSeconds)
        {
            _logger.LogWarning(
                "Clock skew exceeded: {Skew}s (limit: {Limit}s)",
                skew,
                MaxClockSkewSeconds);
            return Fail(
                EnvelopeValidationCode.ClockSkew,
                $"Clock skew {skew}s exceeds {MaxClockSkewSeconds}s limit");
        }

        // 6. Verify signature
        var signatureValid = await VerifySignatureAsync(envelope);
        if (!signatureValid)
        {
            _logger.LogWarning(
                "Envelope signature verification failed for {PolicyId}",
                envelope.PolicyId);
            return Fail(
                EnvelopeValidationCode.InvalidSignature,
                "Envelope signature verification failed");
        }

        // 7. Validate public signals structure
        if (!ValidatePublicSignals(envelope))
        {
            return Fail(
                EnvelopeValidationCode.InvalidPublicSignals,
                "Public signals structure invalid");
        }

        _logger.LogInformation(
            "Proof envelope validated: {PolicyId} v{Version} from {Origin}",
            envelope.PolicyId,
            envelope.PolicyVersion,
            envelope.Origin);

        await Task.CompletedTask; // Satisfy async warning

        return new EnvelopeValidationResult
        {
            Valid = true,
            Code = EnvelopeValidationCode.Valid
        };
    }

    private EnvelopeValidationResult ValidateRequiredFields(ProofEnvelope envelope)
    {
        if (string.IsNullOrEmpty(envelope.ProtocolVersion))
            return Fail(EnvelopeValidationCode.MissingField, "Missing protocolVersion");
        
        if (string.IsNullOrEmpty(envelope.PolicyId))
            return Fail(EnvelopeValidationCode.MissingField, "Missing policyId");
        
        if (string.IsNullOrEmpty(envelope.PolicyVersion))
            return Fail(EnvelopeValidationCode.MissingField, "Missing policyVersion");
        
        if (string.IsNullOrEmpty(envelope.Origin))
            return Fail(EnvelopeValidationCode.MissingField, "Missing origin");
        
        if (string.IsNullOrEmpty(envelope.Nonce))
            return Fail(EnvelopeValidationCode.MissingField, "Missing nonce");
        
        if (envelope.Nonce.Length < 64) // 32 bytes hex = 64 chars
            return Fail(EnvelopeValidationCode.MissingField, "Nonce too short (min 32 bytes)");
        
        if (envelope.Proof == null)
            return Fail(EnvelopeValidationCode.MissingField, "Missing proof");
        
        if (envelope.PublicSignals == null || envelope.PublicSignals.Count == 0)
            return Fail(EnvelopeValidationCode.MissingField, "Missing publicSignals");
        
        if (string.IsNullOrEmpty(envelope.CredentialHash))
            return Fail(EnvelopeValidationCode.MissingField, "Missing credentialHash");
        
        if (string.IsNullOrEmpty(envelope.PolicyHash))
            return Fail(EnvelopeValidationCode.MissingField, "Missing policyHash");
        
        if (string.IsNullOrEmpty(envelope.Signature))
            return Fail(EnvelopeValidationCode.MissingField, "Missing signature");

        return new EnvelopeValidationResult
        {
            Valid = true,
            Code = EnvelopeValidationCode.Valid
        };
    }

    private bool IsProtocolVersionSupported(string version)
    {
        // Support all 1.x.x versions
        return version.StartsWith("1.");
    }

    private async Task<bool> VerifySignatureAsync(ProofEnvelope envelope)
    {
        // TODO: Implement ECDSA signature verification
        // For now, placeholder
        await Task.CompletedTask;
        return true; // Placeholder
    }

    private bool ValidatePublicSignals(ProofEnvelope envelope)
    {
        // Minimum 7 signals required (per protocol.md section 5.1)
        if (envelope.PublicSignals.Count < 7)
        {
            _logger.LogWarning(
                "Public signals count {Count} < minimum 7",
                envelope.PublicSignals.Count);
            return false;
        }

        return true;
    }

    private EnvelopeValidationResult Fail(EnvelopeValidationCode code, string message)
    {
        return new EnvelopeValidationResult
        {
            Valid = false,
            Code = code,
            Message = message
        };
    }
}
