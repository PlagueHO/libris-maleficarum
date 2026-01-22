using FluentValidation;
using LibrisMaleficarum.Api.Models.Requests;
using LibrisMaleficarum.Api.Models.Responses;
using LibrisMaleficarum.Domain.Entities;
using LibrisMaleficarum.Domain.Interfaces.Repositories;
using LibrisMaleficarum.Domain.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;

namespace LibrisMaleficarum.Api.Controllers;

/// <summary>
/// API controller for managing worlds.
/// </summary>
[ApiController]
[Route("api/v1/worlds")]
public class WorldsController : ControllerBase
{
    private readonly IWorldRepository _worldRepository;
    private readonly ISearchService _searchService;
    private readonly IUserContextService _userContextService;
    private readonly IValidator<CreateWorldRequest> _createValidator;
    private readonly IValidator<UpdateWorldRequest> _updateValidator;

    /// <summary>
    /// Initializes a new instance of the <see cref="WorldsController"/> class.
    /// </summary>
    /// <param name="worldRepository">The world repository.</param>
    /// <param name="searchService">The search service.</param>
    /// <param name="userContextService">The user context service.</param>
    /// <param name="createValidator">The create request validator.</param>
    /// <param name="updateValidator">The update request validator.</param>
    public WorldsController(
        IWorldRepository worldRepository,
        ISearchService searchService,
        IUserContextService userContextService,
        IValidator<CreateWorldRequest> createValidator,
        IValidator<UpdateWorldRequest> updateValidator)
    {
        _worldRepository = worldRepository ?? throw new ArgumentNullException(nameof(worldRepository));
        _searchService = searchService ?? throw new ArgumentNullException(nameof(searchService));
        _userContextService = userContextService ?? throw new ArgumentNullException(nameof(userContextService));
        _createValidator = createValidator ?? throw new ArgumentNullException(nameof(createValidator));
        _updateValidator = updateValidator ?? throw new ArgumentNullException(nameof(updateValidator));
    }

