namespace LibrisMaleficarum.Api.Client.Models;

/// <summary>
/// Generic wrapper matching the API response envelope { "data": ... }.
/// </summary>
/// <typeparam name="T">The type of the response data payload.</typeparam>
public sealed class ApiResponse<T>
{
    /// <summary>
    /// Gets the response data payload.
    /// </summary>
    public required T Data { get; init; }
}
