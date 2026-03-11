namespace LibrisMaleficarum.Api.Client.Exceptions;

using LibrisMaleficarum.Api.Client.Models;

/// <summary>
/// Exception thrown when the Libris Maleficarum API returns a non-success status code.
/// </summary>
public class LibrisApiException : Exception
{
    /// <summary>
    /// Gets the HTTP status code returned by the API.
    /// </summary>
    public int StatusCode { get; }

    /// <summary>
    /// Gets the error message from the API response body.
    /// </summary>
    public string? ErrorMessage { get; }

    /// <summary>
    /// Gets the structured error information from the API response body.
    /// </summary>
    public ApiError? ApiError { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="LibrisApiException"/> class.
    /// </summary>
    /// <param name="statusCode">The HTTP status code.</param>
    /// <param name="errorMessage">The error message from the API.</param>
    /// <param name="apiError">The structured error information.</param>
    public LibrisApiException(int statusCode, string? errorMessage, ApiError? apiError = null)
        : base(errorMessage ?? $"API request failed with status code {statusCode}")
    {
        StatusCode = statusCode;
        ErrorMessage = errorMessage;
        ApiError = apiError;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="LibrisApiException"/> class with an inner exception.
    /// </summary>
    /// <param name="statusCode">The HTTP status code.</param>
    /// <param name="errorMessage">The error message from the API.</param>
    /// <param name="innerException">The inner exception.</param>
    public LibrisApiException(int statusCode, string? errorMessage, Exception innerException)
        : base(errorMessage ?? $"API request failed with status code {statusCode}", innerException)
    {
        StatusCode = statusCode;
        ErrorMessage = errorMessage;
    }
}
