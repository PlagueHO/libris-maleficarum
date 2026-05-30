namespace LibrisMaleficarum.Infrastructure.Persistence.Configurations;

using LibrisMaleficarum.Domain.Entities;
using LibrisMaleficarum.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.Text.Json;

/// <summary>
/// EF Core configuration for WorldEntity in Cosmos DB.
/// </summary>
public class WorldEntityConfiguration : IEntityTypeConfiguration<WorldEntity>
{
    private readonly bool _isCosmosDb;

    /// <summary>
    /// Initializes a new instance of the <see cref="WorldEntityConfiguration"/> class.
    /// </summary>
    /// <param name="isCosmosDb">True when configuring for Cosmos DB provider.</param>
    public WorldEntityConfiguration(bool isCosmosDb = true)
    {
        _isCosmosDb = isCosmosDb;
    }

    /// <summary>
    /// Configures the WorldEntity entity type for Cosmos DB.
    /// </summary>
    /// <param name="builder">The entity type builder.</param>
    public void Configure(EntityTypeBuilder<WorldEntity> builder)
    {
        // Map to Cosmos DB container
        builder.ToContainer("WorldEntities");

        // Configure partition key to be /worldId
        // This groups all entities for a single world in the same partition,
        // allowing for efficient single-partition queries when fetching the world tree.
        builder.HasPartitionKey(e => e.WorldId);

        // Configure discriminator to differentiate WorldEntity from DeleteOperation in the same container
        builder.HasDiscriminator<string>("_type")
            .HasValue("WorldEntity");

        // Primary key
        builder.HasKey(e => e.Id);

        // Property configurations with JSON property names
        builder.Property(e => e.Id)
            .ToJsonProperty("id")
            .IsRequired();

        builder.Property(e => e.WorldId)
            .ToJsonProperty("worldId")
            .IsRequired();

        builder.Property(e => e.ParentId)
            .ToJsonProperty("parentId");

        builder.Property(e => e.EntityType)
            .ToJsonProperty("entityType")
            .HasConversion<string>()
            .IsRequired();

        builder.Property(e => e.SchemaId)
            .ToJsonProperty("schemaId")
            .IsRequired(false);

        builder.Property(e => e.Name)
            .ToJsonProperty("name")
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(e => e.Description)
            .ToJsonProperty("description")
            .HasMaxLength(5000);

        builder.Property(e => e.Tags)
            .ToJsonProperty("tags")
            .Metadata.SetValueComparer(
                new ValueComparer<List<string>>(
                    (c1, c2) => c1!.SequenceEqual(c2!),
                    c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                    c => c.ToList()));

        if (_isCosmosDb)
        {
            builder.Property(e => e.Path)
                .ToJsonProperty("path")
                .HasConversion(
                    v => v.Select(pathId => pathId.ToString()).ToList(),
                    v => v.Select(pathId => Guid.Parse(pathId)).ToList())
                .Metadata.SetValueComparer(
                    new ValueComparer<List<Guid>>(
                        (c1, c2) => c1!.SequenceEqual(c2!),
                        c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                        c => c.ToList()));
        }
        else
        {
            builder.Property(e => e.Path)
                .ToJsonProperty("path")
                .Metadata.SetValueComparer(
                    new ValueComparer<List<Guid>>(
                        (c1, c2) => c1!.SequenceEqual(c2!),
                        c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                        c => c.ToList()));
        }

        builder.Property(e => e.Depth)
            .ToJsonProperty("depth")
            .IsRequired();

        builder.Property(e => e.HasChildren)
            .ToJsonProperty("hasChildren")
            .IsRequired();

        builder.Property(e => e.OwnerId)
            .ToJsonProperty("ownerId")
            .IsRequired();

        builder.Property(e => e.CreatedBy)
            .ToJsonProperty("createdBy")
            .IsRequired(false);

        builder.Property(e => e.ModifiedBy)
            .ToJsonProperty("modifiedBy")
            .IsRequired(false);

        var propertiesBuilder = builder.Property(e => e.Properties)
            .ToJsonProperty("properties")
            .HasConversion(
                v => SerializeDictionary(v),
                v => DeserializeDictionary(v))
            .IsRequired(false);

        propertiesBuilder.Metadata.SetValueComparer(new ValueComparer<Dictionary<string, object>?>(
            (left, right) => DictionaryEquals(left, right),
            value => DictionaryHashCode(value),
            value => DictionarySnapshot(value)));

        var systemPropertiesBuilder = builder.Property(e => e.SystemProperties)
            .ToJsonProperty("systemProperties")
            .HasConversion(
                v => SerializeDictionary(v),
                v => DeserializeDictionary(v))
            .IsRequired(false);

        systemPropertiesBuilder.Metadata.SetValueComparer(new ValueComparer<Dictionary<string, object>?>(
            (left, right) => DictionaryEquals(left, right),
            value => DictionaryHashCode(value),
            value => DictionarySnapshot(value)));

        builder.Property(e => e.CreatedAt)
            .ToJsonProperty("createdAt")
            .IsRequired();

        builder.Property(e => e.UpdatedAt)
            .ToJsonProperty("updatedAt")
            .IsRequired();

        builder.Property(e => e.IsDeleted)
            .ToJsonProperty("isDeleted")
            .IsRequired();

        builder.Property(e => e.DeletedDate)
            .ToJsonProperty("deletedDate")
            .IsRequired(false);

        builder.Property(e => e.DeletedBy)
            .ToJsonProperty("deletedBy")
            .IsRequired(false);

        // TTL property for Cosmos DB automatic cleanup
        // Maps to "ttl" (lowercase) - Cosmos DB reserved field name
        // When null, the property is omitted from JSON (not serialized)
        // When set to 7776000 (90 days), Cosmos DB will automatically delete the document
        // after 90 days from the last modified timestamp (_ts)
        builder.Property(e => e.Ttl)
            .ToJsonProperty("ttl")
            .IsRequired(false); // Omit from JSON when null

        // SchemaVersion property with backward compatibility conversion (FR-008)
        // Converts 0 to 1 when reading from database to handle pre-versioning documents.
        // Pre-versioning documents (created before schema versioning was implemented) either:
        // 1. Have no "schemaVersion" field (EF Core defaults to 0)
        // 2. Have "schemaVersion": 0 (Cosmos DB default for missing numeric fields)
        // Converting 0->1 treats these legacy documents as v1, ensuring consistent behavior
        // across the application without requiring a data migration.
        builder.Property(e => e.SchemaVersion)
            .ToJsonProperty("schemaVersion")
            .HasConversion(
                // To database: store as-is
                v => v,
                // From database: treat missing/0 as version 1 for backward compatibility (FR-008)
                v => v == 0 ? 1 : v)
            .IsRequired();
    }

    private static bool DictionaryEquals(Dictionary<string, object>? left, Dictionary<string, object>? right)
    {
        if (ReferenceEquals(left, right))
        {
            return true;
        }

        if (left is null || right is null)
        {
            return false;
        }

        return JsonSerializer.Serialize(left) == JsonSerializer.Serialize(right);
    }

    private static int DictionaryHashCode(Dictionary<string, object>? value)
    {
        return value is null
            ? 0
            : JsonSerializer.Serialize(value).GetHashCode(StringComparison.Ordinal);
    }

    private static Dictionary<string, object>? DictionarySnapshot(Dictionary<string, object>? value)
    {
        if (value is null)
        {
            return null;
        }

        var serialized = JsonSerializer.Serialize(value);
        return JsonSerializer.Deserialize<Dictionary<string, object>>(serialized);
    }

    private static string? SerializeDictionary(Dictionary<string, object>? value)
    {
        return value is null
            ? null
            : JsonSerializer.Serialize(value);
    }

    private static Dictionary<string, object>? DeserializeDictionary(string? json)
    {
        return string.IsNullOrWhiteSpace(json) ? null : JsonSerializer.Deserialize<Dictionary<string, object>>(json);
    }
}
