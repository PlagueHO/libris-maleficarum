namespace LibrisMaleficarum.Api.Models.Responses;

/// <summary>
/// Represents an error response returned from the API.
/// </summary>
public class ErrorResponse
{
    /// <summary>
    /// Gets or sets the error information.
    /// </summary>
    public required ErrorDetail Error { get; set; }

    /// <summary>
    /// Gets or sets optional metadata about the error response (traceId, timestamp, etc.).
    /// </summary>
    public Dictionary<string, object>? Meta { get; set; }
}

/// <summary>
/// Detailed error information.
/// </summary>
public class ErrorDetail
{
    /// <summary>
    /// Gets or sets the error code (e.g., "VALIDATION_ERROR", "NOT_FOUND").
    /// </summary>
    public required string Code { get; set; }

    /// <summary>
    /// Gets or sets the human-readable error message.
    /// </summary>
    public required string Message { get; set; }

    /// <summary>
    /// Gets or sets field-level validation errors (for validation failures).
    /// </summary>
    public List<ValidationError>? ValidationErrors { get; set; }
}
