namespace LibrisMaleficarum.Api.Models.Requests;

/// <summary>
/// Request model for moving an entity to a new parent.
/// </summary>
public class MoveEntityRequest
{
    /// <summary>
    /// Gets or sets the new parent entity identifier (null for root-level).
    /// </summary>
    public Guid? NewParentId { get; set; }
}
