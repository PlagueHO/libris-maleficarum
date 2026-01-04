namespace LibrisMaleficarum.Domain.Entities;

/// <summary>
/// Represents a world - the root aggregate for world-building content.
/// </summary>
public class World
{
    /// <summary>
    /// Gets the unique identifier for this world.
    /// </summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// Gets the unique identifier of the user who owns this world.
    /// </summary>
    public Guid OwnerId { get; private set; }

    /// <summary>
    /// Gets the name of the world.
    /// </summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the description of the world.
    /// </summary>
    public string? Description { get; private set; }

    /// <summary>
    /// Gets the UTC timestamp when this world was created.
    /// </summary>
    public DateTime CreatedDate { get; private set; }

    /// <summary>
    /// Gets the UTC timestamp when this world was last modified.
    /// </summary>
    public DateTime ModifiedDate { get; private set; }

    /// <summary>
    /// Gets a value indicating whether this world has been soft-deleted.
    /// </summary>
    public bool IsDeleted { get; private set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="World"/> class.
    /// Private constructor for domain-driven design.
    /// </summary>
    private World()
    {
    }

    /// <summary>
    /// Creates a new world instance.
    /// </summary>
    /// <param name="ownerId">The unique identifier of the user who owns this world.</param>
    /// <param name="name">The name of the world.</param>
    /// <param name="description">The description of the world.</param>
    /// <returns>A new <see cref="World"/> instance.</returns>
    /// <exception cref="ArgumentException">Thrown when validation fails.</exception>
    public static World Create(Guid ownerId, string name, string? description = null)
    {
        var world = new World
        {
            Id = Guid.NewGuid(),
            OwnerId = ownerId,
            Name = name,
            Description = description,
            CreatedDate = DateTime.UtcNow,
            ModifiedDate = DateTime.UtcNow,
            IsDeleted = false
        };

        world.Validate();
        return world;
    }

    /// <summary>
    /// Validates the world entity properties.
    /// </summary>
    /// <exception cref="ArgumentException">Thrown when validation fails.</exception>
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Name))
        {
            throw new ArgumentException("Name is required.", nameof(Name));
        }

        if (Name.Length < 1 || Name.Length > 100)
        {
            throw new ArgumentException("Name must be between 1 and 100 characters.", nameof(Name));
        }

        if (Description is not null && Description.Length > 2000)
        {
            throw new ArgumentException("Description must not exceed 2000 characters.", nameof(Description));
        }
    }

    /// <summary>
    /// Updates the world with new values.
    /// </summary>
    /// <param name="name">The new name.</param>
    /// <param name="description">The new description.</param>
    /// <exception cref="ArgumentException">Thrown when validation fails.</exception>
    public void Update(string name, string? description)
    {
        Name = name;
        Description = description;
        ModifiedDate = DateTime.UtcNow;

        Validate();
    }

    /// <summary>
    /// Marks the world as soft-deleted.
    /// </summary>
    public void SoftDelete()
    {
        IsDeleted = true;
        ModifiedDate = DateTime.UtcNow;
    }
}
