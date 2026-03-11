namespace LibrisMaleficarum.Import.Models;

/// <summary>
/// A validation error found during import source analysis.
/// </summary>
public sealed class ImportValidationError
{
    /// <summary>
    /// Gets the path of the file where the error was found.
    /// </summary>
    public required string FilePath { get; init; }

    /// <summary>
    /// Gets the error code identifying the type of validation failure.
    /// </summary>
    public required string Code { get; init; }

    /// <summary>
    /// Gets the human-readable error message.
    /// </summary>
    public required string Message { get; init; }

    /// <summary>
    /// Gets the optional line number where the error was found.
    /// </summary>
    public int? LineNumber { get; init; }
}
