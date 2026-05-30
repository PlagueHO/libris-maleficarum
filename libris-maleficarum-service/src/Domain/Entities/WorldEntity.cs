namespace LibrisMaleficarum.Domain.Entities;

using LibrisMaleficarum.Domain.ValueObjects;
using System.Text.Json;

/// <summary>
/// Represents a generic entity within a world (e.g., locations, characters, campaigns).
/// Supports hierarchical relationships via ParentId and flexible property bags via Tags, Properties, and SystemProperties.
/// </summary>
public class WorldEntity
{
    /// <summary>
    /// Gets the unique identifier for this entity.
    /// </summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// Gets the identifier of the world this entity belongs to (partition key).
    /// </summary>
    public Guid WorldId { get; private set; }

    /// <summary>
    /// Gets the identifier of the parent entity (null for root-level entities).
    /// </summary>
    public Guid? ParentId { get; private set; }

    /// <summary>
    /// Gets the type of entity (Character, Location, Campaign, etc.).
    /// </summary>
    public EntityType EntityType { get; private set; }

    /// <summary>
    /// Gets the optional schema identifier for this entity's property template.
    /// </summary>
    public string? SchemaId { get; private set; }

    /// <summary>
    /// Gets the name of this entity.
    /// </summary>
    public string Name { get; private set; }

    /// <summary>
    /// Gets the description of this entity.
    /// </summary>
    public string? Description { get; private set; }

    /// <summary>
    /// Gets the list of tags for categorization and filtering.
    /// </summary>
    public List<string> Tags { get; private set; }

    /// <summary>
    /// Gets the array of ancestor IDs from root to parent (for hierarchy queries).
    /// </summary>
    public List<Guid> Path { get; private set; }

    /// <summary>
    /// Gets the hierarchy level (0 = root).
    /// </summary>
    public int Depth { get; private set; }

    /// <summary>
    /// Gets a value indicating whether this entity has children (optimization flag).
    /// </summary>
    public bool HasChildren { get; private set; }

    /// <summary>
    /// Gets the identifier of the user who owns this entity.
    /// </summary>
    public string OwnerId { get; private set; }

    /// <summary>
    /// Gets the common properties for this entity (cross-system).
    /// </summary>
    public Dictionary<string, object>? Properties { get; private set; }

    /// <summary>
    /// Gets the system-specific properties for this entity.
    /// </summary>
    public Dictionary<string, object>? SystemProperties { get; private set; }

    /// <summary>
    /// Gets the UTC timestamp when this entity was created.
    /// </summary>
    public DateTime CreatedDate { get; private set; }

    /// <summary>
    /// Gets the UTC timestamp when this entity was last modified.
    /// </summary>
    public DateTime ModifiedDate { get; private set; }

    /// <summary>
    /// Gets a value indicating whether this entity has been soft-deleted.
    /// </summary>
    public bool IsDeleted { get; private set; }

    /// <summary>
    /// Gets the UTC timestamp when this entity was soft-deleted.
    /// Null if the entity has not been deleted.
    /// </summary>
    public DateTime? DeletedDate { get; private set; }

    /// <summary>
    /// Gets the user ID who soft-deleted this entity.
    /// Null if the entity has not been deleted.
    /// </summary>
    public string? DeletedBy { get; private set; }

    /// <summary>
    /// Gets the user ID who created this entity.
    /// </summary>
    public string? CreatedBy { get; private set; }

    /// <summary>
    /// Gets the user ID who last modified this entity.
    /// </summary>
    public string? ModifiedBy { get; private set; }

    /// <summary>
    /// Gets the schema version for this entity type's property structure.
    /// Indicates which version of the entity type's schema was used when created/last migrated.
    /// Enables forward schema evolution without requiring bulk updates.
    /// </summary>
    public int SchemaVersion { get; private set; }

    /// <summary>
    /// Gets the time-to-live in seconds for automatic deletion by Cosmos DB.
    /// Null means the item doesn't expire (uses container default behavior).
    /// Set to 7776000 (90 days) when soft-deleted for automatic purge.
    /// </summary>
    public int? Ttl { get; private set; }

    /// <summary>
    /// Private constructor for EF Core.
    /// </summary>
    private WorldEntity()
    {
        Name = string.Empty;
        Tags = [];
        Path = [];
        OwnerId = string.Empty;
        Properties = [];
        SystemProperties = [];
    }

