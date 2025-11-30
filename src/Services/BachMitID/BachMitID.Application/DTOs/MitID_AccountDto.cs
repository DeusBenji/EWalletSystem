namespace BachMitID.Application.DTOs
{
    public class MitIdAccountDto
    {
        public Guid Id { get; set; }          
        public Guid AccountId { get; set; } 
        public string SubId { get; set; } = string.Empty;
        public bool IsAdult { get; set; }

        public MitIdAccountDto() { }


        public MitIdAccountDto (Guid accountId, string subId, bool isAdult)
        {
            
            AccountId = accountId;
            SubId = subId;
            IsAdult = isAdult;
        }

        public MitIdAccountDto(Guid id, Guid accountId, string subId, bool isAdult) : this (accountId, subId, isAdult)
        {
            Id = id;
        }
    }
}
 