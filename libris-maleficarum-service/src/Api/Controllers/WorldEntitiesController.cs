namespace LibrisMaleficarum.Api.Controllers;

using FluentValidation;
using LibrisMaleficarum.Api.Models.Requests;
using LibrisMaleficarum.Api.Models.Responses;
using LibrisMaleficarum.Api.Validators;
using LibrisMaleficarum.Domain.Entities;
using LibrisMaleficarum.Domain.Interfaces.Repositories;
using LibrisMaleficarum.Domain.Interfaces.Services;
using LibrisMaleficarum.Domain.ValueObjects;
using Microsoft.AspNetCore.Mvc;

/// <summary>
/// Controller for managing entities within worlds.
/// </summary>
[ApiController]
[Route("api/v1/worlds/{worldId:guid}/entities")]
public class WorldEntitiesController : ControllerBase
{
    private readonly IWorldEntityRepository _entityRepository;
    private readonly ISearchService _searchService;
    private readonly IWorldRepository _worldRepository;
    private readonly IUserContextService _userContextService;
    private readonly IValidator<CreateWorldEntityRequest> _createValidator;
    private readonly IValidator<UpdateWorldEntityRequest> _updateValidator;
    private readonly SchemaVersionValidator _schemaVersionValidator;

    /// <summary>
    /// Initializes a new instance of the <see cref="WorldEntitiesController"/> class.
    /// </summary>
    public WorldEntitiesController(
        IWorldEntityRepository entityRepository,
        ISearchService searchService,
        IWorldRepository worldRepository,
        IUserContextService userContextService,
        IValidator<CreateWorldEntityRequest> createValidator,
        IValidator<UpdateWorldEntityRequest> updateValidator,
        SchemaVersionValidator schemaVersionValidator)
    {
        _entityRepository = entityRepository;
        _searchService = searchService;
        _worldRepository = worldRepository;
        _userContextService = userContextService;
        _schemaVersionValidator = schemaVersionValidator;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    /// <summary>
    /// Creates a new entity within a world.
    /// </summary>
    /// <param name="worldId">The world identifier.</param>
    /// <param name="request">The entity creation request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created entity with 201 Created status.</returns>
    [HttpPost]
    [ProducesResponseType<ApiResponse<EntityResponse>>(StatusCodes.Status201Created)]
    [ProducesResponseType<ErrorResponse>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ErrorResponse>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ErrorResponse>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreateEntity(
        Guid worldId,
        [FromBody] CreateWorldEntityRequest request,
        CancellationToken cancellationToken)
    {
        // Validate request
        var validationResult = await _createValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors
                .Select(e => new ValidationError { Field = e.PropertyName, Message = e.ErrorMessage })
                .ToList();

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

        // Get current user ID
        var userId = await _userContextService.GetCurrentUserIdAsync();
        var ownerId = userId.ToString();

        // Validate schema version
        _schemaVersionValidator.ValidateCreate(request.EntityType.ToString(), request.SchemaVersion);

        // Create entity
        var entity = WorldEntity.Create(
            worldId,
            request.EntityType,
            request.Name,
            ownerId,
            request.Description,
            request.ParentId,
            request.Tags,
            request.Attributes,
            schemaVersion: request.SchemaVersion ?? 1);

        var createdEntity = await _entityRepository.CreateAsync(entity, cancellationToken);

        var response = MapToResponse(createdEntity);
        var etag = GetETag(createdEntity);

        return CreatedAtAction(
            nameof(GetEntity),
            new { worldId, entityId = createdEntity.Id },
            new ApiResponse<EntityResponse>
            {
                Data = response,
                Meta = new Dictionary<string, object>
                {
                    ["etag"] = etag
                }
            });
    }

    /// <summary>
    /// Retrieves all entities in a world with optional filtering.
    /// </summary>
    /// <param name="worldId">The world identifier.</param>
    /// <param name="parentId">Optional parent entity identifier filter. "null" or empty for root, valid GUID for specific parent.</param>
    /// <param name="type">Optional entity type filter.</param>
    /// <param name="tags">Optional tags filter (comma-separated).</param>
    /// <param name="limit">Maximum number of items to return (default 50, max 200).</param>
    /// <param name="cursor">Continuation cursor from previous response.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Paginated list of entities.</returns>
    [HttpGet]
    [ProducesResponseType<PaginatedApiResponse<EntityResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ErrorResponse>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ErrorResponse>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetEntities(
        Guid worldId,
        [FromQuery] string? parentId = null,
        [FromQuery] EntityType? type = null,
        [FromQuery] string? tags = null,
        [FromQuery] int limit = 50,
        [FromQuery] string? cursor = null,
        CancellationToken cancellationToken = default)
    {
        var tagsList = tags?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();

        // Parse parentId parameter
        // Frontend sends "null" string for root entities (parentId IS NULL)
        // If string is empty or null (not "null" string), also treat as root query if strict filtering desired,
        // BUT strict filtering logic in repo says:
        // - Guid.Empty -> Return ALL entities (ignore hierarchy)
        // - null -> Return ROOT entities (ParentId is null)
        // - Valid GUID -> Return children of that GUID

        Guid? parentIdFilter = null; // Default to filtering for Root entities (null)

        if (!string.IsNullOrEmpty(parentId))
        {
            if (parentId.Equals("null", StringComparison.OrdinalIgnoreCase))
            {
                // Explicitly requesting root entities
                parentIdFilter = null;
            }
            else if (Guid.TryParse(parentId, out var parsedGuid))
            {
                // Requesting children of specific entity
                parentIdFilter = parsedGuid;
            }
            else if (parentId.Equals("all", StringComparison.OrdinalIgnoreCase))
            {
                // Special case: "all" to return flat list of everything
                parentIdFilter = Guid.Empty;
            }
        }

        var (entities, nextCursor) = await _entityRepository.GetAllByWorldAsync(
            worldId,
            parentIdFilter,
            type,
            tagsList,
            limit,
            cursor,
            cancellationToken);

        var responses = entities.Select(MapToResponse).ToList();

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
    /// Retrieves a single entity by its identifier.
    /// </summary>
    /// <param name="worldId">The world identifier.</param>
    /// <param name="entityId">The entity identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The entity details with ETag header.</returns>
    [HttpGet("{entityId:guid}")]
    [ProducesResponseType<ApiResponse<EntityResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ErrorResponse>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ErrorResponse>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetEntity(
        Guid worldId,
        Guid entityId,
        CancellationToken cancellationToken)
    {
        var entity = await _entityRepository.GetByIdAsync(worldId, entityId, cancellationToken);

        if (entity == null)
        {
            return NotFound(new ErrorResponse
            {
                Error = new ErrorDetail
                {
                    Code = "ENTITY_NOT_FOUND",
                    Message = $"Entity with ID '{entityId}' not found in world '{worldId}'."
                }
            });
        }

        var response = MapToResponse(entity);
        var etag = GetETag(entity);

        Response.Headers.Append("ETag", etag);

        return Ok(new ApiResponse<EntityResponse>
        {
            Data = response
        });
    }

    /// <summary>
    /// Updates an existing entity.
    /// </summary>
    /// <param name="worldId">The world identifier.</param>
    /// <param name="entityId">The entity identifier.</param>
    /// <param name="request">The update request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated entity with new ETag.</returns>
    [HttpPut("{entityId:guid}")]
    [ProducesResponseType<ApiResponse<EntityResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ErrorResponse>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ErrorResponse>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ErrorResponse>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ErrorResponse>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateEntity(
        Guid worldId,
        Guid entityId,
        [FromBody] UpdateWorldEntityRequest request,
        CancellationToken cancellationToken)
    {
        // Validate request
        var validationResult = await _updateValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors
                .Select(e => new ValidationError { Field = e.PropertyName, Message = e.ErrorMessage })
                .ToList();

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

        // Retrieve existing entity
        var entity = await _entityRepository.GetByIdAsync(worldId, entityId, cancellationToken);
        if (entity == null)
        {
            return NotFound(new ErrorResponse
            {
                Error = new ErrorDetail
                {
                    Code = "ENTITY_NOT_FOUND",
                    Message = $"Entity with ID '{entityId}' not found in world '{worldId}'."
                }
            });
        }

        // Compute the final schema version that will be used
        var finalSchemaVersion = request.SchemaVersion ?? entity.SchemaVersion;

        // Validate the schema version that will actually be applied
        // Note: Even if request.SchemaVersion is null (using existing version), we validate
        // to ensure consistency and catch any data integrity issues
        _schemaVersionValidator.ValidateUpdate(
            request.EntityType.ToString(),
            entity.SchemaVersion,
            finalSchemaVersion);

        // Update entity
        entity.Update(
            request.Name,
            request.Description,
            request.EntityType,
            request.ParentId,
            request.Tags,
            request.Attributes,
            finalSchemaVersion);

        // Get If-Match header for ETag validation
        var ifMatch = Request.Headers["If-Match"].FirstOrDefault();
        var updatedEntity = await _entityRepository.UpdateAsync(entity, ifMatch, cancellationToken);

        var response = MapToResponse(updatedEntity);
        var newEtag = GetETag(updatedEntity);

        return Ok(new ApiResponse<EntityResponse>
        {
            Data = response,
            Meta = new Dictionary<string, object>
            {
                ["etag"] = newEtag
            }
        });
    }

    /// <summary>
    /// Partially updates an entity (PATCH operation).
    /// </summary>
    /// <param name="worldId">The world identifier.</param>
    /// <param name="entityId">The entity identifier.</param>
    /// <param name="request">The patch request with optional fields.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated entity.</returns>
    [HttpPatch("{entityId:guid}")]
    [ProducesResponseType<ApiResponse<EntityResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ErrorResponse>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ErrorResponse>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ErrorResponse>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> PatchEntity(
        Guid worldId,
        Guid entityId,
        [FromBody] PatchWorldEntityRequest request,
        CancellationToken cancellationToken)
    {
        // Retrieve existing entity
        var entity = await _entityRepository.GetByIdAsync(worldId, entityId, cancellationToken);
        if (entity == null)
        {
            return NotFound(new ErrorResponse
            {
                Error = new ErrorDetail
                {
                    Code = "ENTITY_NOT_FOUND",
                    Message = $"Entity with ID '{entityId}' not found in world '{worldId}'."
                }
            });
        }

        // Merge attributes if provided
        var attributes = entity.GetAttributes();
        if (request.Attributes != null)
        {
            foreach (var kvp in request.Attributes)
            {
                attributes[kvp.Key] = kvp.Value;
            }
        }

        // Update entity with merged values
        entity.Update(
            request.Name ?? entity.Name,
            request.Description ?? entity.Description,
            request.EntityType ?? entity.EntityType,
            request.ParentId ?? entity.ParentId,
            request.Tags ?? entity.Tags,
            request.Attributes != null ? attributes : entity.GetAttributes(),
            request.SchemaVersion ?? entity.SchemaVersion);

        // Get If-Match header for ETag validation
        var ifMatch = Request.Headers["If-Match"].FirstOrDefault();
        var updatedEntity = await _entityRepository.UpdateAsync(entity, ifMatch, cancellationToken);

        var response = MapToResponse(updatedEntity);
        var newEtag = GetETag(updatedEntity);

        return Ok(new ApiResponse<EntityResponse>
        {
            Data = response,
            Meta = new Dictionary<string, object>
            {
                ["etag"] = newEtag
            }
        });
    }

    /// <summary>
    /// Deletes an entity (soft delete).
    /// </summary>
    /// <param name="worldId">The world identifier.</param>
    /// <param name="entityId">The entity identifier.</param>
    /// <param name="cascade">If true, recursively delete all descendants.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>204 No Content on success.</returns>
    [HttpDelete("{entityId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ErrorResponse>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ErrorResponse>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ErrorResponse>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteEntity(
        Guid worldId,
        Guid entityId,
        [FromQuery] bool cascade = false,
        CancellationToken cancellationToken = default)
    {
        await _entityRepository.DeleteAsync(worldId, entityId, cascade, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Retrieves all child entities of a parent entity.
    /// </summary>
    /// <param name="worldId">The world identifier.</param>
    /// <param name="parentId">The parent entity identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Collection of child entities.</returns>
    [HttpGet("{parentId:guid}/children")]
    [ProducesResponseType<ApiResponse<IEnumerable<EntityResponse>>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ErrorResponse>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ErrorResponse>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetChildren(
        Guid worldId,
        Guid parentId,
        CancellationToken cancellationToken)
    {
        var children = await _entityRepository.GetChildrenAsync(worldId, parentId, cancellationToken);
        var responses = children.Select(MapToResponse).ToList();

        return Ok(new ApiResponse<IEnumerable<EntityResponse>>
        {
            Data = responses
        });
    }

    /// <summary>
    /// Moves an entity to a new parent.
    /// </summary>
    /// <param name="worldId">The world identifier.</param>
    /// <param name="entityId">The entity identifier.</param>
    /// <param name="request">The move request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated entity.</returns>
    [HttpPost("{entityId:guid}/move")]
    [ProducesResponseType<ApiResponse<EntityResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ErrorResponse>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ErrorResponse>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ErrorResponse>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> MoveEntity(
        Guid worldId,
        Guid entityId,
        [FromBody] MoveWorldEntityRequest request,
        CancellationToken cancellationToken)
    {
        var entity = await _entityRepository.GetByIdAsync(worldId, entityId, cancellationToken);
        if (entity == null)
        {
            return NotFound(new ErrorResponse
            {
                Error = new ErrorDetail
                {
                    Code = "ENTITY_NOT_FOUND",
                    Message = $"Entity with ID '{entityId}' not found in world '{worldId}'."
                }
            });
        }

        entity.Move(request.NewParentId);
        var updatedEntity = await _entityRepository.UpdateAsync(entity, cancellationToken: cancellationToken);

        var response = MapToResponse(updatedEntity);
        var newEtag = GetETag(updatedEntity);

        return Ok(new ApiResponse<EntityResponse>
        {
            Data = response,
            Meta = new Dictionary<string, object>
            {
                ["etag"] = newEtag
            }
        });
    }

    /// <summary>
    /// Maps a WorldEntity to EntityResponse DTO.
    /// </summary>
    private static EntityResponse MapToResponse(WorldEntity entity)
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

    /// <summary>
    /// Gets the ETag value for an entity (using ModifiedDate as surrogate).
    /// </summary>
    private static string GetETag(WorldEntity entity)
    {
        return $"\"{entity.ModifiedDate.Ticks}\"";
    }
}
