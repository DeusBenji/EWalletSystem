// DTOs/VerifyCredentialResultDto.cs
namespace Application.DTOs
{
    public class VerifyCredentialResultDto
    {
        public bool IsValid { get; set; }
        public string? FailureReason { get; set; }
        public DateTime VerifiedAt { get; set; }
        public string? IssuerDid { get; set; }
    }
}
