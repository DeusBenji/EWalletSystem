using System.Text.Json.Serialization;

namespace ValidationService.Plugins
{
    /// <summary>
    /// ZKP Presentation for Policy verification
    /// </summary>
    public class PolicyZkpPresentation
    {
        [JsonPropertyName("vcJwt")]
        public string VcJwt { get; set; } = string.Empty;

        [JsonPropertyName("proof")]
        public string Proof { get; set; } = string.Empty;

        [JsonPropertyName("publicInputs")]
        public PolicyZkpPublicInputs PublicInputs { get; set; } = new();
    }

    /// <summary>
    /// Public inputs for Policy ZKP circuit
    /// </summary>
    public class PolicyZkpPublicInputs
    {
        [JsonPropertyName("challengeHash")]
        public string ChallengeHash { get; set; } = string.Empty;

        [JsonPropertyName("policyHash")]
        public string PolicyHash { get; set; } = string.Empty;

        [JsonPropertyName("subjectCommitment")]
        public string SubjectCommitment { get; set; } = string.Empty;

        [JsonPropertyName("sessionTag")]
        public string? SessionTag { get; set; }
    }
}
