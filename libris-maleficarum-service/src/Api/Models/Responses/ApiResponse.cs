namespace LibrisMaleficarum.Api.Models.Responses;

/// <summary>
/// Generic wrapper for successful API responses.
/// </summary>
/// <typeparam name="T">The type of the response data payload.</typeparam>
public class ApiResponse<T>
{
    /// <summary>
    /// Gets or sets the response data payload.
    /// </summary>
    public required T Data { get; set; }

    /// <summary>
    /// Gets or sets optional metadata about the response (pagination, ETag, etc.).
    /// </summary>
    public Dictionary<string, object>? Meta { get; set; }
}
