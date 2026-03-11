namespace LibrisMaleficarum.Api.Client.Models;

/// <summary>
/// Request model for creating a new world.
/// </summary>
public sealed class CreateWorldRequest
{
    /// <summary>
    /// Gets the name of the world to create.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets the optional description of the world.
    /// </summary>
    public string? Description { get; init; }
}
