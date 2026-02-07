namespace IdentityService.Domain.Interfaces;

public interface IPiiRedactionService
{
    string RedactLogMessage(string message);
    string RedactJson(string json);
    bool ContainsPii(string text);
    bool ContainsPiiFieldNames(string text);
}
