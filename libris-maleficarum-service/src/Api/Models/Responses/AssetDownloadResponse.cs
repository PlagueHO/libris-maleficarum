namespace LibrisMaleficarum.Api.Models.Responses;

/// <summary>
/// Response DTO for asset download with time-limited SAS URL.
/// </summary>
public sealed class AssetDownloadResponse
{
    /// <summary>
    /// Time-limited download URL with SAS token (read-only access).
    /// </summary>
    public required string DownloadUrl { get; init; }

    /// <summary>
    /// Timestamp when the SAS token expires.
    /// </summary>
    public required DateTime ExpiresAt { get; init; }

    /// <summary>
    /// Original filename for download.
    /// </summary>
    public required string FileName { get; init; }

    /// <summary>
    /// MIME content type for Content-Type header.
    /// </summary>
    public required string ContentType { get; init; }

    /// <summary>
    /// Size of the file in bytes.
    /// </summary>
    public required long SizeBytes { get; init; }
}
