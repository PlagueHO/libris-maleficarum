namespace LibrisMaleficarum.Api.Client.Models;

/// <summary>
/// Error information from the API.
/// </summary>
public sealed class ApiError
{
    /// <summary>
    /// Gets the human-readable error message.
    /// </summary>
    public string? Message { get; init; }

    /// <summary>
    /// Gets the error code (e.g., "VALIDATION_ERROR", "NOT_FOUND").
    /// </summary>
    public string? Code { get; init; }

    /// <summary>
    /// Gets additional contextual details about the error.
    /// </summary>
    public string? Details { get; init; }
}
