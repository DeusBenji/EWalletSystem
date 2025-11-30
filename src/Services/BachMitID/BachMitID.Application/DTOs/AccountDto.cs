namespace BachMitID.Application.DTOs
{
    public class AccountDto
    {
        public Guid Id { get; set; }
        public string? Email { get; set; }

        public AccountDto() { }

        public AccountDto(Guid id, string email)
        {
            Id = id;
            Email = email;
        }

    }
}
 