namespace LibrisMaleficarum.Api.Client;

/// <summary>
/// Configuration options for the Libris Maleficarum API client.
/// </summary>
public sealed class LibrisApiClientOptions
{
    /// <summary>
    /// Gets or sets the base URL of the Libris Maleficarum API.
    /// </summary>
    public required string BaseUrl { get; set; }

    /// <summary>
    /// Gets or sets the optional Bearer authentication token.
    /// </summary>
    public string? AuthToken { get; set; }

    /// <summary>
    /// Gets or sets the per-request timeout for the resilience handler.
    /// When set, overrides the default 30-second total request timeout and 10-second attempt timeout.
    /// Recommended for bulk import operations where backend cold-start may be slow.
    /// </summary>
    public TimeSpan? RequestTimeout { get; set; }
}
