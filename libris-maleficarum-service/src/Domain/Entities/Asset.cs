namespace LibrisMaleficarum.Domain.Entities;

/// <summary>
/// Represents an asset (image, audio, document) attached to a world entity.
/// Assets are stored as metadata in Cosmos DB with binary data in Azure Blob Storage.
/// </summary>
public sealed class Asset
{
    /// <summary>
    /// Unique identifier for this asset.
    /// </summary>
    public required Guid Id { get; init; }

    /// <summary>
    /// World identifier for partition key and authorization.
    /// </summary>
    public required Guid WorldId { get; init; }

    /// <summary>
    /// Entity identifier this asset is attached to.
    /// </summary>
    public required Guid EntityId { get; init; }

    /// <summary>
    /// Original filename of the uploaded asset.
    /// </summary>
    public required string FileName { get; init; }

    /// <summary>
    /// MIME content type (e.g., image/jpeg, audio/mp3, application/pdf).
    /// </summary>
    public required string ContentType { get; init; }

    /// <summary>
    /// Size of the asset in bytes.
    /// </summary>
    public required long SizeBytes { get; init; }

    /// <summary>
    /// Azure Blob Storage URL for the asset binary data.
    /// </summary>
    public required string BlobUrl { get; init; }

    /// <summary>
    /// Timestamp when the asset was created.
    /// </summary>
    public required DateTime CreatedDate { get; init; }

    /// <summary>
    /// Soft delete flag. When true, asset should not be returned in queries.
    /// </summary>
    public bool IsDeleted { get; private set; }

    /// <summary>
    /// ETag for optimistic concurrency control.
    /// </summary>
    public string? ETag { get; set; }

    /// <summary>
    /// Private constructor to enforce factory method usage.
    /// </summary>
    private Asset()
    {
    }

    /// <summary>
    /// Creates a new Asset instance with validation.
    /// </summary>
    /// <param name="worldId">World identifier for partition key.</param>
    /// <param name="entityId">Entity identifier this asset is attached to.</param>
    /// <param name="fileName">Original filename (max 255 characters).</param>
    /// <param name="contentType">MIME content type.</param>
    /// <param name="sizeBytes">Size in bytes (must not exceed configured limit).</param>
    /// <param name="blobUrl">Azure Blob Storage URL.</param>
    /// <param name="maxSizeBytes">Maximum allowed file size in bytes (default 25MB).</param>
    /// <param name="allowedContentTypes">List of allowed MIME types (default: common image/audio/video/document types).</param>
    /// <returns>New Asset instance.</returns>
    /// <exception cref="ArgumentException">Thrown when validation fails.</exception>
    public static Asset Create(
        Guid worldId,
        Guid entityId,
        string fileName,
        string contentType,
        long sizeBytes,
        string blobUrl,
        long maxSizeBytes = 26214400, // 25MB default
        IReadOnlySet<string>? allowedContentTypes = null)
    {
        var asset = new Asset
        {
            Id = Guid.NewGuid(),
            WorldId = worldId,
            EntityId = entityId,
            FileName = fileName,
            ContentType = contentType,
            SizeBytes = sizeBytes,
            BlobUrl = blobUrl,
            CreatedDate = DateTime.UtcNow,
            IsDeleted = false
        };

        asset.Validate(maxSizeBytes, allowedContentTypes);
        return asset;
    }

    /// <summary>
    /// Validates asset properties against business rules.
    /// </summary>
    /// <param name="maxSizeBytes">Maximum allowed file size in bytes.</param>
    /// <param name="allowedContentTypes">Set of allowed MIME content types. If null, uses default allowed types.</param>
    /// <exception cref="ArgumentException">Thrown when validation fails.</exception>
    public void Validate(long maxSizeBytes = 26214400, IReadOnlySet<string>? allowedContentTypes = null)
    {
        if (WorldId == Guid.Empty)
        {
            throw new ArgumentException("WorldId cannot be empty.", nameof(WorldId));
        }

        if (EntityId == Guid.Empty)
        {
            throw new ArgumentException("EntityId cannot be empty.", nameof(EntityId));
        }

        if (string.IsNullOrWhiteSpace(FileName))
        {
            throw new ArgumentException("FileName cannot be null or whitespace.", nameof(FileName));
        }

        if (FileName.Length > 255)
        {
            throw new ArgumentException("FileName must not exceed 255 characters.", nameof(FileName));
        }

        if (string.IsNullOrWhiteSpace(ContentType))
        {
            throw new ArgumentException("ContentType cannot be null or whitespace.", nameof(ContentType));
        }

        // Default allowed content types if not provided
        allowedContentTypes ??= new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            // Images
            "image/jpeg", "image/jpg", "image/png", "image/gif", "image/webp",
            // Audio
            "audio/mpeg", "audio/mp3", "audio/wav", "audio/ogg",
            // Video
            "video/mp4", "video/webm",
            // Documents
            "application/pdf", "text/plain", "text/markdown"
        };

        if (!allowedContentTypes.Contains(ContentType))
        {
            throw new ArgumentException(
                $"ContentType '{ContentType}' is not in the allowed list. Allowed types: {string.Join(", ", allowedContentTypes)}",
                nameof(ContentType));
        }

        if (SizeBytes <= 0)
        {
            throw new ArgumentException("SizeBytes must be greater than zero.", nameof(SizeBytes));
        }

        if (SizeBytes > maxSizeBytes)
        {
            throw new ArgumentException(
                $"SizeBytes ({SizeBytes}) exceeds maximum allowed size ({maxSizeBytes} bytes).",
                nameof(SizeBytes));
        }

        if (string.IsNullOrWhiteSpace(BlobUrl))
        {
            throw new ArgumentException("BlobUrl cannot be null or whitespace.", nameof(BlobUrl));
        }

        if (!Uri.TryCreate(BlobUrl, UriKind.Absolute, out _))
        {
            throw new ArgumentException("BlobUrl must be a valid absolute URI.", nameof(BlobUrl));
        }
    }

    /// <summary>
    /// Marks the asset as deleted (soft delete).
    /// </summary>
    public void SoftDelete()
    {
        IsDeleted = true;
    }
}
