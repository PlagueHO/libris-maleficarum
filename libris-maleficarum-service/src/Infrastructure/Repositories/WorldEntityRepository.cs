namespace LibrisMaleficarum.Infrastructure.Repositories;

using LibrisMaleficarum.Domain.Entities;
using LibrisMaleficarum.Domain.Exceptions;
using LibrisMaleficarum.Domain.Interfaces.Repositories;
using LibrisMaleficarum.Domain.Interfaces.Services;
using LibrisMaleficarum.Domain.ValueObjects;
using LibrisMaleficarum.Infrastructure.Persistence;
using Microsoft.Azure.Cosmos;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Reflection;
using System.Text;
using System.Text.Json;

/// <summary>
/// Repository implementation for WorldEntity using Cosmos SDK stream operations.
/// </summary>
public class WorldEntityRepository : IWorldEntityRepository
{
    private const string WorldEntityDiscriminator = "WorldEntity";
    private const string WorldEntitiesContainerName = "WorldEntities";

    private readonly ApplicationDbContext _context;
    private readonly IUserContextService _userContextService;
    private readonly IWorldRepository _worldRepository;
    private readonly ITelemetryService _telemetryService;

    /// <summary>
    /// Initializes a new instance of the <see cref="WorldEntityRepository"/> class.
    /// </summary>
    /// <param name="context">The application database context.</param>
    /// <param name="userContextService">The user context service for authorization.</param>
    /// <param name="worldRepository">The world repository for validation.</param>
    /// <param name="telemetryService">The telemetry service for tracking metrics and traces.</param>
    public WorldEntityRepository(
        ApplicationDbContext context,
        IUserContextService userContextService,
        IWorldRepository worldRepository,
        ITelemetryService telemetryService)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _userContextService = userContextService ?? throw new ArgumentNullException(nameof(userContextService));
        _worldRepository = worldRepository ?? throw new ArgumentNullException(nameof(worldRepository));
        _telemetryService = telemetryService ?? throw new ArgumentNullException(nameof(telemetryService));
    }

    /// <inheritdoc/>
    public async Task<WorldEntity?> GetByIdAsync(Guid worldId, Guid entityId, CancellationToken cancellationToken = default)
    {
        await EnsureWorldAccessAsync(worldId, cancellationToken);
        var lookup = await GetByIdInternalAsync(worldId, entityId, includeDeleted: false, cancellationToken);
        return lookup?.Entity;
    }

    /// <inheritdoc/>
    public async Task<WorldEntity?> GetByIdIncludingDeletedAsync(Guid worldId, Guid entityId, CancellationToken cancellationToken = default)
    {
        await EnsureWorldAccessAsync(worldId, cancellationToken);
        var lookup = await GetByIdInternalAsync(worldId, entityId, includeDeleted: true, cancellationToken);
        return lookup?.Entity;
    }

    /// <inheritdoc/>
    public async Task<(IEnumerable<WorldEntity> Entities, string? NextCursor)> GetAllByWorldAsync(
        Guid worldId,
        Guid? parentId = null,
        EntityType? entityType = null,
        List<string>? tags = null,
        int limit = 50,
        string? cursor = null,
        CancellationToken cancellationToken = default)
    {
        await EnsureWorldAccessAsync(worldId, cancellationToken);

        limit = Math.Clamp(limit, 1, 200);
        var take = limit + 1;

        var sqlBuilder = new StringBuilder(
            "SELECT TOP @take * FROM c WHERE c._type = @discriminator AND c.worldId = @worldId AND c.isDeleted = false");

        var parameters = new Dictionary<string, object?>
        {
            ["@take"] = take,
            ["@discriminator"] = WorldEntityDiscriminator,
            ["@worldId"] = worldId.ToString("D"),
        };

        if (parentId != Guid.Empty)
        {
            if (parentId is null)
            {
                sqlBuilder.Append(" AND (NOT IS_DEFINED(c.parentId) OR IS_NULL(c.parentId))");
            }
            else
            {
                sqlBuilder.Append(" AND c.parentId = @parentId");
                parameters["@parentId"] = parentId.Value.ToString("D");
            }
        }

        if (!string.IsNullOrWhiteSpace(cursor) && DateTime.TryParse(cursor, out _))
        {
            sqlBuilder.Append(" AND c.createdAt > @cursor");
            parameters["@cursor"] = cursor;
        }

        if (entityType.HasValue)
        {
            sqlBuilder.Append(" AND c.entityType = @entityType");
            parameters["@entityType"] = entityType.Value.ToString();
        }

        if (tags is not null && tags.Count > 0)
        {
            for (var i = 0; i < tags.Count; i++)
            {
                var tagParameterName = $"@tag{i}";
                sqlBuilder.Append($" AND EXISTS(SELECT VALUE t FROM t IN c.tags WHERE CONTAINS(t, {tagParameterName}, true))");
                parameters[tagParameterName] = tags[i];
            }
        }

        sqlBuilder.Append(" ORDER BY c.createdAt, c.id");
        var query = new QueryDefinition(sqlBuilder.ToString());
        foreach (var parameter in parameters)
        {
            query.WithParameter(parameter.Key, parameter.Value);
        }

        var documents = await ExecuteQueryAsync(query, worldId, cancellationToken);
        var entities = documents.Select(ToDomainEntity).ToList();

        string? nextCursor = null;
        if (entities.Count > limit)
        {
            var pageBoundary = entities[limit - 1];
            entities.RemoveAt(limit);
            nextCursor = pageBoundary.CreatedAt.ToString("O");
        }

        return (entities, nextCursor);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<WorldEntity>> GetChildrenAsync(Guid worldId, Guid parentId, CancellationToken cancellationToken = default)
    {
        await EnsureWorldAccessAsync(worldId, cancellationToken);
        return await GetChildrenInternalAsync(worldId, parentId, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<WorldEntity>> GetDescendantsAsync(Guid entityId, Guid worldId, CancellationToken cancellationToken = default)
    {
        await EnsureWorldAccessAsync(worldId, cancellationToken);

        var descendants = new List<WorldEntity>();
        var queue = new Queue<Guid>();
        queue.Enqueue(entityId);

        while (queue.Count > 0)
        {
            var currentId = queue.Dequeue();
            var children = await GetChildrenInternalAsync(worldId, currentId, cancellationToken);

            foreach (var child in children)
            {
                descendants.Add(child);
                queue.Enqueue(child.Id);
            }
        }

        return descendants;
    }

    /// <inheritdoc/>
    public async Task<WorldEntity> CreateAsync(WorldEntity entity, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entity);

        var currentUserId = await EnsureWorldAccessAsync(entity.WorldId, cancellationToken);

        if (entity.ParentId.HasValue)
        {
            var parentLookup = await GetByIdInternalAsync(entity.WorldId, entity.ParentId.Value, includeDeleted: false, cancellationToken);
            if (parentLookup is null)
            {
                throw new EntityNotFoundException(
                    entity.WorldId,
                    entity.ParentId.Value,
                    $"Parent entity with ID '{entity.ParentId.Value}' not found.");
            }

            if (!parentLookup.Entity.HasChildren)
            {
                parentLookup.Entity.SetHasChildren(true);
                parentLookup.Entity.UpdateModifiedBy(currentUserId);
                await ReplaceItemAsync(parentLookup.Entity, parentLookup.ETag, cancellationToken);
            }
        }

        using var activity = _telemetryService.StartActivity("CreateWorldEntity", new Dictionary<string, object>
        {
            { "world.id", entity.WorldId },
            { "entity.id", entity.Id },
            { "entity.name", entity.Name },
            { "entity.type", entity.EntityType.ToString() },
            { "entity.parent_id", entity.ParentId?.ToString() ?? "null" }
        });

        try
        {
            var container = GetWorldEntitiesContainer();
            using var content = CreateDocumentContent(entity);
            using var response = await container.CreateItemStreamAsync(
                content,
                new PartitionKey(entity.WorldId.ToString("D")),
                cancellationToken: cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                throw new CosmosException(
                    $"Failed to create world entity. Status code: {response.StatusCode}",
                    response.StatusCode,
                    (int)response.StatusCode,
                    response.Headers.ActivityId,
                    response.Headers.RequestCharge);
            }

            _telemetryService.RecordEntityCreated(entity.EntityType.ToString());
            return entity;
        }
        finally
        {
            activity?.Dispose();
        }
    }

    /// <inheritdoc/>
    public async Task<WorldEntity> UpdateAsync(WorldEntity entity, string? etag = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entity);

        await EnsureWorldAccessAsync(entity.WorldId, cancellationToken);

        var existingLookup = await GetByIdInternalAsync(entity.WorldId, entity.Id, includeDeleted: false, cancellationToken);
        if (existingLookup is null)
        {
            throw new EntityNotFoundException(entity.WorldId, entity.Id);
        }

        if (!string.IsNullOrWhiteSpace(etag) && !string.Equals(existingLookup.ETag, etag, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"ETag mismatch: Expected '{etag}' but current is '{existingLookup.ETag}'. The entity may have been modified by another request.");
        }

        var oldParentId = existingLookup.Entity.ParentId;
        var newParentId = entity.ParentId;

        if (newParentId.HasValue && newParentId != oldParentId)
        {
            await ValidateNoCircularReferenceAsync(entity.WorldId, entity.Id, newParentId.Value, cancellationToken);
        }

        if (oldParentId != newParentId)
        {
            if (newParentId.HasValue)
            {
                var newParentLookup = await GetByIdInternalAsync(entity.WorldId, newParentId.Value, includeDeleted: false, cancellationToken);
                if (newParentLookup != null && !newParentLookup.Entity.HasChildren)
                {
                    newParentLookup.Entity.SetHasChildren(true);
                    await ReplaceItemAsync(newParentLookup.Entity, newParentLookup.ETag, cancellationToken);
                }
            }

            if (oldParentId.HasValue)
            {
                var oldParentLookup = await GetByIdInternalAsync(entity.WorldId, oldParentId.Value, includeDeleted: false, cancellationToken);
                if (oldParentLookup != null)
                {
                    var remainingChildrenAny = await AnyChildrenExceptAsync(entity.WorldId, oldParentId.Value, entity.Id, cancellationToken);
                    if (!remainingChildrenAny && oldParentLookup.Entity.HasChildren)
                    {
                        oldParentLookup.Entity.SetHasChildren(false);
                        await ReplaceItemAsync(oldParentLookup.Entity, oldParentLookup.ETag, cancellationToken);
                    }
                }
            }
        }

        await ReplaceItemAsync(entity, etag, cancellationToken);
        return entity;
    }

    /// <inheritdoc/>
    public async Task<int> DeleteAsync(Guid worldId, Guid entityId, string deletedBy, bool cascade = false, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(deletedBy))
        {
            throw new ArgumentException("DeletedBy is required.", nameof(deletedBy));
        }

        await EnsureWorldAccessAsync(worldId, cancellationToken);

        var entityLookup = await GetByIdInternalAsync(worldId, entityId, includeDeleted: false, cancellationToken);
        if (entityLookup is null)
        {
            throw new EntityNotFoundException(worldId, entityId);
        }

        var entity = entityLookup.Entity;

        var children = await GetChildrenInternalAsync(worldId, entityId, cancellationToken);
        var childrenList = children.ToList();

        if (childrenList.Count > 0 && !cascade)
        {
            throw new InvalidOperationException(
                $"Cannot delete entity '{entityId}' because it has {childrenList.Count} child entities. " +
                "Use cascade=true to delete all descendants.");
        }

        var deletedCount = 0;

        using var activity = _telemetryService.StartActivity("DeleteWorldEntity", new Dictionary<string, object>
        {
            { "world.id", worldId },
            { "entity.id", entityId },
            { "entity.name", entity.Name },
            { "entity.type", entity.EntityType.ToString() },
            { "cascade", cascade },
            { "user.id", deletedBy }
        });

        try
        {
            if (cascade && childrenList.Count > 0)
            {
                foreach (var child in childrenList)
                {
                    deletedCount += await DeleteAsync(worldId, child.Id, deletedBy, cascade: true, cancellationToken);
                }
            }

            if (entity.ParentId.HasValue)
            {
                var parentLookup = await GetByIdInternalAsync(worldId, entity.ParentId.Value, includeDeleted: false, cancellationToken);
                if (parentLookup != null)
                {
                    var remainingChildrenExist = await AnyChildrenExceptAsync(worldId, entity.ParentId.Value, entityId, cancellationToken);
                    if (!remainingChildrenExist && parentLookup.Entity.HasChildren)
                    {
                        parentLookup.Entity.SetHasChildren(false);
                        await ReplaceItemAsync(parentLookup.Entity, parentLookup.ETag, cancellationToken);
                    }
                }
            }

            entity.SoftDelete(deletedBy);
            await ReplaceItemAsync(entity, entityLookup.ETag, cancellationToken);
            deletedCount++;

            _telemetryService.RecordEntityDeleted(entity.EntityType.ToString());
            activity?.AddTag("deleted_count", deletedCount);

            return deletedCount;
        }
        finally
        {
            activity?.Dispose();
        }
    }

    /// <inheritdoc/>
    public async Task<int> CountChildrenAsync(Guid worldId, Guid entityId, CancellationToken cancellationToken = default)
    {
        await EnsureWorldAccessAsync(worldId, cancellationToken);

        var query = new QueryDefinition(
            "SELECT VALUE COUNT(1) FROM c WHERE c._type = @discriminator AND c.worldId = @worldId AND c.parentId = @parentId AND c.isDeleted = false")
            .WithParameter("@discriminator", WorldEntityDiscriminator)
            .WithParameter("@worldId", worldId.ToString("D"))
            .WithParameter("@parentId", entityId.ToString("D"));

        var countValues = await ExecuteScalarQueryAsync(query, worldId, cancellationToken);
        return countValues.FirstOrDefault();
    }

    private async Task ValidateNoCircularReferenceAsync(Guid worldId, Guid entityId, Guid newParentId, CancellationToken cancellationToken)
    {
        var currentParentId = newParentId;
        var visited = new HashSet<Guid> { entityId };

        while (currentParentId != Guid.Empty)
        {
            if (visited.Contains(currentParentId))
            {
                throw new InvalidOperationException(
                    $"Circular reference detected: Entity '{entityId}' cannot have ancestor '{currentParentId}' as its parent.");
            }

            visited.Add(currentParentId);

            var parent = await GetByIdInternalAsync(worldId, currentParentId, includeDeleted: false, cancellationToken);
            if (parent is null)
            {
                break;
            }

            currentParentId = parent.Entity.ParentId ?? Guid.Empty;
        }
    }

    private async Task<string> EnsureWorldAccessAsync(Guid worldId, CancellationToken cancellationToken)
    {
        var currentUserId = await _userContextService.GetCurrentUserIdAsync();
        var world = await _worldRepository.GetByIdAsync(worldId, cancellationToken);

        if (world == null)
        {
            throw new WorldNotFoundException(worldId);
        }

        if (!string.Equals(world.OwnerId, currentUserId, StringComparison.Ordinal))
        {
            throw new UnauthorizedWorldAccessException(worldId, currentUserId);
        }

        return currentUserId;
    }

    private Container GetWorldEntitiesContainer()
    {
        var client = _context.Database.GetCosmosClient();
        var databaseId = _context.Database.GetCosmosDatabaseId();
        return client.GetContainer(databaseId, WorldEntitiesContainerName);
    }

    private async Task<WorldEntityLookupResult?> GetByIdInternalAsync(
        Guid worldId,
        Guid entityId,
        bool includeDeleted,
        CancellationToken cancellationToken)
    {
        var container = GetWorldEntitiesContainer();

        try
        {
            using var response = await container.ReadItemStreamAsync(
                entityId.ToString("D"),
                new PartitionKey(worldId.ToString("D")),
                cancellationToken: cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                if (response.StatusCode == HttpStatusCode.NotFound)
                {
                    return null;
                }

                throw new CosmosException(
                    $"Failed to read world entity. Status code: {response.StatusCode}",
                    response.StatusCode,
                    (int)response.StatusCode,
                    response.Headers.ActivityId,
                    response.Headers.RequestCharge);
            }

            using var document = await JsonDocument.ParseAsync(response.Content, cancellationToken: cancellationToken);
            var root = document.RootElement;

            if (!root.TryGetProperty("_type", out var typeProperty) ||
                !string.Equals(typeProperty.GetString(), WorldEntityDiscriminator, StringComparison.Ordinal))
            {
                return null;
            }

            var entity = ToDomainEntity(root);
            if (!includeDeleted && entity.IsDeleted)
            {
                return null;
            }

            return new WorldEntityLookupResult(entity, response.Headers.ETag);
        }
        catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    private async Task<List<JsonElement>> ExecuteQueryAsync(QueryDefinition query, Guid worldId, CancellationToken cancellationToken)
    {
        var container = GetWorldEntitiesContainer();
        using var iterator = container.GetItemQueryStreamIterator(
            query,
            requestOptions: new QueryRequestOptions
            {
                PartitionKey = new PartitionKey(worldId.ToString("D")),
            });

        var documents = new List<JsonElement>();

        while (iterator.HasMoreResults)
        {
            using var response = await iterator.ReadNextAsync(cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                throw new CosmosException(
                    $"Failed to execute query. Status code: {response.StatusCode}",
                    response.StatusCode,
                    (int)response.StatusCode,
                    response.Headers.ActivityId,
                    response.Headers.RequestCharge);
            }

            using var json = await JsonDocument.ParseAsync(response.Content, cancellationToken: cancellationToken);
            if (!json.RootElement.TryGetProperty("Documents", out var docsElement) || docsElement.ValueKind != JsonValueKind.Array)
            {
                continue;
            }

            documents.AddRange(docsElement.EnumerateArray().Select(doc => doc.Clone()));
        }

        return documents;
    }

    private async Task<List<int>> ExecuteScalarQueryAsync(QueryDefinition query, Guid worldId, CancellationToken cancellationToken)
    {
        var container = GetWorldEntitiesContainer();
        using var iterator = container.GetItemQueryStreamIterator(
            query,
            requestOptions: new QueryRequestOptions
            {
                PartitionKey = new PartitionKey(worldId.ToString("D")),
            });

        var values = new List<int>();

        while (iterator.HasMoreResults)
        {
            using var response = await iterator.ReadNextAsync(cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                throw new CosmosException(
                    $"Failed to execute scalar query. Status code: {response.StatusCode}",
                    response.StatusCode,
                    (int)response.StatusCode,
                    response.Headers.ActivityId,
                    response.Headers.RequestCharge);
            }

            using var json = await JsonDocument.ParseAsync(response.Content, cancellationToken: cancellationToken);
            if (!json.RootElement.TryGetProperty("Documents", out var docsElement) || docsElement.ValueKind != JsonValueKind.Array)
            {
                continue;
            }

            foreach (var value in docsElement.EnumerateArray())
            {
                if (value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out var intValue))
                {
                    values.Add(intValue);
                }
            }
        }

        return values;
    }

    private async Task<List<WorldEntity>> GetChildrenInternalAsync(Guid worldId, Guid parentId, CancellationToken cancellationToken)
    {
        var query = new QueryDefinition(
            "SELECT * FROM c WHERE c._type = @discriminator AND c.worldId = @worldId AND c.parentId = @parentId AND c.isDeleted = false")
            .WithParameter("@discriminator", WorldEntityDiscriminator)
            .WithParameter("@worldId", worldId.ToString("D"))
            .WithParameter("@parentId", parentId.ToString("D"));

        var documents = await ExecuteQueryAsync(query, worldId, cancellationToken);
        return documents.Select(ToDomainEntity).ToList();
    }

    private async Task<bool> AnyChildrenExceptAsync(Guid worldId, Guid parentId, Guid exceptEntityId, CancellationToken cancellationToken)
    {
        var query = new QueryDefinition(
            "SELECT TOP 1 VALUE c.id FROM c WHERE c._type = @discriminator AND c.worldId = @worldId AND c.parentId = @parentId AND c.isDeleted = false AND c.id != @exceptId")
            .WithParameter("@discriminator", WorldEntityDiscriminator)
            .WithParameter("@worldId", worldId.ToString("D"))
            .WithParameter("@parentId", parentId.ToString("D"))
            .WithParameter("@exceptId", exceptEntityId.ToString("D"));

        var container = GetWorldEntitiesContainer();
        using var iterator = container.GetItemQueryStreamIterator(
            query,
            requestOptions: new QueryRequestOptions
            {
                PartitionKey = new PartitionKey(worldId.ToString("D")),
            });

        while (iterator.HasMoreResults)
        {
            using var response = await iterator.ReadNextAsync(cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                throw new CosmosException(
                    $"Failed to query sibling entities. Status code: {response.StatusCode}",
                    response.StatusCode,
                    (int)response.StatusCode,
                    response.Headers.ActivityId,
                    response.Headers.RequestCharge);
            }

            using var json = await JsonDocument.ParseAsync(response.Content, cancellationToken: cancellationToken);
            if (!json.RootElement.TryGetProperty("Documents", out var docsElement) || docsElement.ValueKind != JsonValueKind.Array)
            {
                continue;
            }

            if (docsElement.GetArrayLength() > 0)
            {
                return true;
            }
        }

        return false;
    }

    private async Task ReplaceItemAsync(WorldEntity entity, string? etag, CancellationToken cancellationToken)
    {
        var container = GetWorldEntitiesContainer();
        var requestOptions = string.IsNullOrWhiteSpace(etag)
            ? null
            : new ItemRequestOptions { IfMatchEtag = etag };

        using var content = CreateDocumentContent(entity);
        using var response = await container.ReplaceItemStreamAsync(
            content,
            entity.Id.ToString("D"),
            new PartitionKey(entity.WorldId.ToString("D")),
            requestOptions,
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new CosmosException(
                $"Failed to replace world entity. Status code: {response.StatusCode}",
                response.StatusCode,
                (int)response.StatusCode,
                response.Headers.ActivityId,
                response.Headers.RequestCharge);
        }
    }

    private static MemoryStream CreateDocumentContent(WorldEntity entity)
    {
        var payload = new Dictionary<string, object?>
        {
            ["id"] = entity.Id.ToString("D"),
            ["worldId"] = entity.WorldId.ToString("D"),
            ["parentId"] = entity.ParentId?.ToString("D"),
            ["entityType"] = entity.EntityType.ToString(),
            ["schemaId"] = entity.SchemaId,
            ["name"] = entity.Name,
            ["description"] = entity.Description,
            ["tags"] = entity.Tags,
            ["path"] = entity.Path.Select(pathId => pathId.ToString("D")).ToList(),
            ["depth"] = entity.Depth,
            ["hasChildren"] = entity.HasChildren,
            ["ownerId"] = entity.OwnerId,
            ["properties"] = entity.Properties,
            ["systemProperties"] = entity.SystemProperties,
            ["createdAt"] = entity.CreatedAt,
            ["updatedAt"] = entity.UpdatedAt,
            ["isDeleted"] = entity.IsDeleted,
            ["deletedDate"] = entity.DeletedDate,
            ["deletedBy"] = entity.DeletedBy,
            ["createdBy"] = entity.CreatedBy,
            ["modifiedBy"] = entity.ModifiedBy,
            ["schemaVersion"] = entity.SchemaVersion,
            ["ttl"] = entity.Ttl,
            ["_type"] = WorldEntityDiscriminator,
        };

        var json = JsonSerializer.Serialize(payload);
        return new MemoryStream(Encoding.UTF8.GetBytes(json));
    }

    private static WorldEntity ToDomainEntity(JsonElement document)
    {
        var worldId = Guid.Parse(document.GetProperty("worldId").GetString()!);
        var parentId = document.TryGetProperty("parentId", out var parentIdElement) &&
                       parentIdElement.ValueKind == JsonValueKind.String &&
                       Guid.TryParse(parentIdElement.GetString(), out var parsedParentId)
            ? (Guid?)parsedParentId
            : null;

        var entityType = ParseEntityType(document.GetProperty("entityType"));
        var name = document.GetProperty("name").GetString()!;
        var ownerId = document.GetProperty("ownerId").GetString()!;
        var schemaId = document.TryGetProperty("schemaId", out var schemaIdElement) && schemaIdElement.ValueKind == JsonValueKind.String
            ? schemaIdElement.GetString()
            : null;
        var description = document.TryGetProperty("description", out var descriptionElement) && descriptionElement.ValueKind == JsonValueKind.String
            ? descriptionElement.GetString()
            : null;

        var tags = ParseStringList(document, "tags");
        var path = ParseGuidPath(document, "path");
        var depth = document.TryGetProperty("depth", out var depthElement) && depthElement.TryGetInt32(out var parsedDepth)
            ? parsedDepth
            : 0;
        var schemaVersion = document.TryGetProperty("schemaVersion", out var schemaVersionElement) && schemaVersionElement.TryGetInt32(out var parsedSchemaVersion)
            ? parsedSchemaVersion
            : 1;

        var properties = document.TryGetProperty("properties", out var propertiesElement)
            ? ParsePropertyBag(propertiesElement)
            : null;
        var systemProperties = document.TryGetProperty("systemProperties", out var systemPropertiesElement)
            ? ParsePropertyBag(systemPropertiesElement)
            : null;

        var entity = WorldEntity.Create(
            worldId,
            entityType,
            name,
            ownerId,
            description,
            parentId,
            tags,
            schemaId,
            properties,
            systemProperties,
            parentPath: parentId.HasValue ? path.Take(Math.Max(path.Count - 1, 0)).ToList() : null,
            parentDepth: parentId.HasValue ? depth - 1 : -1,
            schemaVersion: schemaVersion);

        SetPrivateProperty(entity, nameof(WorldEntity.Id), Guid.Parse(document.GetProperty("id").GetString()!));
        SetPrivateProperty(entity, nameof(WorldEntity.Path), path);
        SetPrivateProperty(entity, nameof(WorldEntity.Depth), depth);
        SetPrivateProperty(entity, nameof(WorldEntity.HasChildren), document.TryGetProperty("hasChildren", out var hasChildrenElement) && hasChildrenElement.GetBoolean());
        SetPrivateProperty(entity, nameof(WorldEntity.CreatedAt), ParseDateTime(document, "createdAt"));
        SetPrivateProperty(entity, nameof(WorldEntity.UpdatedAt), ParseDateTime(document, "updatedAt"));
        SetPrivateProperty(entity, nameof(WorldEntity.IsDeleted), document.TryGetProperty("isDeleted", out var isDeletedElement) && isDeletedElement.GetBoolean());
        SetPrivateProperty(entity, nameof(WorldEntity.DeletedDate), ParseNullableDateTime(document, "deletedDate"));
        SetPrivateProperty(entity, nameof(WorldEntity.DeletedBy), ParseNullableString(document, "deletedBy"));
        SetPrivateProperty(entity, nameof(WorldEntity.CreatedBy), ParseNullableString(document, "createdBy"));
        SetPrivateProperty(entity, nameof(WorldEntity.ModifiedBy), ParseNullableString(document, "modifiedBy"));
        SetPrivateProperty(entity, nameof(WorldEntity.Ttl), ParseNullableInt(document, "ttl"));

        return entity;
    }

    private static EntityType ParseEntityType(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.String)
        {
            var value = element.GetString();
            if (Enum.TryParse<EntityType>(value, ignoreCase: true, out var parsedEnum))
            {
                return parsedEnum;
            }
        }

        if (element.ValueKind == JsonValueKind.Number && element.TryGetInt32(out var enumValue))
        {
            return (EntityType)enumValue;
        }

        throw new InvalidOperationException($"Invalid EntityType value '{element.GetRawText()}'.");
    }

    private static List<string> ParseStringList(JsonElement document, string propertyName)
    {
        if (!document.TryGetProperty(propertyName, out var element) || element.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        return element.EnumerateArray()
            .Where(item => item.ValueKind == JsonValueKind.String)
            .Select(item => item.GetString()!)
            .ToList();
    }

    private static List<Guid> ParseGuidPath(JsonElement document, string propertyName)
    {
        if (!document.TryGetProperty(propertyName, out var element) || element.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        var result = new List<Guid>();
        foreach (var item in element.EnumerateArray())
        {
            if (item.ValueKind == JsonValueKind.String && Guid.TryParse(item.GetString(), out var parsed))
            {
                result.Add(parsed);
            }
        }

        return result;
    }

    private static Dictionary<string, object>? ParsePropertyBag(JsonElement element)
    {
        if (element.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
        {
            return null;
        }

        if (element.ValueKind == JsonValueKind.String)
        {
            var raw = element.GetString();
            return string.IsNullOrWhiteSpace(raw)
                ? null
                : JsonSerializer.Deserialize<Dictionary<string, object>>(raw);
        }

        if (element.ValueKind == JsonValueKind.Object)
        {
            return JsonSerializer.Deserialize<Dictionary<string, object>>(element.GetRawText());
        }

        throw new InvalidOperationException("Expected property bag to be either JSON object, JSON string, or null.");
    }

    private static DateTime ParseDateTime(JsonElement document, string propertyName)
    {
        return document.TryGetProperty(propertyName, out var element) && element.ValueKind == JsonValueKind.String
            ? DateTime.Parse(element.GetString()!, null, System.Globalization.DateTimeStyles.RoundtripKind)
            : DateTime.UtcNow;
    }

    private static DateTime? ParseNullableDateTime(JsonElement document, string propertyName)
    {
        if (!document.TryGetProperty(propertyName, out var element) || element.ValueKind != JsonValueKind.String)
        {
            return null;
        }

        return DateTime.Parse(element.GetString()!, null, System.Globalization.DateTimeStyles.RoundtripKind);
    }

    private static string? ParseNullableString(JsonElement document, string propertyName)
    {
        return document.TryGetProperty(propertyName, out var element) && element.ValueKind == JsonValueKind.String
            ? element.GetString()
            : null;
    }

    private static int? ParseNullableInt(JsonElement document, string propertyName)
    {
        return document.TryGetProperty(propertyName, out var element) && element.ValueKind == JsonValueKind.Number && element.TryGetInt32(out var value)
            ? value
            : null;
    }

    private static void SetPrivateProperty<T>(WorldEntity entity, string propertyName, T value)
    {
        var propertyInfo = typeof(WorldEntity).GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException($"Property '{propertyName}' not found on WorldEntity.");

        propertyInfo.SetValue(entity, value);
    }

    private sealed record WorldEntityLookupResult(WorldEntity Entity, string? ETag);
}
