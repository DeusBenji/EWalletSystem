namespace IdentityService.Domain.Exceptions;

/// <summary>
/// Exception thrown during age verification process
/// Contains user-friendly error codes for controlled failure modes
/// </summary>
public class AgeVerificationException : Exception
{
    public AgeVerificationErrorCode ErrorCode { get; }
    
    public AgeVerificationException(
        AgeVerificationErrorCode errorCode, 
        string message) 
        : base(message)
    {
        ErrorCode = errorCode;
    }
    
    public AgeVerificationException(
        AgeVerificationErrorCode errorCode, 
        string message, 
        Exception innerException) 
        : base(message, innerException)
    {
        ErrorCode = errorCode;
    }
}

/// <summary>
/// Error codes for age verification failures
/// Enables user-friendly error handling without exposing system details
/// </summary>
public enum AgeVerificationErrorCode
{
    /// <summary>
    /// Required attribute (e.g., dateOfBirth) missing from provider response
    /// </summary>
    MISSING_ATTRIBUTE,
    
    /// <summary>
    /// Date format from provider is invalid
    /// </summary>
    INVALID_DATE_FORMAT,
    
    /// <summary>
    /// Subject ID missing from provider response
    /// </summary>
    MISSING_SUBJECT_ID,
    
    /// <summary>
    /// Subject ID contains invalid characters or exceeds length limit
    /// </summary>
    INVALID_SUBJECT_ID,
    
    /// <summary>
    /// Provider is not in the allowed providers list
    /// </summary>
    INVALID_PROVIDER,
    
    /// <summary>
    /// Session not found or expired
    /// </summary>
    SESSION_NOT_FOUND,
    
    /// <summary>
    /// Session has already been used (one-time use enforcement)
    /// </summary>
    SESSION_ALREADY_USED
}
