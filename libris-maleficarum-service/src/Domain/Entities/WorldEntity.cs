namespace LibrisMaleficarum.Domain.Entities;

using LibrisMaleficarum.Domain.ValueObjects;
using System.Text.Json;

/// <summary>
/// Represents a generic entity within a world (e.g., locations, characters, campaigns).
/// Supports hierarchical relationships via ParentId and flexible attributes via Tags and Attributes.
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
    /// Gets the custom attributes as a JSON string (flexible schema).
    /// </summary>
    public string Attributes { get; private set; }

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
    /// Gets the schema version for this entity type's property structure.
    /// Indicates which version of the entity type's schema was used when created/last migrated.
    /// Enables forward schema evolution without requiring bulk updates.
    /// </summary>
    public int SchemaVersion { get; private set; }

    /// <summary>
    /// Private constructor for EF Core.
    /// </summary>
    private WorldEntity()
    {
        Name = string.Empty;
        Tags = [];
        Path = [];
        OwnerId = string.Empty;
        Attributes = "{}";
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
    /// <param name="attributes">Optional custom attributes as dictionary (serialized to JSON, max 100KB).</param>
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
        Dictionary<string, object>? attributes = null,
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
            Name = name,
            Description = description,
            Tags = tags ?? [],
            Path = path,
            Depth = depth,
            HasChildren = false,
            OwnerId = ownerId,
            Attributes = attributes != null ? JsonSerializer.Serialize(attributes) : "{}",
            CreatedDate = DateTime.UtcNow,
            ModifiedDate = DateTime.UtcNow,
            IsDeleted = false,
            SchemaVersion = schemaVersion
        };

        entity.Validate();
        return entity;
    }

    /// <summary>
    /// Updates the entity properties and refreshes ModifiedDate.
    /// </summary>
    /// <param name="name">The new name (1-200 characters).</param>
    /// <param name="description">The new description (max 5000 characters).</param>
    /// <param name="entityType">The new entity type.</param>
    /// <param name="parentId">The new parent ID (null for root-level).</param>
    /// <param name="tags">The new tags list (max 20, each max 50 characters).</param>
    /// <param name="attributes">The new attributes dictionary (max 100KB serialized).</param>
    /// <param name="schemaVersion">The schema version for this entity type.</param>
    public void Update(
        string name,
        string? description,
        EntityType entityType,
        Guid? parentId,
        List<string>? tags,
        Dictionary<string, object>? attributes,
        int schemaVersion)
    {
        Name = name;
        Description = description;
        EntityType = entityType;
        ParentId = parentId;
        Tags = tags ?? [];
        Attributes = attributes != null ? JsonSerializer.Serialize(attributes) : "{}";
        SchemaVersion = schemaVersion;
        ModifiedDate = DateTime.UtcNow;

        Validate();
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

        // Validate Attributes JSON size (max 100KB)
        var attributesBytes = System.Text.Encoding.UTF8.GetByteCount(Attributes);
        if (attributesBytes > 100 * 1024)
        {
            throw new ArgumentException("Attributes must not exceed 100KB serialized.");
        }
    }

    /// <summary>
    /// Gets the attributes as a deserialized dictionary.
    /// </summary>
    /// <returns>Dictionary of custom attributes.</returns>
    public Dictionary<string, object> GetAttributes()
    {
        return JsonSerializer.Deserialize<Dictionary<string, object>>(Attributes) ?? [];
    }
}
