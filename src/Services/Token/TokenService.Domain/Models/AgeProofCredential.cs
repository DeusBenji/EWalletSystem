using System.Text.Json.Serialization;

namespace Domain.Models
{
    public class AgeProofCredentialSubject
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("commitment")]
        public string Commitment { get; set; } = string.Empty;
    }

    public class AgeProofCredential
    {
        [JsonPropertyName("@context")]
        public string[] Context { get; set; } = new[] { "https://www.w3.org/2018/credentials/v1" };

        [JsonPropertyName("type")]
        public string[] Type { get; set; } = new[] { "VerifiableCredential", "AgeProofCredential" };

        [JsonPropertyName("issuer")]
        public string Issuer { get; set; } = string.Empty;

        [JsonPropertyName("issuanceDate")]
        public string IssuanceDate { get; set; } = string.Empty;

        [JsonPropertyName("expirationDate")]
        public string ExpirationDate { get; set; } = string.Empty;

        [JsonPropertyName("credentialSubject")]
        public AgeProofCredentialSubject CredentialSubject { get; set; } = new();
    }
}
