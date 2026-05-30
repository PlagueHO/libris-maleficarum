using LibrisMaleficarum.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LibrisMaleficarum.Infrastructure.Persistence.Configurations;

/// <summary>
/// Entity Framework Core configuration for the <see cref="World"/> entity.
/// </summary>
public class WorldConfiguration : IEntityTypeConfiguration<World>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<World> builder)
    {
        // Configure Cosmos DB container
        builder.ToContainer("Worlds");

        // Configure partition key
        builder.HasPartitionKey(w => w.Id);

        // Disable discriminator for single-entity container
        builder.HasNoDiscriminator();

        // Configure primary key
        builder.HasKey(w => w.Id);

        // Configure properties
        builder.Property(w => w.Id)
            .ToJsonProperty("id")
            .IsRequired();

        builder.Property(w => w.OwnerId)
            .ToJsonProperty("ownerId")
            .IsRequired();

        builder.Property(w => w.Name)
            .ToJsonProperty("name")
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(w => w.Description)
            .ToJsonProperty("description")
            .HasMaxLength(2000);

        builder.Property(w => w.CreatedAt)
            .ToJsonProperty("createdAt")
            .IsRequired();

        builder.Property(w => w.UpdatedAt)
            .ToJsonProperty("updatedAt")
            .IsRequired();

        builder.Property(w => w.IsDeleted)
            .ToJsonProperty("isDeleted")
            .IsRequired();
    }
}
