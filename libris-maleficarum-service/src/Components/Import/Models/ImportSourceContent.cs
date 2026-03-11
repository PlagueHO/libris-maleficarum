namespace LibrisMaleficarum.Import.Models;

/// <summary>
/// Raw parsed content from an import source before validation.
/// </summary>
public sealed class ImportSourceContent
{
    /// <summary>
    /// Gets the parsed world definition, or <see langword="null"/> if the world file was missing or invalid.
    /// </summary>
    public required WorldImportDefinition? World { get; init; }

    /// <summary>
    /// Gets the list of parsed entity definitions.
    /// </summary>
    public required IReadOnlyList<EntityImportDefinition> Entities { get; init; }

    /// <summary>
    /// Gets the list of errors encountered during parsing.
    /// </summary>
    public required IReadOnlyList<ImportValidationError> ParseErrors { get; init; }

    /// <summary>
    /// Gets the path to the import source (folder or archive).
    /// </summary>
    public required string SourcePath { get; init; }

    /// <summary>
    /// Gets the type of import source.
    /// </summary>
    public required ImportSourceType SourceType { get; init; }
}

/// <summary>
/// Specifies the type of import source.
/// </summary>
public enum ImportSourceType
{
    /// <summary>
    /// A folder on disk containing world and entity JSON files.
    /// </summary>
    Folder,

    /// <summary>
    /// A ZIP archive containing world and entity JSON files.
    /// </summary>
    ZipArchive
}
