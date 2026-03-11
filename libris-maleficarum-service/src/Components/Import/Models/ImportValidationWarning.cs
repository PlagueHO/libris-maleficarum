namespace LibrisMaleficarum.Import.Models;

/// <summary>
/// A non-blocking warning found during import source analysis.
/// </summary>
public sealed class ImportValidationWarning
{
    /// <summary>
    /// Gets the path of the file where the warning was found.
    /// </summary>
    public required string FilePath { get; init; }

    /// <summary>
    /// Gets the warning code identifying the type of issue.
    /// </summary>
    public required string Code { get; init; }

    /// <summary>
    /// Gets the human-readable warning message.
    /// </summary>
    public required string Message { get; init; }
}
