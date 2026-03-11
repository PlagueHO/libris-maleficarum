namespace LibrisMaleficarum.Import.Models;

/// <summary>
/// Configuration options controlling import behavior.
/// </summary>
public sealed class ImportOptions
{
    /// <summary>
    /// Gets the base URL of the backend API.
    /// </summary>
    public required string ApiBaseUrl { get; init; }

    /// <summary>
    /// Gets the authentication token for API requests.
    /// </summary>
    public required string AuthToken { get; init; }

    /// <summary>
    /// Gets or initializes the maximum number of concurrent API requests. Defaults to 10.
    /// </summary>
    public int MaxConcurrency { get; init; } = 10;

    /// <summary>
    /// Gets a value indicating whether to only validate without executing the import.
    /// </summary>
    public bool ValidateOnly { get; init; }

    /// <summary>
    /// Gets a value indicating whether to enable verbose logging.
    /// </summary>
    public bool Verbose { get; init; }
}