    /// <summary>
    /// Creates a new world.
    /// </summary>
    /// <param name="request">The create world request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created world.</returns>
    /// <response code="201">World created successfully.</response>
    /// <response code="400">Invalid request data.</response>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<WorldResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateWorld(
        [FromBody] CreateWorldRequest request,
        CancellationToken cancellationToken)
    {
        var validationResult = await _createValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors.Select(e => new ValidationError
            {
                Field = e.PropertyName,
                Message = e.ErrorMessage
            }).ToList();

            return BadRequest(new ErrorResponse
            {
                Error = new ErrorDetail
                {
                    Code = "VALIDATION_ERROR",
                    Message = "One or more validation errors occurred.",
                    ValidationErrors = errors
                }
            });
        }

        var currentUserId = await _userContextService.GetCurrentUserIdAsync();
        var world = World.Create(currentUserId, request.Name, request.Description);

        var createdWorld = await _worldRepository.CreateAsync(world, cancellationToken);

        var response = new ApiResponse<WorldResponse>
        {
            Data = MapToResponse(createdWorld),
            Meta = new Dictionary<string, object>
            {
                ["etag"] = GetETag(createdWorld)
            }
        };

        return CreatedAtAction(
            nameof(GetWorld),
            new { worldId = createdWorld.Id },
            response);
    }

    /// <summary>
    /// Searches for entities within a world by query string.
    /// Performs case-insensitive partial matching on Name, Description, and Tags.
    /// </summary>
    /// <param name="worldId">The world identifier.</param>
    /// <param name="q">The search query string.</param>
    /// <param name="sortBy">Field to sort by (name, createdDate, modifiedDate). Default: modifiedDate.</param>
    /// <param name="sortOrder">Sort order (asc, desc). Default: desc.</param>
    /// <param name="limit">Maximum number of results to return (default 50, max 200).</param>
    /// <param name="cursor">Continuation cursor from previous response.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Paginated search results.</returns>
    /// <response code="200">Search results retrieved successfully.</response>
    /// <response code="400">Invalid search query.</response>
    /// <response code="403">Forbidden - user does not own this world.</response>
    /// <response code="404">World not found.</response>
    [HttpGet("{worldId:guid}/search")]
    [ProducesResponseType(typeof(PaginatedApiResponse<EntityResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SearchEntities(
        Guid worldId,
        [FromQuery] string q,
        [FromQuery] string? sortBy = null,
        [FromQuery] string? sortOrder = null,
        [FromQuery] int limit = 50,
        [FromQuery] string? cursor = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(q))
        {
            return BadRequest(new ErrorResponse
            {
                Error = new ErrorDetail
                {
                    Code = "INVALID_SEARCH_QUERY",
                    Message = "Search query parameter 'q' is required and cannot be empty."
                }
            });
        }

        var (entities, nextCursor) = await _searchService.SearchEntitiesAsync(
            worldId,
            q,
            sortBy,
            sortOrder,
            limit,
            cursor,
            cancellationToken);

        var responses = entities.Select(MapToEntityResponse).ToList();

        return Ok(new PaginatedApiResponse<EntityResponse>
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
    /// Gets all worlds owned by the current user.
    /// </summary>
    /// <param name="limit">Maximum number of items to return (default 50, max 200).</param>
    /// <param name="cursor">Continuation token for pagination.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A paginated list of worlds.</returns>
    /// <response code="200">Worlds retrieved successfully.</response>
    [HttpGet]
    [ProducesResponseType(typeof(PaginatedApiResponse<WorldResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetWorlds(
        [FromQuery] int limit = 50,
        [FromQuery] string? cursor = null,
        CancellationToken cancellationToken = default)
    {
        var currentUserId = await _userContextService.GetCurrentUserIdAsync();
        var (worlds, nextCursor) = await _worldRepository.GetAllByOwnerAsync(
            currentUserId,
            limit,
            cursor,
            cancellationToken);

        var worldList = worlds.ToList();

        var response = new PaginatedApiResponse<WorldResponse>
        {
            Data = worldList.Select(MapToResponse).ToList(),
            Meta = new PaginationMeta
            {
                Count = worldList.Count,
                NextCursor = nextCursor
            }
        };

        return Ok(response);
    }

    /// <summary>
    /// Gets a specific world by ID.
    /// </summary>
    /// <param name="worldId">The unique identifier of the world.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The requested world.</returns>
    /// <response code="200">World retrieved successfully.</response>
    /// <response code="404">World not found.</response>
    /// <response code="403">Forbidden - user does not own this world.</response>
    [HttpGet("{worldId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<WorldResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetWorld(
        Guid worldId,
        CancellationToken cancellationToken)
    {
        var world = await _worldRepository.GetByIdAsync(worldId, cancellationToken);

        if (world is null)
        {
            return NotFound(new ErrorResponse
            {
                Error = new ErrorDetail
                {
                    Code = "WORLD_NOT_FOUND",
                    Message = $"World with ID '{worldId}' was not found."
                }
            });
        }

        var response = new ApiResponse<WorldResponse>
        {
            Data = MapToResponse(world),
            Meta = new Dictionary<string, object>
            {
                ["etag"] = GetETag(world)
            }
        };

        Response.Headers.ETag = $"\"{GetETag(world)}\"";

        return Ok(response);
    }

    /// <summary>
    /// Updates an existing world.
    /// </summary>
    /// <param name="worldId">The unique identifier of the world to update.</param>
    /// <param name="request">The update world request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated world.</returns>
    /// <response code="200">World updated successfully.</response>
    /// <response code="400">Invalid request data.</response>
    /// <response code="404">World not found.</response>
    /// <response code="403">Forbidden - user does not own this world.</response>
    /// <response code="409">Conflict - ETag mismatch.</response>
    [HttpPut("{worldId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<WorldResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateWorld(
        Guid worldId,
        [FromBody] UpdateWorldRequest request,
        CancellationToken cancellationToken)
    {
        var validationResult = await _updateValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors.Select(e => new ValidationError
            {
                Field = e.PropertyName,
                Message = e.ErrorMessage
            }).ToList();

            return BadRequest(new ErrorResponse
            {
                Error = new ErrorDetail
                {
                    Code = "VALIDATION_ERROR",
                    Message = "One or more validation errors occurred.",
                    ValidationErrors = errors
                }
            });
        }

        var existingWorld = await _worldRepository.GetByIdAsync(worldId, cancellationToken);
        if (existingWorld is null)
        {
            return NotFound(new ErrorResponse
            {
                Error = new ErrorDetail
                {
                    Code = "WORLD_NOT_FOUND",
                    Message = $"World with ID '{worldId}' was not found."
                }
            });
        }

        // Get If-Match header for ETag validation
        var ifMatch = Request.Headers.IfMatch.ToString().Trim('"');

        existingWorld.Update(request.Name, request.Description);
        var updatedWorld = await _worldRepository.UpdateAsync(existingWorld, ifMatch, cancellationToken);

        var response = new ApiResponse<WorldResponse>
        {
            Data = MapToResponse(updatedWorld),
            Meta = new Dictionary<string, object>
            {
                ["etag"] = GetETag(updatedWorld)
            }
        };

        Response.Headers.ETag = $"\"{GetETag(updatedWorld)}\"";

        return Ok(response);
    }

    /// <summary>
    /// Deletes a world (soft delete).
    /// </summary>
    /// <param name="worldId">The unique identifier of the world to delete.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>No content on success.</returns>
    /// <response code="204">World deleted successfully.</response>
    /// <response code="404">World not found.</response>
    /// <response code="403">Forbidden - user does not own this world.</response>
    [HttpDelete("{worldId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> DeleteWorld(
        Guid worldId,
        CancellationToken cancellationToken)
    {
        await _worldRepository.DeleteAsync(worldId, cancellationToken);
        return NoContent();
    }

    private static WorldResponse MapToResponse(World world)
    {
        return new WorldResponse
        {
            Id = world.Id,
            OwnerId = world.OwnerId,
            Name = world.Name,
            Description = world.Description,
            CreatedDate = world.CreatedDate,
            ModifiedDate = world.ModifiedDate
        };
    }

    private static string GetETag(World world)
    {
        // Generate a simple ETag based on ModifiedDate
        return Convert.ToBase64String(
            System.Text.Encoding.UTF8.GetBytes(world.ModifiedDate.ToString("O")));
    }

    private static EntityResponse MapToEntityResponse(Domain.Entities.WorldEntity entity)
    {
        return new EntityResponse
        {
            Id = entity.Id,
            WorldId = entity.WorldId,
            ParentId = entity.ParentId,
            EntityType = entity.EntityType,
            Name = entity.Name,
            Description = entity.Description,
            Tags = entity.Tags,
            Attributes = entity.GetAttributes(),
            Path = entity.Path,
            Depth = entity.Depth,
            HasChildren = entity.HasChildren,
            OwnerId = entity.OwnerId,
            IsDeleted = entity.IsDeleted,
            CreatedDate = entity.CreatedDate,
            ModifiedDate = entity.ModifiedDate,
            SchemaVersion = entity.SchemaVersion
        };
    }
}