    /// <summary>
    /// Creates a new WorldEntity with validation.
    /// </summary>
    /// <param name="worldId">The world identifier.</param>
    /// <param name="entityType">The type of entity.</param>
    /// <param name="name">The entity name (1-200 characters).</param>
    /// <param name="ownerId">The identifier of the user who owns this entity.</param>
    /// <param name="description">Optional description (max 5000 characters).</param>
    /// <param name="parentId">Optional parent entity identifier for hierarchical relationships.</param>
    /// <param name="tags">Optional list of tags (max 20, each max 50 characters).</param>
    /// <param name="schemaId">Optional schema identifier for the entity property template.</param>
    /// <param name="properties">Optional common properties as dictionary (max 100KB serialized).</param>
    /// <param name="systemProperties">Optional system-specific properties as dictionary (max 100KB serialized).</param>
    /// <param name="parentPath">Optional parent's path (for calculating this entity's path).</param>
    /// <param name="parentDepth">Optional parent's depth (for calculating this entity's depth).</param>
    /// <param name="schemaVersion">The schema version for this entity type (defaults to 1).</param>
    /// <returns>A new WorldEntity instance.</returns>
    public static WorldEntity Create(
        Guid worldId,
        EntityType entityType,
        string name,
        string ownerId,
        string? description = null,
        Guid? parentId = null,
        List<string>? tags = null,
        string? schemaId = null,
        Dictionary<string, object>? properties = null,
        Dictionary<string, object>? systemProperties = null,
        List<Guid>? parentPath = null,
        int parentDepth = -1,
        int schemaVersion = 1)
    {
        // Calculate path and depth
        var path = new List<Guid>();
        var depth = 0;

        if (parentId.HasValue)
        {
            // If parent provided, path = parent's path + parent's ID
            if (parentPath != null)
            {
                path.AddRange(parentPath);
            }

            path.Add(parentId.Value);
            depth = parentDepth + 1;
        }

        var entity = new WorldEntity
        {
            Id = Guid.NewGuid(),
            WorldId = worldId,
            ParentId = parentId,
            EntityType = entityType,
            SchemaId = schemaId,
            Name = name,
            Description = description,
            Tags = tags ?? [],
            Path = path,
            Depth = depth,
            HasChildren = false,
            OwnerId = ownerId,
            Properties = properties,
            SystemProperties = systemProperties,
            CreatedDate = DateTime.UtcNow,
            ModifiedDate = DateTime.UtcNow,
            IsDeleted = false,
            SchemaVersion = schemaVersion,
            CreatedBy = ownerId,
            ModifiedBy = ownerId
        };

        entity.Validate();
        return entity;
    }

    /// <summary>
    /// Creates a new WorldEntity using legacy call signature.
    /// </summary>
    public static WorldEntity Create(
        Guid worldId,
        EntityType entityType,
        string name,
        string ownerId,
        string? description,
        Guid? parentId,
        List<string>? tags,
        Dictionary<string, object>? properties,
        List<Guid>? parentPath = null,
        int parentDepth = -1,
        int schemaVersion = 1)
    {
        return Create(
            worldId,
            entityType,
            name,
            ownerId,
            description,
            parentId,
            tags,
            schemaId: null,
            properties,
            systemProperties: null,
            parentPath,
            parentDepth,
            schemaVersion);
    }

    /// <summary>
    /// Updates the entity properties and refreshes ModifiedDate.
    /// </summary>
    /// <param name="name">The new name (1-200 characters).</param>
    /// <param name="description">The new description (max 5000 characters).</param>
    /// <param name="entityType">The new entity type.</param>
    /// <param name="parentId">The new parent ID (null for root-level).</param>
    /// <param name="tags">The new tags list (max 20, each max 50 characters).</param>
    /// <param name="schemaId">The new schema identifier for property template selection.</param>
    /// <param name="properties">The new common properties dictionary (max 100KB serialized).</param>
    /// <param name="systemProperties">The new system-specific properties dictionary (max 100KB serialized).</param>
    /// <param name="schemaVersion">The schema version for this entity type.</param>
    public void Update(
        string name,
        string? description,
        EntityType entityType,
        Guid? parentId,
        List<string>? tags,
        string? schemaId,
        Dictionary<string, object>? properties,
        Dictionary<string, object>? systemProperties,
        int schemaVersion)
    {
        Name = name;
        Description = description;
        EntityType = entityType;
        SchemaId = schemaId;
        ParentId = parentId;
        Tags = tags ?? [];
        Properties = properties;
        SystemProperties = systemProperties;
        SchemaVersion = schemaVersion;
        ModifiedDate = DateTime.UtcNow;

        Validate();
    }

    /// <summary>
    /// Updates a WorldEntity using legacy call signature.
    /// </summary>
    public void Update(
        string name,
        string? description,
        EntityType entityType,
        Guid? parentId,
        List<string>? tags,
        Dictionary<string, object>? properties,
        int schemaVersion)
    {
        Update(
            name,
            description,
            entityType,
            parentId,
            tags,
            SchemaId,
            properties,
            SystemProperties,
            schemaVersion);
    }

