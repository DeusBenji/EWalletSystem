namespace IdentityService.Application.DTOs;

/// <summary>
/// Response from getting session status
/// </summary>
public class GetSessionResponse
{
    /// <summary>
    /// Unique session identifier
    /// </summary>
    public string Id { get; set; } = null!;
    
    /// <summary>
    /// Session status: SUCCESS, PENDING, FAILED, ABORTED
    /// </summary>
    public string Status { get; set; } = null!;
    
    /// <summary>
    /// Subject information (user claims) if authentication succeeded
    /// </summary>
    public SessionSubject? Subject { get; set; }
    
    /// <summary>
    /// Authentication result details
    /// </summary>
    public SessionResult? Result { get; set; }
    
    /// <summary>
    /// Error information if authentication failed
    /// </summary>
    public SessionError? Error { get; set; }
    
    /// <summary>
    /// Identity provider used (e.g., "mitid")
    /// </summary>
    public string? Provider { get; set; }
    
    /// <summary>
    /// Level of Assurance achieved
    /// </summary>
    public string? Loa { get; set; }
    
    /// <summary>
    /// When the session was created
    /// </summary>
    public DateTime Created { get; set; }
    
    /// <summary>
    /// When the user was authenticated (if successful)
    /// </summary>
    public DateTime? Authenticated { get; set; }
}

/// <summary>
/// Subject information from authentication
/// </summary>
public class SessionSubject
{
    /// <summary>
    /// Date of birth (format: YYYY-MM-DD)
    /// </summary>
    public string? DateOfBirth { get; set; }
    
    /// <summary>
    /// Full name
    /// </summary>
    public string? Name { get; set; }
    
    /// <summary>
    /// National ID (CPR for MitID in Denmark)
    /// </summary>
    public string? NationalId { get; set; }
    
    /// <summary>
    /// Unique identifier (UUID)
    /// </summary>
    public string? Uuid { get; set; }
    
    /// <summary>
    /// First name
    /// </summary>
    public string? FirstName { get; set; }
    
    /// <summary>
    /// Last name
    /// </summary>
    public string? LastName { get; set; }
    
    /// <summary>
    /// Additional attributes from provider
    /// </summary>
    public Dictionary<string, object>? AdditionalAttributes { get; set; }
}

/// <summary>
/// Authentication result details
/// </summary>
public class SessionResult
{
    /// <summary>
    /// Authentication method used
    /// </summary>
    public string? Method { get; set; }
    
    /// <summary>
    /// Additional metadata
    /// </summary>
    public Dictionary<string, object>? Metadata { get; set; }
}

/// <summary>
/// Error information
/// </summary>
public class SessionError
{
    /// <summary>
    /// Error code
    /// </summary>
    public string? Code { get; set; }
    
    /// <summary>
    /// Error message
    /// </summary>
    public string? Message { get; set; }
}
