namespace LibrisMaleficarum.Import.Models;

/// <summary>
/// Represents the world definition parsed from world.json.
/// </summary>
public sealed class WorldImportDefinition
{
    /// <summary>
    /// Gets the name of the world.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets the optional description of the world.
    /// </summary>
    public string? Description { get; init; }
}
