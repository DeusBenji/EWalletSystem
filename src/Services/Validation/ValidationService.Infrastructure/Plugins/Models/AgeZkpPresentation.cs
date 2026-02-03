using System.Text.Json.Serialization;

namespace ValidationService.Plugins
{
    public class AgeZkpPresentation
    {
        [JsonPropertyName("verifiableCredential")]
        public string VerifiableCredential { get; set; } = string.Empty;

        [JsonPropertyName("proof")]
        public string Proof { get; set; } = string.Empty;

        [JsonPropertyName("publicInputs")]
        public AgeZkpPublicInputs PublicInputs { get; set; } = new();
    }

    public class AgeZkpPublicInputs
    {
        [JsonPropertyName("commitment")]
        public string Commitment { get; set; } = string.Empty;

        [JsonPropertyName("challengeHash")]
        public string ChallengeHash { get; set; } = string.Empty;

        [JsonPropertyName("currentYear")]
        public string CurrentYear { get; set; } = string.Empty;
    }
}
