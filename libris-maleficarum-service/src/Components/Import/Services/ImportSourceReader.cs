namespace LibrisMaleficarum.Import.Services;

using System.IO.Compression;
using System.Text.Json;
using LibrisMaleficarum.Import.Interfaces;
using LibrisMaleficarum.Import.Models;
using LibrisMaleficarum.Import.Validation;

/// <summary>
/// Reads and parses an import source folder or ZIP archive into raw content.
/// </summary>
public sealed class ImportSourceReader : IImportSourceReader
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    /// <inheritdoc />
    public async Task<ImportSourceContent> ReadAsync(string sourcePath, CancellationToken cancellationToken = default)
    {
        if (sourcePath.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
        {
            return await ReadZipAsync(sourcePath, cancellationToken).ConfigureAwait(false);
        }

        return await ReadFolderAsync(sourcePath, cancellationToken).ConfigureAwait(false);
    }

    private static async Task<ImportSourceContent> ReadZipAsync(string sourcePath, CancellationToken cancellationToken)
    {
        var parseErrors = new List<ImportValidationError>();
        WorldImportDefinition? world = null;
        var entities = new List<EntityImportDefinition>();

        if (!File.Exists(sourcePath))
        {
            parseErrors.Add(new ImportValidationError
            {
                FilePath = sourcePath,
                Code = ImportValidationErrorCodes.WorldMissing,
                Message = $"ZIP archive does not exist: {sourcePath}"
            });

            return new ImportSourceContent
            {
                World = null,
                Entities = entities,
                ParseErrors = parseErrors,
                SourcePath = sourcePath,
                SourceType = ImportSourceType.ZipArchive
            };
        }

        ZipArchive archive;
        try
        {
            archive = ZipFile.OpenRead(sourcePath);
        }
        catch (InvalidDataException)
        {
            parseErrors.Add(new ImportValidationError
            {
                FilePath = sourcePath,
                Code = ImportValidationErrorCodes.ZipInvalid,
                Message = $"The file is not a valid ZIP archive: {sourcePath}"
            });

            return new ImportSourceContent
            {
                World = null,
                Entities = entities,
                ParseErrors = parseErrors,
                SourcePath = sourcePath,
                SourceType = ImportSourceType.ZipArchive
            };
        }

        using (archive)
        {
            var extractBase = Path.Combine(Path.GetTempPath(), "zip-slip-check");
            var normalizedBase = Path.GetFullPath(extractBase) + Path.DirectorySeparatorChar;

            // Find world.json at root (case-insensitive)
            var worldEntry = archive.Entries
                .FirstOrDefault(e => e.FullName.Equals("world.json", StringComparison.OrdinalIgnoreCase));

            if (worldEntry is null)
            {
                parseErrors.Add(new ImportValidationError
                {
                    FilePath = "world.json",
                    Code = ImportValidationErrorCodes.WorldMissing,
                    Message = "The world.json file is missing from the ZIP archive."
                });
            }
            else
            {
                try
                {
                    using var stream = worldEntry.Open();
                    using var reader = new StreamReader(stream);
                    var json = await reader.ReadToEndAsync(cancellationToken).ConfigureAwait(false);
                    world = JsonSerializer.Deserialize<WorldImportDefinition>(json, JsonOptions);
                }
                catch (JsonException)
                {
                    parseErrors.Add(new ImportValidationError
                    {
                        FilePath = "world.json",
                        Code = ImportValidationErrorCodes.WorldInvalidJson,
                        Message = "The world.json file contains invalid JSON."
                    });
                }
            }

            foreach (var entry in archive.Entries)
            {
                cancellationToken.ThrowIfCancellationRequested();

                // Skip directory entries
                if (string.IsNullOrEmpty(entry.Name))
                {
                    continue;
                }

                // Skip non-JSON entries
                if (!entry.FullName.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                // Skip world.json (already processed)
                if (entry.FullName.Equals("world.json", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                // Zip-slip protection
                var fullPath = Path.GetFullPath(Path.Combine(extractBase, entry.FullName));
                if (!fullPath.StartsWith(normalizedBase, StringComparison.Ordinal))
                {
                    parseErrors.Add(new ImportValidationError
                    {
                        FilePath = entry.FullName,
                        Code = ImportValidationErrorCodes.ZipSlipDetected,
                        Message = $"ZIP entry attempts path traversal: {entry.FullName}"
                    });
                    continue;
                }

                try
                {
                    using var stream = entry.Open();
                    using var reader = new StreamReader(stream);
                    var json = await reader.ReadToEndAsync(cancellationToken).ConfigureAwait(false);
                    var entity = JsonSerializer.Deserialize<EntityImportDefinition>(json, JsonOptions);

                    if (entity is not null)
                    {
                        entity.SourceFilePath = entry.FullName;
                        entities.Add(entity);
                    }
                }
                catch (JsonException)
                {
                    parseErrors.Add(new ImportValidationError
                    {
                        FilePath = entry.FullName,
                        Code = ImportValidationErrorCodes.EntityInvalidJson,
                        Message = $"Entity file contains invalid JSON: {entry.FullName}"
                    });
                }
            }
        }

        return new ImportSourceContent
        {
            World = world,
            Entities = entities,
            ParseErrors = parseErrors,
            SourcePath = sourcePath,
            SourceType = ImportSourceType.ZipArchive
        };
    }

    private static async Task<ImportSourceContent> ReadFolderAsync(string sourcePath, CancellationToken cancellationToken)
    {
        var parseErrors = new List<ImportValidationError>();
        WorldImportDefinition? world = null;
        var entities = new List<EntityImportDefinition>();

        if (!Directory.Exists(sourcePath))
        {
            parseErrors.Add(new ImportValidationError
            {
                FilePath = sourcePath,
                Code = ImportValidationErrorCodes.WorldMissing,
                Message = $"Source folder does not exist: {sourcePath}"
            });

            return new ImportSourceContent
            {
                World = null,
                Entities = entities,
                ParseErrors = parseErrors,
                SourcePath = sourcePath,
                SourceType = ImportSourceType.Folder
            };
        }

        // Look for world.json (case-insensitive)
        var worldFile = Directory.EnumerateFiles(sourcePath, "*.json", SearchOption.TopDirectoryOnly)
            .FirstOrDefault(f => Path.GetFileName(f).Equals("world.json", StringComparison.OrdinalIgnoreCase));

        if (worldFile is null)
        {
            parseErrors.Add(new ImportValidationError
            {
                FilePath = "world.json",
                Code = ImportValidationErrorCodes.WorldMissing,
                Message = "The world.json file is missing from the import source."
            });
        }
        else
        {
            try
            {
                var json = await File.ReadAllTextAsync(worldFile, cancellationToken).ConfigureAwait(false);
                world = JsonSerializer.Deserialize<WorldImportDefinition>(json, JsonOptions);
            }
            catch (JsonException)
            {
                parseErrors.Add(new ImportValidationError
                {
                    FilePath = "world.json",
                    Code = ImportValidationErrorCodes.WorldInvalidJson,
                    Message = "The world.json file contains invalid JSON."
                });
            }
        }

        // Recursively enumerate all *.json files except world.json
        var jsonFiles = Directory.EnumerateFiles(sourcePath, "*.json", SearchOption.AllDirectories)
            .Where(f => !Path.GetFileName(f).Equals("world.json", StringComparison.OrdinalIgnoreCase));

        foreach (var file in jsonFiles)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var relativePath = Path.GetRelativePath(sourcePath, file);

            try
            {
                var json = await File.ReadAllTextAsync(file, cancellationToken).ConfigureAwait(false);
                var entity = JsonSerializer.Deserialize<EntityImportDefinition>(json, JsonOptions);

                if (entity is not null)
                {
                    entity.SourceFilePath = relativePath;
                    entities.Add(entity);
                }
            }
            catch (JsonException)
            {
                parseErrors.Add(new ImportValidationError
                {
                    FilePath = relativePath,
                    Code = ImportValidationErrorCodes.EntityInvalidJson,
                    Message = $"Entity file contains invalid JSON: {relativePath}"
                });
            }
        }

        return new ImportSourceContent
        {
            World = world,
            Entities = entities,
            ParseErrors = parseErrors,
            SourcePath = sourcePath,
            SourceType = ImportSourceType.Folder
        };
    }
}
