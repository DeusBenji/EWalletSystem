using System.Text.RegularExpressions;
using IdentityService.Domain.Interfaces;

namespace IdentityService.Infrastructure.Services;

public class PiiRedactionService : IPiiRedactionService
{
    private static readonly string[] PiiFieldNames = 
    {
        "nationalId", "dateOfBirth", "nin", "personalNumber", 
        "name", "firstName", "lastName", "address", "cpr"
    };
    
    public string RedactLogMessage(string message)
    {
        // Redact CPR (10 digits)
        message = Regex.Replace(message, @"\b\d{10}\b", "**CPR**");
        
        // Redact Swedish personnummer (YYYYMMDD-XXXX)
        message = Regex.Replace(message, @"\b\d{8}-\d{4}\b", "**PERSONNUMMER**");
        
        // Redact Norwegian fødselsnummer (11 digits)
        message = Regex.Replace(message, @"\b\d{11}\b", "**FØDSELSNUMMER**");
        
        // Redact dates (YYYY-MM-DD, DD-MM-YYYY, etc.)
        message = Regex.Replace(message, @"\b\d{4}-\d{2}-\d{2}\b", "**DATE**");
        message = Regex.Replace(message, @"\b\d{2}/\d{2}/\d{4}\b", "**DATE**");
        
        // Detect JSON objects with PII fields (prohibit logging entire objects)
        if (message.Contains("{") && PiiFieldNames.Any(f => message.Contains($"\"{f}\"")))
        {
            throw new InvalidOperationException(
                "Attempted to log object containing PII fields. Use structured logging with whitelisted fields only.");
        }
        
        return message;
    }
    
    public string RedactJson(string json)
    {
        // Redact specific JSON fields
        json = Regex.Replace(json, 
            "\"(nationalId|dateOfBirth|nin|personalNumber|name|firstName|lastName|address|cpr)\"\\s*:\\s*\"[^\"]+\"", 
            "\"$1\":\"**REDACTED**\"");
        
        return json;
    }
    
    public bool ContainsPii(string text)
    {
        return Regex.IsMatch(text, @"\b\d{10,11}\b") || // CPR/fødselsnummer
               Regex.IsMatch(text, @"\b\d{4}-\d{2}-\d{2}\b") || // DOB
               (text.Contains("{") && PiiFieldNames.Any(f => text.Contains($"\"{f}\"")));
    }

    public bool ContainsPiiFieldNames(string text)
    {
         return PiiFieldNames.Any(f => text.Contains(f));
    }
}
