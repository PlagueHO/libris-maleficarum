namespace LibrisMaleficarum.Domain.Entities;

/// <summary>
/// Status of a delete operation.
/// </summary>
public enum DeleteOperationStatus
{
    /// <summary>
    /// Operation created but not yet started.
    /// </summary>
    Pending,

    /// <summary>
    /// Operation is currently processing entities.
    /// </summary>
    InProgress,

    /// <summary>
    /// Operation completed successfully (all entities deleted).
    /// </summary>
    Completed,

    /// <summary>
    /// Operation completed with some failures (partial success).
    /// </summary>
    Partial,

    /// <summary>
    /// Operation failed completely.
    /// </summary>
    Failed
}
