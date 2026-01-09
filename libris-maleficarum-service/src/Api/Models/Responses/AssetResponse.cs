namespace LibrisMaleficarum.Api.Models.Responses;

using LibrisMaleficarum.Domain.Entities;

/// <summary>
/// Response DTO for asset metadata.
/// </summary>
public sealed class AssetResponse
{
    /// <summary>
    /// Asset unique identifier.
    /// </summary>
    public required Guid Id { get; init; }

    /// <summary>
    /// World identifier the asset belongs to.
    /// </summary>
    public required Guid WorldId { get; init; }

    /// <summary>
    /// Entity identifier the asset is attached to.
    /// </summary>
    public required Guid EntityId { get; init; }

    /// <summary>
    /// Original filename of the uploaded asset.
    /// </summary>
    public required string FileName { get; init; }

    /// <summary>
    /// MIME content type (e.g., image/jpeg, audio/mp3).
    /// </summary>
    public required string ContentType { get; init; }

    /// <summary>
    /// Size of the asset in bytes.
    /// </summary>
    public required long SizeBytes { get; init; }

    /// <summary>
    /// Azure Blob Storage URL (not a direct download URL - use /download endpoint for SAS URL).
    /// </summary>
    public required string BlobUrl { get; init; }

    /// <summary>
    /// Asset type classification (Image, Audio, Video, Document).
    /// </summary>
    public required AssetType AssetType { get; init; }

    /// <summary>
    /// Optional tags for categorizing and filtering assets.
    /// </summary>
    public List<string>? Tags { get; init; }

    /// <summary>
    /// Optional description of the asset's purpose or content.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Image dimensions (Width, Height) for image assets only.
    /// </summary>
    public ImageDimensions? ImageDimensions { get; init; }

    /// <summary>
    /// Timestamp when the asset was created.
    /// </summary>
    public required DateTime CreatedDate { get; init; }

    /// <summary>
    /// Timestamp when the asset metadata was last modified.
    /// </summary>
    public DateTime? ModifiedDate { get; init; }
}
