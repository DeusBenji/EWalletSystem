namespace Application.Interfaces
{
    public interface IMitIdVerifiedService
    {
        Task HandleMitIdVerifiedAsync(Guid accountId, bool isAdult, DateTime verifiedAt, CancellationToken ct = default);
    }
}
