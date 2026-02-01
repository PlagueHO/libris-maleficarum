namespace LibrisMaleficarum.Api.Models.Responses;

/// <summary>
/// Response DTO for delete operation status.
/// </summary>
public class DeleteOperationResponse
{
    /// <summary>
    /// Gets or sets the unique identifier for this operation.
    /// </summary>
    public required Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the world identifier this operation belongs to.
    /// </summary>
    public required Guid WorldId { get; set; }

    /// <summary>
    /// Gets or sets the root entity ID being deleted.
    /// </summary>
    public required Guid RootEntityId { get; set; }

    /// <summary>
    /// Gets or sets the name of the root entity (for display purposes).
    /// </summary>
    public required string RootEntityName { get; set; }

    /// <summary>
    /// Gets or sets the current status of the operation (pending, in_progress, completed, partial, failed).
    /// </summary>
    public required string Status { get; set; }

    /// <summary>
    /// Gets or sets the total number of entities to delete (including root and all descendants).
    /// </summary>
    public int TotalEntities { get; set; }

    /// <summary>
    /// Gets or sets the number of entities successfully deleted so far.
    /// </summary>
    public int DeletedCount { get; set; }

    /// <summary>
    /// Gets or sets the number of entities that failed to delete.
    /// </summary>
    public int FailedCount { get; set; }

    /// <summary>
    /// Gets or sets the list of entity IDs that failed to delete (for retry/debugging).
    /// </summary>
    public List<Guid>? FailedEntityIds { get; set; }

    /// <summary>
    /// Gets or sets error details if the operation failed.
    /// </summary>
    public string? ErrorDetails { get; set; }

    /// <summary>
    /// Gets or sets whether cascade delete is enabled for this operation.
    /// </summary>
    public bool Cascade { get; set; }

    /// <summary>
    /// Gets or sets the user ID who initiated this operation.
    /// </summary>
    public required string CreatedBy { get; set; }

    /// <summary>
    /// Gets or sets the UTC timestamp when this operation was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the UTC timestamp when this operation started processing.
    /// </summary>
    public DateTime? StartedAt { get; set; }

    /// <summary>
    /// Gets or sets the UTC timestamp when this operation completed (success or failure).
    /// </summary>
    public DateTime? CompletedAt { get; set; }
}
