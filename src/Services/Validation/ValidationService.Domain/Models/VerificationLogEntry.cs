namespace Domain.Models
{
    public class VerificationLog
    {
        public Guid Id { get; }
        public string VcJwtHash { get; }
        public bool IsValid { get; }
        public string? FailureReason { get; }
        public DateTime VerifiedAt { get; }

        public VerificationLog(
            Guid id,
            string vcJwtHash,
            bool isValid,
            string? failureReason,
            DateTime verifiedAt)
        {
            Id = id;
            VcJwtHash = vcJwtHash;
            IsValid = isValid;
            FailureReason = failureReason;
            VerifiedAt = verifiedAt;
        }
    }
}
