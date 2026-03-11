namespace LibrisMaleficarum.Import.Interfaces;

/// <summary>
/// Orchestrates the full world import workflow: read, validate, and execute.
/// </summary>
public interface IWorldImportService
{
    /// <summary>
    /// Imports a world and its entities from the specified source path.
    /// </summary>
    /// <param name="sourcePath">The path to the import source (folder or ZIP file).</param>
    /// <param name="options">Configuration options controlling import behavior.</param>
    /// <param name="progress">An optional progress reporter for tracking import status.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The result of the import operation.</returns>
    Task<Models.ImportResult> ImportAsync(
        string sourcePath,
        Models.ImportOptions options,
        IProgress<Models.ImportProgress>? progress = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates the import source without executing the import.
    /// </summary>
    /// <param name="sourcePath">The path to the import source (folder or ZIP file).</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The validation result.</returns>
    Task<Models.ImportValidationResult> ValidateAsync(
        string sourcePath,
        CancellationToken cancellationToken = default);
}
