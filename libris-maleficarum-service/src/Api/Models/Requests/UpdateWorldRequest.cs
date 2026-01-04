namespace LibrisMaleficarum.Api.Models.Requests;

/// <summary>
/// Request model for updating an existing world.
/// </summary>
public class UpdateWorldRequest
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
