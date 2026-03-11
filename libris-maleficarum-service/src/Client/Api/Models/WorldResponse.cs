namespace LibrisMaleficarum.Api.Client.Models;

/// <summary>
/// Response model for world data returned by the API.
/// </summary>
public sealed class WorldResponse
{
    /// <summary>
    /// Gets the unique identifier of the world.
    /// </summary>
    public required Guid Id { get; init; }

    /// <summary>
    /// Gets the unique identifier of the world owner.
    /// </summary>
    public required Guid OwnerId { get; init; }

    /// <summary>
    /// Gets the name of the world.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets the description of the world.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Gets the UTC timestamp when the world was created.
    /// </summary>
    public required DateTime CreatedDate { get; init; }

    /// <summary>
    /// Gets the UTC timestamp when the world was last modified.
    /// </summary>
    public required DateTime ModifiedDate { get; init; }
}
