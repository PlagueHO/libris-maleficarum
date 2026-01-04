using LibrisMaleficarum.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace LibrisMaleficarum.Infrastructure.Persistence;

/// <summary>
/// Entity Framework Core database context for the Libris Maleficarum application.
/// Manages entities stored in Azure Cosmos DB.
/// </summary>
public class ApplicationDbContext : DbContext
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ApplicationDbContext"/> class.
    /// </summary>
    /// <param name="options">The options to be used by the <see cref="DbContext"/>.</param>
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    /// <summary>
    /// Gets or sets the DbSet for World entities.
    /// </summary>
    public DbSet<World> Worlds { get; set; } = null!;

    /// <summary>
    /// Gets or sets the DbSet for WorldEntity entities.
    /// </summary>
    public DbSet<WorldEntity> WorldEntities { get; set; } = null!;

    /// <summary>
    /// Gets or sets the DbSet for Asset entities.
    /// </summary>
    public DbSet<Asset> Assets { get; set; } = null!;

    /// <summary>
    /// Configures the model that was discovered from the entity types exposed in <see cref="DbSet{TEntity}"/> properties.
    /// </summary>
    /// <param name="modelBuilder">The builder used to construct the model for this context.</param>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply entity configurations
        modelBuilder.ApplyConfiguration(new Configurations.WorldConfiguration());
        modelBuilder.ApplyConfiguration(new Configurations.WorldEntityConfiguration());
        modelBuilder.ApplyConfiguration(new Configurations.AssetConfiguration());
    }
}
