namespace LibrisMaleficarum.Domain.Interfaces.Repositories;

/// <summary>
/// Repository interface for Asset entity operations.
/// </summary>
public interface IAssetRepository
{
    /// <summary>
    /// Retrieves an asset by its ID with authorization check.
    /// </summary>
    /// <param name="assetId">Asset identifier.</param>
    /// <param name="userId">Current user identifier for authorization.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Asset if found and user is authorized.</returns>
    /// <exception cref="Exceptions.AssetNotFoundException">Thrown when asset not found.</exception>
    /// <exception cref="Exceptions.UnauthorizedWorldAccessException">Thrown when user does not own the world.</exception>
    Task<Entities.Asset> GetByIdAsync(Guid assetId, Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all assets attached to a specific entity with pagination.
    /// </summary>
    /// <param name="entityId">Entity identifier to query assets for.</param>
    /// <param name="worldId">World identifier for partition key.</param>
    /// <param name="userId">Current user identifier for authorization.</param>
    /// <param name="limit">Maximum number of assets to return (default 50, max 200).</param>
    /// <param name="cursor">Continuation token for pagination.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Tuple containing list of assets and next cursor.</returns>
    /// <exception cref="Exceptions.UnauthorizedWorldAccessException">Thrown when user does not own the world.</exception>
    Task<(IReadOnlyList<Entities.Asset> Assets, string? NextCursor)> GetAllByEntityAsync(
        Guid entityId,
        Guid worldId,
        Guid userId,
        int limit = 50,
        string? cursor = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new asset with blob storage upload.
    /// </summary>
    /// <param name="worldId">World identifier.</param>
    /// <param name="entityId">Parent entity identifier.</param>
    /// <param name="fileName">File name.</param>
    /// <param name="contentType">MIME content type.</param>
    /// <param name="sizeBytes">File size in bytes.</param>
    /// <param name="fileStream">Binary file content stream.</param>
    /// <param name="assetType">Asset type classification.</param>
    /// <param name="tags">Optional tags for categorization.</param>
    /// <param name="description">Optional description.</param>
    /// <param name="imageDimensions">Optional image dimensions for image assets.</param>
    /// <param name="userId">Current user identifier for authorization.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Created asset with BlobUrl populated.</returns>
    /// <exception cref="Exceptions.UnauthorizedWorldAccessException">Thrown when user does not own the world.</exception>
    /// <exception cref="Exceptions.EntityNotFoundException">Thrown when parent entity not found.</exception>
    Task<Entities.Asset> CreateAsync(
        Guid worldId,
        Guid entityId,
        string fileName,
        string contentType,
        long sizeBytes,
        Stream fileStream,
        Entities.AssetType assetType,
        List<string>? tags,
        string? description,
        Entities.ImageDimensions? imageDimensions,
        Guid userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates asset metadata (tags and description).
    /// </summary>
    /// <param name="assetId">Asset identifier.</param>
    /// <param name="tags">New tags (null to keep existing).</param>
    /// <param name="description">New description (null to keep existing).</param>
    /// <param name="userId">Current user identifier for authorization.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Updated asset.</returns>
    /// <exception cref="Exceptions.AssetNotFoundException">Thrown when asset not found.</exception>
    /// <exception cref="Exceptions.UnauthorizedWorldAccessException">Thrown when user does not own the world.</exception>
    Task<Entities.Asset> UpdateAsync(
        Guid assetId,
        List<string>? tags,
        string? description,
        Guid userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes an asset (soft delete in database, hard delete from blob storage).
    /// </summary>
    /// <param name="assetId">Asset identifier.</param>
    /// <param name="userId">Current user identifier for authorization.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <exception cref="Exceptions.AssetNotFoundException">Thrown when asset not found.</exception>
    /// <exception cref="Exceptions.UnauthorizedWorldAccessException">Thrown when user does not own the world.</exception>
    Task DeleteAsync(Guid assetId, Guid userId, CancellationToken cancellationToken = default);
}