    /// <summary>
    /// Updates the ModifiedBy field to track who last modified this entity.
    /// </summary>
    /// <param name="modifiedBy">The user ID performing the modification.</param>
    public void UpdateModifiedBy(string modifiedBy)
    {
        ModifiedBy = modifiedBy;
    }

    /// <summary>
    /// Moves this entity to a new parent.
    /// </summary>
    /// <param name="newParentId">The new parent entity identifier (null for root-level).</param>
    /// <param name="newParentPath">The new parent's path (for calculating this entity's path).</param>
    /// <param name="newParentDepth">The new parent's depth (for calculating this entity's depth).</param>
    public void Move(Guid? newParentId, List<Guid>? newParentPath = null, int newParentDepth = -1)
    {
        ParentId = newParentId;

        // Recalculate path and depth based on new parent
        if (newParentId.HasValue)
        {
            Path = new List<Guid>();
            if (newParentPath != null)
            {
                Path.AddRange(newParentPath);
            }

            Path.Add(newParentId.Value);
            Depth = newParentDepth + 1;
        }
        else
        {
            // Root-level entity
            Path = [];
            Depth = 0;
        }

        ModifiedDate = DateTime.UtcNow;
    }

    /// <summary>
    /// Marks this entity as soft-deleted with audit metadata.
    /// Sets TTL to 90 days (7776000 seconds) for automatic Cosmos DB cleanup.
    /// </summary>
    /// <param name="deletedBy">The user ID performing the deletion.</param>
    public void SoftDelete(string deletedBy)
    {
        if (string.IsNullOrWhiteSpace(deletedBy))
        {
            throw new ArgumentException("DeletedBy is required.", nameof(deletedBy));
        }

        IsDeleted = true;
        DeletedDate = DateTime.UtcNow;
        DeletedBy = deletedBy;
        Ttl = 7776000; // 90 days in seconds (90 * 24 * 60 * 60)
        ModifiedDate = DateTime.UtcNow;
    }

    /// <summary>
    /// Restores a soft-deleted entity by clearing deletion metadata and TTL.
    /// </summary>
    public void Restore()
    {
        IsDeleted = false;
        DeletedDate = null;
        DeletedBy = null;
        Ttl = null; // Remove TTL to prevent auto-deletion
        ModifiedDate = DateTime.UtcNow;
    }

    /// <summary>
    /// Sets the HasChildren flag (typically updated by repository after child count check).
    /// </summary>
    /// <param name="hasChildren">True if this entity has children, false otherwise.</param>
    public void SetHasChildren(bool hasChildren)
    {
        HasChildren = hasChildren;
    }

    /// <summary>
    /// Validates entity properties against business rules.
    /// </summary>
    /// <exception cref="ArgumentException">Thrown when validation fails.</exception>
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Name))
        {
            throw new ArgumentException("Entity name is required.");
        }

        if (Name.Length > 200)
        {
            throw new ArgumentException("Entity name must not exceed 200 characters.");
        }

        if (string.IsNullOrWhiteSpace(OwnerId))
        {
            throw new ArgumentException("OwnerId is required.");
        }

        if (Description?.Length > 5000)
        {
            throw new ArgumentException("Description must not exceed 5000 characters.");
        }

        if (Tags.Count > 20)
        {
            throw new ArgumentException("Maximum 20 tags allowed.");
        }

        if (Tags.Any(t => string.IsNullOrWhiteSpace(t) || t.Length > 50))
        {
            throw new ArgumentException("Each tag must be 1-50 characters.");
        }

        if (Depth < 0 || Depth > 10)
        {
            throw new ArgumentException("Hierarchy depth must be between 0 and 10.");
        }

        if (SchemaVersion < 1)
        {
            throw new ArgumentException("Schema version must be at least 1.");
        }

        // Validate Properties JSON size (max 100KB)
        if (Properties is not null && !ValidatePropertyBagSize(Properties))
        {
            throw new ArgumentException("Properties must not exceed 100KB serialized.");
        }

        // Validate SystemProperties JSON size (max 100KB)
        if (SystemProperties is not null && !ValidatePropertyBagSize(SystemProperties))
        {
            throw new ArgumentException("SystemProperties must not exceed 100KB serialized.");
        }
    }

    private static bool ValidatePropertyBagSize(Dictionary<string, object> propertyBag)
    {
        var json = JsonSerializer.Serialize(propertyBag);
        var sizeBytes = System.Text.Encoding.UTF8.GetByteCount(json);
        return sizeBytes <= 100 * 1024;
    }
}
