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
    public Dictionary<string, object?>? Details { get; init; }

    /// <summary>
    /// Gets field-level validation errors when the request fails validation.
    /// </summary>
    public List<ApiValidationError>? ValidationErrors { get; init; }
}

/// <summary>
/// Client model for the API error envelope.
/// </summary>
public sealed class ApiErrorResponse
{
    /// <summary>
    /// Gets the error payload.
    /// </summary>
    public required ApiError Error { get; init; }
}

/// <summary>
/// Client model for field-level validation errors.
/// </summary>
public sealed class ApiValidationError
{
    /// <summary>
    /// Gets the field that failed validation.
    /// </summary>
    public required string Field { get; init; }

    /// <summary>
    /// Gets the validation message.
    /// </summary>
    public required string Message { get; init; }
}
