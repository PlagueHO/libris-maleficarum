namespace LibrisMaleficarum.Domain.Configuration;

/// <summary>
/// Configuration options for access code protection in single-user mode.
/// When an access code is configured, API requests require a valid X-Access-Code header.
/// </summary>
public sealed record AccessControlOptions
{
    /// <summary>
    /// Section name in configuration file.
    /// </summary>
    public const string SectionName = "AccessControl";

    /// <summary>
    /// Optional access code for protecting the application.
    /// When null or empty, no access code protection is applied.
    /// </summary>
    public string? AccessCode { get; set; }
}
