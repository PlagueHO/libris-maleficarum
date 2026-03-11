namespace LibrisMaleficarum.Import.Models;

/// <summary>
/// Result of import source validation.
/// </summary>
public sealed class ImportValidationResult
{
    /// <summary>
    /// Gets a value indicating whether the import source is valid (no errors).
    /// </summary>
    public bool IsValid => Errors.Count == 0;

    /// <summary>
    /// Gets the list of validation errors.
    /// </summary>
    public required IReadOnlyList<ImportValidationError> Errors { get; init; }

    /// <summary>
    /// Gets the list of non-blocking validation warnings.
    /// </summary>
    public required IReadOnlyList<ImportValidationWarning> Warnings { get; init; }

    /// <summary>
    /// Gets the resolved import manifest, available when validation succeeds.
    /// </summary>
    public ImportManifest? Manifest { get; init; }
}
