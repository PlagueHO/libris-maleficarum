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
            .IsRequired();

        builder.Property(w => w.OwnerId)
            .IsRequired();

        builder.Property(w => w.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(w => w.Description)
            .HasMaxLength(2000);

        builder.Property(w => w.CreatedDate)
            .IsRequired();

        builder.Property(w => w.ModifiedDate)
            .IsRequired();

        builder.Property(w => w.IsDeleted)
            .IsRequired();
    }
}
