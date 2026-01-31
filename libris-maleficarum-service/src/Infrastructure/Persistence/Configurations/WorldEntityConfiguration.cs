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
    /// <summary>
    /// Configures the WorldEntity entity type for Cosmos DB.
    /// </summary>
    /// <param name="builder">The entity type builder.</param>
    public void Configure(EntityTypeBuilder<WorldEntity> builder)
    {
        // Map to Cosmos DB container
        builder.ToContainer("WorldEntities");

        // Configure partition key to be /WorldId
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
            .ToJsonProperty("WorldId")
            .IsRequired();

        builder.Property(e => e.ParentId)
            .ToJsonProperty("ParentId");

        builder.Property(e => e.EntityType)
            .ToJsonProperty("EntityType")
            .HasConversion<string>()
            .IsRequired();

        builder.Property(e => e.Name)
            .ToJsonProperty("Name")
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(e => e.Description)
            .ToJsonProperty("Description")
            .HasMaxLength(5000);

        builder.Property(e => e.Tags)
            .ToJsonProperty("Tags")
            .Metadata.SetValueComparer(
                new ValueComparer<List<string>>(
                    (c1, c2) => c1!.SequenceEqual(c2!),
                    c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                    c => c.ToList()));

        builder.Property(e => e.Path)
            .ToJsonProperty("Path")
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<List<Guid>>(v, (JsonSerializerOptions?)null) ?? new List<Guid>())
            .Metadata.SetValueComparer(
                new ValueComparer<List<Guid>>(
                    (c1, c2) => c1!.SequenceEqual(c2!),
                    c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                    c => c.ToList()));

        builder.Property(e => e.Depth)
            .ToJsonProperty("Depth")
            .IsRequired();

        builder.Property(e => e.HasChildren)
            .ToJsonProperty("HasChildren")
            .IsRequired();

        builder.Property(e => e.OwnerId)
            .ToJsonProperty("OwnerId")
            .IsRequired();

        builder.Property(e => e.Attributes)
            .ToJsonProperty("Attributes")
            .IsRequired();

        builder.Property(e => e.CreatedDate)
            .ToJsonProperty("CreatedDate")
            .IsRequired();

        builder.Property(e => e.ModifiedDate)
            .ToJsonProperty("ModifiedDate")
            .IsRequired();

        builder.Property(e => e.IsDeleted)
            .ToJsonProperty("IsDeleted")
            .IsRequired();

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
}
