using LibrisMaleficarum.Domain.Entities;
using LibrisMaleficarum.Domain.Exceptions;
using LibrisMaleficarum.Domain.Interfaces.Repositories;
using LibrisMaleficarum.Domain.Interfaces.Services;
using LibrisMaleficarum.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LibrisMaleficarum.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for Asset entity using Entity Framework Core with Cosmos DB.
/// </summary>
public sealed class AssetRepository : IAssetRepository
{
    private readonly ApplicationDbContext _context;
    private readonly IBlobStorageService _blobStorageService;
    private readonly IWorldRepository _worldRepository;

    /// <summary>
    /// Initializes a new instance of the <see cref="AssetRepository"/> class.
    /// </summary>
    /// <param name="context">Database context.</param>
    /// <param name="blobStorageService">Blob storage service for binary file operations.</param>
    /// <param name="worldRepository">World repository for authorization checks.</param>
    public AssetRepository(
        ApplicationDbContext context,
        IBlobStorageService blobStorageService,
        IWorldRepository worldRepository)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _blobStorageService = blobStorageService ?? throw new ArgumentNullException(nameof(blobStorageService));
        _worldRepository = worldRepository ?? throw new ArgumentNullException(nameof(worldRepository));
    }

    /// <inheritdoc/>
    public async Task<Asset> GetByIdAsync(Guid assetId, Guid userId, CancellationToken cancellationToken = default)
    {
        // Query asset from Cosmos DB - cross-partition query required since we don't have WorldId
        var asset = await _context.Assets
            .Where(a => a.Id == assetId && !a.IsDeleted)
            .FirstOrDefaultAsync(cancellationToken);

        if (asset is null)
        {
            throw new AssetNotFoundException(assetId);
        }

        // Verify user owns the world (authorization check)
        var world = await _worldRepository.GetByIdAsync(asset.WorldId, cancellationToken);
        if (world is null || world.OwnerId != userId)
        {
            throw new UnauthorizedWorldAccessException(asset.WorldId, userId);
        }

        return asset;
    }

    /// <inheritdoc/>
    public async Task<(IReadOnlyList<Asset> Assets, string? NextCursor)> GetAllByEntityAsync(
        Guid entityId,
        Guid worldId,
        Guid userId,
        int limit = 50,
        string? cursor = null,
        CancellationToken cancellationToken = default)
    {
        // Verify user owns the world (authorization check)
        var world = await _worldRepository.GetByIdAsync(worldId, cancellationToken);
        if (world is null || world.OwnerId != userId)
        {
            throw new UnauthorizedWorldAccessException(worldId, userId);
        }

        // Enforce pagination limits
        limit = Math.Min(Math.Max(1, limit), 200);

        // Build base query with partition key
        var query = _context.Assets
            .Where(a => a.WorldId == worldId && a.EntityId == entityId && !a.IsDeleted);

        // Apply cursor pagination if provided
        if (!string.IsNullOrEmpty(cursor) && DateTime.TryParse(cursor, null, System.Globalization.DateTimeStyles.RoundtripKind, out var cursorDate))
        {
            query = query.Where(a => a.CreatedDate > cursorDate);
        }

        // Apply ordering AFTER all filters
        var orderedQuery = query.OrderBy(a => a.CreatedDate).ThenBy(a => a.Id);

        // Fetch limit + 1 to determine if there are more results
        var assets = await orderedQuery.Take(limit + 1).ToListAsync(cancellationToken);

        // Determine next cursor
        string? nextCursor = null;
        if (assets.Count > limit)
        {
            var lastAsset = assets[limit - 1];
            assets.RemoveAt(limit);
            nextCursor = lastAsset.CreatedDate.ToString("O");
        }

        return (assets, nextCursor);
    }

    /// <inheritdoc/>
    public async Task<Asset> CreateAsync(
        Guid worldId,
        Guid entityId,
        string fileName,
        string contentType,
        long sizeBytes,
        Stream fileStream,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(fileStream);
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);
        ArgumentException.ThrowIfNullOrWhiteSpace(contentType);

        // Verify user owns the world (authorization check)
        var world = await _worldRepository.GetByIdAsync(worldId, cancellationToken);
        if (world is null || world.OwnerId != userId)
        {
            throw new UnauthorizedWorldAccessException(worldId, userId);
        }

        // Verify parent entity exists
        var entity = await _context.WorldEntities
            .Where(e => e.Id == entityId && e.WorldId == worldId && !e.IsDeleted)
            .FirstOrDefaultAsync(cancellationToken);

        if (entity is null)
        {
            throw new EntityNotFoundException(worldId, entityId);
        }

        // Generate unique blob name with GUID to avoid collisions
        var assetId = Guid.NewGuid();
        var blobName = $"{worldId}/{entityId}/{assetId}_{fileName}";

        // Upload to blob storage
        var metadata = new Dictionary<string, string>
        {
            { "WorldId", worldId.ToString() },
            { "EntityId", entityId.ToString() },
            { "AssetId", assetId.ToString() },
            { "OriginalFileName", fileName }
        };

        var blobUrl = await _blobStorageService.UploadAsync(
            "assets",
            blobName,
            fileStream,
            contentType,
            metadata,
            cancellationToken);

        // Create asset entity with blob URL
        var asset = Asset.Create(
            worldId,
            entityId,
            fileName,
            contentType,
            sizeBytes,
            blobUrl);

        // Save to Cosmos DB
        _context.Assets.Add(asset);
        await _context.SaveChangesAsync(cancellationToken);

        return asset;
    }

    /// <inheritdoc/>
    public async Task DeleteAsync(Guid assetId, Guid userId, CancellationToken cancellationToken = default)
    {
        // Retrieve asset with authorization check
        var asset = await GetByIdAsync(assetId, userId, cancellationToken);

        // Soft delete in Cosmos DB
        asset.SoftDelete();
        _context.Assets.Update(asset);
        await _context.SaveChangesAsync(cancellationToken);

        // Hard delete from blob storage (fire and forget with error handling)
        try
        {
            await _blobStorageService.DeleteAsync(asset.BlobUrl, cancellationToken);
        }
        catch (Exception ex)
        {
            // Log error but don't fail the operation (blob deletion is best-effort)
            // In production, this should be logged to Application Insights or similar
            Console.WriteLine($"Failed to delete blob {asset.BlobUrl}: {ex.Message}");
        }
    }
}
