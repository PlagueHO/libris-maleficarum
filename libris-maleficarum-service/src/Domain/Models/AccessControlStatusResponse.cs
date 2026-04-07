using System.Text.Json.Serialization;

namespace LibrisMaleficarum.Domain.Models;

/// <summary>
/// Response model indicating whether an access code is required.
/// </summary>
public sealed record AccessControlStatusResponse
{
    /// <summary>
    /// Whether the application requires an access code for API requests.
    /// </summary>
    [JsonPropertyName("accessCodeRequired")]
    public required bool AccessCodeRequired { get; init; }
}
