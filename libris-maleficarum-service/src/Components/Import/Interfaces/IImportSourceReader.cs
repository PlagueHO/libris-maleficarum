namespace LibrisMaleficarum.Import.Interfaces;

/// <summary>
/// Reads and parses an import source (folder or ZIP archive) into raw content.
/// </summary>
public interface IImportSourceReader
{
    /// <summary>
    /// Reads the import source at the specified path and returns parsed content.
    /// </summary>
    /// <param name="sourcePath">The path to the import source (folder or ZIP file).</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The parsed import source content.</returns>
    Task<Models.ImportSourceContent> ReadAsync(string sourcePath, CancellationToken cancellationToken = default);
}
