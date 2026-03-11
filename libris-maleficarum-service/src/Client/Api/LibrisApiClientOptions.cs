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
}
