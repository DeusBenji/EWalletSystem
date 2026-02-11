namespace ValidationService.Protocol;

/// <summary>
/// Minimum policy versions enforced by validator.
/// Protects against downgrade attacks where malicious websites
/// request vulnerable old circuit versions.
/// </summary>
public static class MinimumPolicyVersions
{
    /// <summary>
    /// Minimum acceptable version per policy ID.
    /// Versions below this are rejected with DOWNGRADE_REJECTED error.
    /// </summary>
    public static readonly Dictionary<string, string> Minimums = new()
    {
        { "age_over_18", "1.2.0" },      // v1.0.0-1.1.x had vulnerability (example)
        { "drivers_license", "1.0.0" }   // No known vulnerabilities
    };
    
    /// <summary>
    /// Checks if a policy version meets the minimum requirement.
    /// </summary>
    /// <param name="policyId">Policy identifier</param>
    /// <param name="version">Version to check (semver format)</param>
    /// <returns>True if version >= minimum, false otherwise</returns>
    public static bool IsVersionAcceptable(string policyId, string version)
    {
        if (!Minimums.TryGetValue(policyId, out var minimum))
        {
            return false; // Unknown policy
        }
        
        return CompareVersions(version, minimum) >= 0;
    }
    
    /// <summary>
    /// Gets the minimum version for a policy.
    /// </summary>
    public static string? GetMinimumVersion(string policyId)
    {
        Minimums.TryGetValue(policyId, out var minimum);
        return minimum;
    }
    
    /// <summary>
    /// Simple semver comparison (Major.Minor.Patch).
    /// Returns: -1 if v1 < v2, 0 if equal, 1 if v1 > v2
    /// </summary>
    private static int CompareVersions(string v1, string v2)
    {
        var parts1 = ParseVersion(v1);
        var parts2 = ParseVersion(v2);
        
        // Compare major
        if (parts1.Major != parts2.Major)
            return parts1.Major.CompareTo(parts2.Major);
        
        // Compare minor
        if (parts1.Minor != parts2.Minor)
            return parts1.Minor.CompareTo(parts2.Minor);
        
        // Compare patch
        return parts1.Patch.CompareTo(parts2.Patch);
    }
    
    private static (int Major, int Minor, int Patch) ParseVersion(string version)
    {
        var parts = version.Split('.');
        if (parts.Length != 3)
        {
            throw new ArgumentException($"Invalid semver format: {version}");
        }
        
        return (
            int.Parse(parts[0]),
            int.Parse(parts[1]),
            int.Parse(parts[2])
        );
    }
}
