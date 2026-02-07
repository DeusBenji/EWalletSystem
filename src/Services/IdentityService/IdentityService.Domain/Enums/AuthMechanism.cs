namespace IdentityService.Domain.Enums;

/// <summary>
/// Authentication mechanism used by a provider
/// </summary>
public enum AuthMechanism
{
    /// <summary>
    /// Session-based authentication via Signicat
    /// Used by MitID, Swedish BankID, Norwegian BankID
    /// </summary>
    SessionBased,
    
    /// <summary>
    /// OAuth 2.0 / OpenID Connect flow
    /// </summary>
    OAuth,
    
    /// <summary>
    /// SAML-based authentication
    /// </summary>
    Saml
}
