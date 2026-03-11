namespace LibrisMaleficarum.Api.Client.Exceptions;

using LibrisMaleficarum.Api.Client.Models;

/// <summary>
/// Exception thrown when the Libris Maleficarum API returns a 401 Unauthorized or 403 Forbidden response.
/// </summary>
public class LibrisApiAuthenticationException : LibrisApiException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LibrisApiAuthenticationException"/> class.
    /// </summary>
    /// <param name="statusCode">The HTTP status code (401 or 403).</param>
    /// <param name="errorMessage">The error message from the API.</param>
    /// <param name="apiError">The structured error information.</param>
    public LibrisApiAuthenticationException(int statusCode, string? errorMessage, ApiError? apiError = null)
        : base(statusCode, errorMessage ?? "Authentication or authorization failed.", apiError)
    {
    }
}
