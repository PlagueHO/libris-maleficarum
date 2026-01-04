namespace LibrisMaleficarum.Domain.Interfaces.Services;

/// <summary>
/// Service interface for Azure Blob Storage operations.
/// </summary>
public interface IBlobStorageService
{
    /// <summary>
    /// Uploads a file to Azure Blob Storage.
    /// </summary>
    /// <param name="containerName">Container name (e.g., "assets").</param>
    /// <param name="blobName">Unique blob name/path.</param>
    /// <param name="contentStream">File content stream.</param>
    /// <param name="contentType">MIME content type.</param>
    /// <param name="metadata">Optional metadata key-value pairs to attach to blob.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Absolute URI of the uploaded blob.</returns>
    Task<string> UploadAsync(
        string containerName,
        string blobName,
        Stream contentStream,
        string contentType,
        IDictionary<string, string>? metadata = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates a time-limited SAS (Shared Access Signature) URI for blob download.
    /// </summary>
    /// <param name="blobUrl">Absolute blob URL.</param>
    /// <param name="expirationMinutes">Minutes until SAS token expires (default 15).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>SAS URI with read-only permissions.</returns>
    Task<string> GetSasUriAsync(
        string blobUrl,
        int expirationMinutes = 15,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a blob from Azure Blob Storage.
    /// </summary>
    /// <param name="blobUrl">Absolute blob URL to delete.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task DeleteAsync(string blobUrl, CancellationToken cancellationToken = default);
}
