namespace LibrisMaleficarum.Infrastructure.Persistence.Configurations;

using LibrisMaleficarum.Domain.Entities;
using LibrisMaleficarum.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
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

        // Configure hierarchical partition key [/WorldId, /id]
        // This enables efficient queries and prevents hot partitions by distributing
        // each entity into its own logical partition
        builder.HasPartitionKey(e => new { e.WorldId, e.Id });

        // Disable discriminator for single-type container
        builder.HasNoDiscriminator();

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
            .IsRequired();

        builder.Property(e => e.Path)
            .ToJsonProperty("Path")
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<List<Guid>>(v, (JsonSerializerOptions?)null) ?? new List<Guid>())
            .IsRequired();

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
    }
}
