using LibrisMaleficarum.Import.Models;

namespace LibrisMaleficarum.Cli.Output;

/// <summary>
/// Formats and writes import/validation output to the console,
/// matching the contract output format from import-api-contract.md.
/// </summary>
public static class ConsoleReporter
{
    /// <summary>
    /// Reports a fully successful import result.
    /// </summary>
    public static void ReportImportSuccess(ImportResult result)
    {
        Console.WriteLine("Import completed successfully.");
        Console.WriteLine();
        Console.WriteLine($"  World:     {result.WorldId}");
        Console.WriteLine($"  Duration:  {result.Duration.TotalSeconds:F1}s");
        Console.WriteLine();
        Console.WriteLine($"  Entities created: {result.TotalEntitiesCreated}");
        WriteCreatedByType(result.CreatedByType);
    }

    /// <summary>
    /// Reports an import result with partial failures.
    /// </summary>
    public static void ReportImportPartialFailure(ImportResult result)
    {
        Console.WriteLine("Import completed with errors.");
        Console.WriteLine();
        Console.WriteLine($"  World:     {result.WorldId}");
        Console.WriteLine($"  Duration:  {result.Duration.TotalSeconds:F1}s");
        Console.WriteLine();
        Console.WriteLine($"  Entities created: {result.TotalEntitiesCreated}");
        Console.WriteLine($"  Entities failed:  {result.TotalEntitiesFailed}");

        if (result.TotalEntitiesSkipped > 0)
        {
            Console.WriteLine($"  Entities skipped: {result.TotalEntitiesSkipped} (descendants of failed entities)");
        }

        WriteErrors(result.Errors);
    }

    /// <summary>
    /// Reports a total import failure where no entities were created.
    /// </summary>
    public static void ReportImportTotalFailure(ImportResult result)
    {
        Console.WriteLine("Import failed.");
        Console.WriteLine();
        Console.WriteLine($"  World:     {result.WorldId}");
        Console.WriteLine($"  Duration:  {result.Duration.TotalSeconds:F1}s");
        Console.WriteLine();
        Console.WriteLine($"  Entities failed:  {result.TotalEntitiesFailed}");

        if (result.TotalEntitiesSkipped > 0)
        {
            Console.WriteLine($"  Entities skipped: {result.TotalEntitiesSkipped} (descendants of failed entities)");
        }

        WriteErrors(result.Errors);
    }

    /// <summary>
    /// Reports a successful validation result with manifest summary.
    /// </summary>
    public static void ReportValidationSuccess(ImportValidationResult result)
    {
        Console.WriteLine("Validation passed.");

        if (result.Manifest is not null)
        {
            var manifest = result.Manifest;
            Console.WriteLine();
            Console.WriteLine($"  World:     {manifest.World.Name}");
            Console.WriteLine($"  Entities:  {manifest.TotalEntityCount} across {manifest.CountsByType.Count} types");
            WriteCountsByType(manifest.CountsByType);
            Console.WriteLine($"  Max depth: {manifest.MaxDepth}");
        }
    }

    /// <summary>
    /// Reports a validation result with errors.
    /// </summary>
    public static void ReportValidationFailure(ImportValidationResult result)
    {
        Console.WriteLine($"Validation failed with {result.Errors.Count} error{(result.Errors.Count == 1 ? string.Empty : "s")}.");
        Console.WriteLine();
        Console.WriteLine("  Errors:");

        foreach (var error in result.Errors)
        {
            Console.WriteLine($"    [{error.FilePath}] {error.Code}: {error.Message}");
        }
    }

    /// <summary>
    /// Reports progress of an ongoing import operation.
    /// </summary>
    public static void ReportProgress(ImportProgress progress)
    {
        Console.WriteLine($"  [{progress.Phase}] {progress.CompletedEntities}/{progress.TotalEntities} - {progress.CurrentEntityName}");
    }

    private static void WriteCreatedByType(IReadOnlyDictionary<string, int> createdByType)
    {
        foreach (var (type, count) in createdByType.OrderBy(kvp => kvp.Key))
        {
            Console.WriteLine($"    {type,-12}{count}");
        }
    }

    private static void WriteCountsByType(IReadOnlyDictionary<string, int> countsByType)
    {
        foreach (var (type, count) in countsByType.OrderBy(kvp => kvp.Key))
        {
            Console.WriteLine($"    {type,-12}{count}");
        }
    }

    private static void WriteErrors(IReadOnlyList<EntityImportError> errors)
    {
        if (errors.Count == 0)
        {
            return;
        }

        Console.WriteLine();
        Console.WriteLine("  Errors:");

        foreach (var error in errors)
        {
            var fileRef = error.FilePath is not null ? $"[{error.FilePath}] " : string.Empty;
            Console.WriteLine($"    {fileRef}{error.EntityName}: {error.ErrorMessage}");

            if (error.SkippedDescendantLocalIds.Count > 0)
            {
                Console.WriteLine($"      Skipped descendants: {string.Join(", ", error.SkippedDescendantLocalIds)}");
            }
        }
    }
}
