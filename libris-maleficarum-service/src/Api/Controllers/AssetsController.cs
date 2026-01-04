using LibrisMaleficarum.Api.Models.Responses;
using LibrisMaleficarum.Domain.Entities;
using LibrisMaleficarum.Domain.Exceptions;
using LibrisMaleficarum.Domain.Interfaces.Repositories;
using LibrisMaleficarum.Domain.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;

namespace LibrisMaleficarum.Api.Controllers;

/// <summary>
/// Controller for managing assets (images, audio, documents) attached to entities.
/// </summary>
[ApiController]
[Route("api/v1")]
public sealed class AssetsController : ControllerBase
{
    private readonly IAssetRepository _assetRepository;
    private readonly IBlobStorageService _blobStorageService;
    private readonly IWorldEntityRepository _entityRepository;
    private readonly IUserContextService _userContextService;
    private readonly ILogger<AssetsController> _logger;

    private const long DefaultMaxSizeBytes = 26214400; // 25MB
    private const int SasExpirationMinutes = 15;

    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
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

    /// <summary>
    /// Initializes a new instance of the <see cref="AssetsController"/> class.
    /// </summary>
    public AssetsController(
        IAssetRepository assetRepository,
        IBlobStorageService blobStorageService,
        IWorldEntityRepository entityRepository,
        IUserContextService userContextService,
        ILogger<AssetsController> logger)
    {
        _assetRepository = assetRepository ?? throw new ArgumentNullException(nameof(assetRepository));
        _blobStorageService = blobStorageService ?? throw new ArgumentNullException(nameof(blobStorageService));
        _entityRepository = entityRepository ?? throw new ArgumentNullException(nameof(entityRepository));
        _userContextService = userContextService ?? throw new ArgumentNullException(nameof(userContextService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Retrieves all assets attached to a specific entity.
    /// </summary>
    /// <param name="worldId">World identifier.</param>
    /// <param name="entityId">Entity identifier.</param>
    /// <param name="limit">Maximum number of assets to return (default 50, max 200).</param>
    /// <param name="cursor">Continuation token for pagination.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Paginated list of assets.</returns>
    [HttpGet("worlds/{worldId:guid}/entities/{entityId:guid}/assets")]
    [ProducesResponseType(typeof(PaginatedApiResponse<AssetResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAssets(
        Guid worldId,
        Guid entityId,
        [FromQuery] int limit = 50,
        [FromQuery] string? cursor = null,
        CancellationToken cancellationToken = default)
    {
        var currentUserId = await _userContextService.GetCurrentUserIdAsync();

        var (assets, nextCursor) = await _assetRepository.GetAllByEntityAsync(
            entityId,
            worldId,
            currentUserId,
            limit,
            cursor,
            cancellationToken);

        var responses = assets.Select(MapToResponse).ToList();

        return Ok(new PaginatedApiResponse<AssetResponse>
        {
            Data = responses,
            Meta = new PaginationMeta
            {
                Count = responses.Count,
                NextCursor = nextCursor
            }
        });
    }

    /// <summary>
    /// Uploads a new asset attached to an entity.
    /// </summary>
    /// <param name="worldId">World identifier.</param>
    /// <param name="entityId">Entity identifier.</param>
    /// <param name="file">File to upload (multipart/form-data).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Created asset metadata.</returns>
    [HttpPost("worlds/{worldId:guid}/entities/{entityId:guid}/assets")]
    [ProducesResponseType(typeof(ApiResponse<AssetResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UploadAsset(
        Guid worldId,
        Guid entityId,
        IFormFile file,
        CancellationToken cancellationToken = default)
    {
        if (file is null || file.Length == 0)
        {
            return BadRequest(new ErrorResponse
            {
                Error = new ErrorDetail
                {
                    Code = "FILE_REQUIRED",
                    Message = "File is required and cannot be empty."
                }
            });
        }

        // Validate file size
        if (file.Length > DefaultMaxSizeBytes)
        {
            return BadRequest(new ErrorResponse
            {
                Error = new ErrorDetail
                {
                    Code = "FILE_TOO_LARGE",
                    Message = $"File size ({file.Length} bytes) exceeds maximum allowed size ({DefaultMaxSizeBytes} bytes)."
                }
            });
        }

        // Validate content type
        if (!AllowedContentTypes.Contains(file.ContentType))
        {
            return BadRequest(new ErrorResponse
            {
                Error = new ErrorDetail
                {
                    Code = "UNSUPPORTED_FILE_TYPE",
                    Message = $"Content type '{file.ContentType}' is not supported. Allowed types: {string.Join(", ", AllowedContentTypes)}"
                }
            });
        }

        var currentUserId = await _userContextService.GetCurrentUserIdAsync();

        // Validate file size and content type before creating entity
        if (file.Length > DefaultMaxSizeBytes)
        {
            return BadRequest(new ErrorResponse
            {
                Error = new ErrorDetail
                {
                    Code = "FILE_TOO_LARGE",
                    Message = $"File size {file.Length} bytes exceeds maximum allowed size of {DefaultMaxSizeBytes} bytes"
                }
            });
        }

        if (!AllowedContentTypes.Contains(file.ContentType))
        {
            return BadRequest(new ErrorResponse
            {
                Error = new ErrorDetail
                {
                    Code = "UNSUPPORTED_FILE_TYPE",
                    Message = $"Content type '{file.ContentType}' is not supported. Allowed types: {string.Join(", ", AllowedContentTypes)}"
                }
            });
        }

        // Upload file and create asset entity (repository will set BlobUrl after upload)
        using var fileStream = file.OpenReadStream();
        var createdAsset = await _assetRepository.CreateAsync(
            worldId,
            entityId,
            file.FileName,
            file.ContentType,
            file.Length,
            fileStream,
            currentUserId,
            cancellationToken);

        var response = MapToResponse(createdAsset);

        return CreatedAtAction(
            nameof(GetAsset),
            new { worldId, assetId = createdAsset.Id },
            new ApiResponse<AssetResponse> { Data = response });
    }

    /// <summary>
    /// Retrieves asset metadata by ID.
    /// </summary>
    /// <param name="worldId">World identifier.</param>
    /// <param name="assetId">Asset identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Asset metadata.</returns>
    [HttpGet("worlds/{worldId:guid}/assets/{assetId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<AssetResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAsset(
        Guid worldId,
        Guid assetId,
        CancellationToken cancellationToken = default)
    {
        var currentUserId = await _userContextService.GetCurrentUserIdAsync();

        var asset = await _assetRepository.GetByIdAsync(assetId, currentUserId, cancellationToken);

        var response = MapToResponse(asset);

        return Ok(new ApiResponse<AssetResponse> { Data = response });
    }

    /// <summary>
    /// Generates a time-limited download URL with SAS token for an asset.
    /// </summary>
    /// <param name="worldId">World identifier.</param>
    /// <param name="assetId">Asset identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Download URL with SAS token and metadata.</returns>
    [HttpGet("worlds/{worldId:guid}/assets/{assetId:guid}/download")]
    [ProducesResponseType(typeof(ApiResponse<AssetDownloadResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAssetDownloadUrl(
        Guid worldId,
        Guid assetId,
        CancellationToken cancellationToken = default)
    {
        var currentUserId = await _userContextService.GetCurrentUserIdAsync();

        var asset = await _assetRepository.GetByIdAsync(assetId, currentUserId, cancellationToken);

        // Generate SAS URL
        var sasUrl = await _blobStorageService.GetSasUriAsync(asset.BlobUrl, SasExpirationMinutes, cancellationToken);

        var response = new AssetDownloadResponse
        {
            DownloadUrl = sasUrl,
            ExpiresAt = DateTime.UtcNow.AddMinutes(SasExpirationMinutes),
            FileName = asset.FileName,
            ContentType = asset.ContentType,
            SizeBytes = asset.SizeBytes
        };

        return Ok(new ApiResponse<AssetDownloadResponse> { Data = response });
    }

    /// <summary>
    /// Deletes an asset (soft delete metadata, hard delete blob).
    /// </summary>
    /// <param name="worldId">World identifier.</param>
    /// <param name="assetId">Asset identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>No content on success.</returns>
    [HttpDelete("worlds/{worldId:guid}/assets/{assetId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteAsset(
        Guid worldId,
        Guid assetId,
        CancellationToken cancellationToken = default)
    {
        var currentUserId = await _userContextService.GetCurrentUserIdAsync();

        await _assetRepository.DeleteAsync(assetId, currentUserId, cancellationToken);

        return NoContent();
    }

    private static AssetResponse MapToResponse(Asset asset)
    {
        return new AssetResponse
        {
            Id = asset.Id,
            WorldId = asset.WorldId,
            EntityId = asset.EntityId,
            FileName = asset.FileName,
            ContentType = asset.ContentType,
            SizeBytes = asset.SizeBytes,
            BlobUrl = asset.BlobUrl,
            CreatedDate = asset.CreatedDate
        };
    }
}
