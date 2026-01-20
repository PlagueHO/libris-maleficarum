namespace LibrisMaleficarum.Api.Models.Requests;

/// <summary>
/// Request model for moving a world entity to a new parent.
/// </summary>
public class MoveWorldEntityRequest
{
    /// <summary>
    /// Gets or sets the new parent entity identifier (null for root-level).
    /// </summary>
    public Guid? NewParentId { get; set; }
}
