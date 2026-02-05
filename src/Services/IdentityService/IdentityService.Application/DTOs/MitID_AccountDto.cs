namespace IdentityService.Application.DTOs
{
    /// <summary>
    /// MitID account data transfer object
    /// </summary>
    public class MitIdAccountDto
    {
        public Guid Id { get; set; }
        public Guid AccountId { get; set; }
        
        /// <summary>
        /// National ID (CPR for Denmark)
        /// </summary>
        public string NationalId { get; set; } = null!;
        
        /// <summary>
        /// Full name from MitID
        /// </summary>
        public string? Name { get; set; }
        
        /// <summary>
        /// Date of birth
        /// </summary>
        public DateTime DateOfBirth { get; set; }
        
        /// <summary>
        /// Identity provider used (e.g., "mitid")
        /// </summary>
        public string? Provider { get; set; }
        
        /// <summary>
        /// Level of Assurance (e.g., "substantial", "high")
        /// </summary>
        public string? Loa { get; set; }
        
        /// <summary>
        /// When the account was created
        /// </summary>
        public DateTime CreatedAt { get; set; }
        
        /// <summary>
        /// When the user last authenticated
        /// </summary>
        public DateTime LastAuthenticated { get; set; }
        
        // Legacy fields (kept for backwards compatibility)
        public string? SubId { get; set; }
        public bool IsAdult { get; set; }
        
        public MitIdAccountDto() { }
        
        public MitIdAccountDto(Guid accountId, string nationalId, DateTime dateOfBirth)
        {
            AccountId = accountId;
            NationalId = nationalId;
            DateOfBirth = dateOfBirth;
            IsAdult = CalculateIsAdult(dateOfBirth);
        }
        
        private static bool CalculateIsAdult(DateTime dateOfBirth)
        {
            var age = DateTime.UtcNow.Year - dateOfBirth.Year;
            if (DateTime.UtcNow < dateOfBirth.AddYears(age))
                age--;
            return age >= 18;
        }
    }
}
 