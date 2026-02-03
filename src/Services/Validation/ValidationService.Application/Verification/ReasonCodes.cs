namespace ValidationService.Application.Verification
{
    public static class ReasonCodes
    {
        public const string UNSUPPORTED_PRESENTATION = "UNSUPPORTED_PRESENTATION";
        public const string MALFORMED_PRESENTATION = "MALFORMED_PRESENTATION";
        public const string ISSUER_UNTRUSTED = "ISSUER_UNTRUSTED";
        public const string VC_SIGNATURE_INVALID = "VC_SIGNATURE_INVALID";
        public const string VC_EXPIRED = "VC_EXPIRED";
        public const string BINDING_MISMATCH = "BINDING_MISMATCH";
        public const string REPLAY_DETECTED = "REPLAY_DETECTED";
        public const string ZKP_SERVICE_UNAVAILABLE = "ZKP_SERVICE_UNAVAILABLE";
        public const string PROOF_INVALID = "PROOF_INVALID";
        public const string POLICY_MISMATCH = "POLICY_MISMATCH";
        public const string MISSING_CLAIMS = "MISSING_CLAIMS";
        public const string SYSTEM_ERROR = "SYSTEM_ERROR";
        public const string INVALID_SIGNATURE = "INVALID_SIGNATURE";
    }
}
