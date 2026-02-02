namespace LibrisMaleficarum.Domain.Extensions;

using LibrisMaleficarum.Domain.Entities;

/// <summary>
/// Extension methods for DeleteOperationStatus enum.
/// </summary>
public static class DeleteOperationStatusExtensions
{
    /// <summary>
    /// Converts DeleteOperationStatus enum to the documented API string format.
    /// Maps enum values to lowercase with underscores as specified in the API contract.
    /// </summary>
    /// <param name="status">The delete operation status enum value.</param>
    /// <returns>The API string representation matching the documented contract.</returns>
    /// <remarks>
    /// This ensures consistency between the API documentation and actual wire format.
    /// Example: DeleteOperationStatus.InProgress -> "in_progress"
    /// </remarks>
    public static string ToApiString(this DeleteOperationStatus status)
    {
        return status switch
        {
            DeleteOperationStatus.Pending => "pending",
            DeleteOperationStatus.InProgress => "in_progress",
            DeleteOperationStatus.Completed => "completed",
            DeleteOperationStatus.Partial => "partial",
            DeleteOperationStatus.Failed => "failed",
            _ => status.ToString().ToLowerInvariant()
        };
    }
}
