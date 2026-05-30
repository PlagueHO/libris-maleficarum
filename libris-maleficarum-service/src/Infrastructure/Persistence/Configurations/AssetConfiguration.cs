using LibrisMaleficarum.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LibrisMaleficarum.Infrastructure.Persistence.Configurations;

/// <summary>
/// Entity Framework Core configuration for Asset entity in Cosmos DB.
/// </summary>
public sealed class AssetConfiguration : IEntityTypeConfiguration<Asset>
{
    /// <summary>
    /// Configures the Asset entity for Cosmos DB storage.
    /// </summary>
    /// <param name="builder">Entity type builder.</param>
    public void Configure(EntityTypeBuilder<Asset> builder)
    {
        // Configure Cosmos DB container
        builder.ToContainer("Assets");

        // Configure hierarchical partition key [/worldId, /entityId]
        // This enables efficient entity-scoped queries (2-5 RUs) and prevents hot partitions
        builder.HasPartitionKey(asset => new { asset.WorldId, asset.EntityId });

        // Disable discriminator (not using inheritance)
        builder.HasNoDiscriminator();

        // Configure primary key
        builder.HasKey(asset => asset.Id);

        // Configure properties
        builder.Property(asset => asset.Id)
            .ToJsonProperty("id")
            .IsRequired();

        builder.Property(asset => asset.WorldId)
            .ToJsonProperty("worldId")
            .IsRequired();

        builder.Property(asset => asset.EntityId)
            .ToJsonProperty("entityId")
            .IsRequired();

        builder.Property(asset => asset.FileName)
            .ToJsonProperty("fileName")
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(asset => asset.ContentType)
            .ToJsonProperty("contentType")
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(asset => asset.SizeBytes)
            .ToJsonProperty("sizeBytes")
            .IsRequired();

        builder.Property(asset => asset.BlobUrl)
            .ToJsonProperty("blobUrl")
            .IsRequired()
            .HasMaxLength(2048);

        builder.Property(asset => asset.CreatedAt)
            .ToJsonProperty("createdAt")
            .IsRequired();

        builder.Property(asset => asset.IsDeleted)
            .ToJsonProperty("isDeleted")
            .IsRequired();

        builder.Property(asset => asset.ETag)
            .IsETagConcurrency();

        // Configure ImageDimensions as an owned complex type (value object)
        builder.OwnsOne(asset => asset.ImageDimensions, imageDimensions =>
        {
            imageDimensions.Property(id => id.Width).IsRequired();
            imageDimensions.Property(id => id.Height).IsRequired();
        });

        // Note: Cosmos DB provider does not support HasIndex()
        // Cosmos DB automatically indexes all properties by default
        // Custom indexing policies can be configured via Azure Portal or Azure Bicep
    }
}
