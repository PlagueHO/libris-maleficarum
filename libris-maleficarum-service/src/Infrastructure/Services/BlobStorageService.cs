using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;
using LibrisMaleficarum.Domain.Interfaces.Services;

namespace LibrisMaleficarum.Infrastructure.Services;

/// <summary>
/// Implementation of IBlobStorageService using Azure Blob Storage SDK.
/// </summary>
public sealed class BlobStorageService : IBlobStorageService
{
    private readonly BlobServiceClient _blobServiceClient;

    /// <summary>
    /// Initializes a new instance of the <see cref="BlobStorageService"/> class.
    /// </summary>
    /// <param name="blobServiceClient">Azure Blob Storage service client.</param>
    public BlobStorageService(BlobServiceClient blobServiceClient)
    {
        _blobServiceClient = blobServiceClient ?? throw new ArgumentNullException(nameof(blobServiceClient));
    }

    /// <inheritdoc/>
    public async Task<string> UploadAsync(
        string containerName,
        string blobName,
        Stream contentStream,
        string contentType,
        IDictionary<string, string>? metadata = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(containerName);
        ArgumentNullException.ThrowIfNull(blobName);
        ArgumentNullException.ThrowIfNull(contentStream);
        ArgumentNullException.ThrowIfNull(contentType);

        // Get or create container
        var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
        await containerClient.CreateIfNotExistsAsync(PublicAccessType.None, cancellationToken: cancellationToken);

        // Get blob client
        var blobClient = containerClient.GetBlobClient(blobName);

        // Set blob HTTP headers
        var blobHttpHeaders = new BlobHttpHeaders
        {
            ContentType = contentType
        };

        // Upload blob
        var uploadOptions = new BlobUploadOptions
        {
            HttpHeaders = blobHttpHeaders,
            Metadata = metadata
        };

        await blobClient.UploadAsync(contentStream, uploadOptions, cancellationToken);

        // Return absolute URI
        return blobClient.Uri.AbsoluteUri;
    }

    /// <inheritdoc/>
    public Task<string> GetSasUriAsync(
        string blobUrl,
        int expirationMinutes = 15,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(blobUrl);

        if (expirationMinutes <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(expirationMinutes), "Expiration minutes must be greater than zero.");
        }

        // Parse blob URL to extract container and blob name
        var blobUri = new Uri(blobUrl);

        // Extract container and blob name from URL
        // Azurite URLs include account name: /devstoreaccount1/container/blob
        // Production URLs: /container/blob
        var segments = blobUri.AbsolutePath.TrimStart('/').Split('/');

        // Skip account name if present (Azurite emulator)
        var startIndex = segments.Length >= 3 && segments[0] == "devstoreaccount1" ? 1 : 0;

        if (segments.Length < startIndex + 2)
        {
            throw new ArgumentException($"Invalid blob URL format: {blobUrl}", nameof(blobUrl));
        }

        var containerName = segments[startIndex];
        var blobName = string.Join("/", segments.Skip(startIndex + 1));

        // Get blob client using the service client's account
        var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
        var blobClient = containerClient.GetBlobClient(blobName);

        // Check if we can generate SAS (requires shared key credentials)
        if (!blobClient.CanGenerateSasUri)
        {
            throw new InvalidOperationException("Blob client does not have shared key credentials to generate SAS URI. Ensure BlobServiceClient is configured with AccountKey or use managed identity with delegated SAS.");
        }

        // Create SAS builder
        var sasBuilder = new BlobSasBuilder
        {
            BlobContainerName = containerName,
            BlobName = blobName,
            Resource = "b", // Blob
            StartsOn = DateTimeOffset.UtcNow.AddMinutes(-5), // Allow 5 minutes clock skew
            ExpiresOn = DateTimeOffset.UtcNow.AddMinutes(expirationMinutes)
        };

        // Set read permissions
        sasBuilder.SetPermissions(BlobSasPermissions.Read);

        // Generate SAS URI
        var sasUri = blobClient.GenerateSasUri(sasBuilder);

        return Task.FromResult(sasUri.AbsoluteUri);
    }

    /// <inheritdoc/>
    public async Task DeleteAsync(string blobUrl, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(blobUrl);

        // Parse blob URL to extract container and blob name
        var blobUri = new Uri(blobUrl);

        // Extract container and blob name from URL
        // Azurite URLs include account name: /devstoreaccount1/container/blob
        // Production URLs: /container/blob
        var segments = blobUri.AbsolutePath.TrimStart('/').Split('/');

        // Skip account name if present (Azurite emulator)
        var startIndex = segments.Length >= 3 && segments[0] == "devstoreaccount1" ? 1 : 0;

        if (segments.Length < startIndex + 2)
        {
            throw new ArgumentException($"Invalid blob URL format: {blobUrl}", nameof(blobUrl));
        }

        var containerName = segments[startIndex];
        var blobName = string.Join("/", segments.Skip(startIndex + 1));

        // Get blob client using the service client's account
        var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
        var blobClient = containerClient.GetBlobClient(blobName);

        // Delete blob if exists
        await blobClient.DeleteIfExistsAsync(DeleteSnapshotsOption.IncludeSnapshots, cancellationToken: cancellationToken);
    }
}
