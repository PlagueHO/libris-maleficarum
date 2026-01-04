namespace LibrisMaleficarum.Api.Models.Requests;

/// <summary>
/// Request model for creating a new world.
/// </summary>
public class CreateWorldRequest
{
    /// <summary>
    /// Gets or sets the name of the world.
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Gets or sets the description of the world.
    /// </summary>
    public string? Description { get; set; }
}
