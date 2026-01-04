namespace LibrisMaleficarum.Api.Models.Responses;

/// <summary>
/// Response model for world data.
/// </summary>
public class WorldResponse
{
    /// <summary>
    /// Gets or sets the unique identifier of the world.
    /// </summary>
    public required Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the unique identifier of the world owner.
    /// </summary>
    public required Guid OwnerId { get; set; }

    /// <summary>
    /// Gets or sets the name of the world.
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Gets or sets the description of the world.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the UTC timestamp when the world was created.
    /// </summary>
    public required DateTime CreatedDate { get; set; }

    /// <summary>
    /// Gets or sets the UTC timestamp when the world was last modified.
    /// </summary>
    public required DateTime ModifiedDate { get; set; }
}
