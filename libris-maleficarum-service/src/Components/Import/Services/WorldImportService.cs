namespace LibrisMaleficarum.Import.Services;

using System.Diagnostics;
using LibrisMaleficarum.Api.Client;
using LibrisMaleficarum.Api.Client.Models;
using LibrisMaleficarum.Import.Interfaces;
using LibrisMaleficarum.Import.Models;

/// <summary>
/// Orchestrates the full world import workflow: read, validate, and execute.
/// </summary>
public sealed class WorldImportService(
    IImportSourceReader reader,
    IImportValidator validator,
    ILibrisApiClient apiClient) : IWorldImportService
{
    private readonly IImportSourceReader _reader = reader;
    private readonly IImportValidator _validator = validator;
    private readonly ILibrisApiClient _apiClient = apiClient;

    /// <inheritdoc />
    public async Task<ImportValidationResult> ValidateAsync(
        string sourcePath,
        CancellationToken cancellationToken = default)
    {
        var content = await _reader.ReadAsync(sourcePath, cancellationToken).ConfigureAwait(false);
        return _validator.Validate(content);
    }

    /// <inheritdoc />
    public async Task<ImportResult> ImportAsync(
        string sourcePath,
        ImportOptions options,
        IProgress<ImportProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();

        // Reading phase
        ReportProgress(progress, ImportPhase.Reading, 0, 0, 0, "Reading source...");
        var content = await _reader.ReadAsync(sourcePath, cancellationToken).ConfigureAwait(false);

        // Validating phase
        ReportProgress(progress, ImportPhase.Validating, 0, 0, 0, "Validating...");
        var validationResult = _validator.Validate(content);

        if (!validationResult.IsValid)
        {
            stopwatch.Stop();
            var validationErrors = validationResult.Errors
                .Select(e => new EntityImportError
                {
                    LocalId = "",
                    EntityName = "",
                    ErrorMessage = $"[{e.Code}] {e.Message}",
                    FilePath = e.FilePath,
                    SkippedDescendantLocalIds = []
                })
                .ToList();

            return new ImportResult
            {
                Success = false,
                WorldId = Guid.Empty,
                TotalEntitiesCreated = 0,
                TotalEntitiesFailed = validationErrors.Count,
                TotalEntitiesSkipped = 0,
                CreatedByType = new Dictionary<string, int>(),
                Errors = validationErrors,
                Duration = stopwatch.Elapsed
            };
        }

        if (options.ValidateOnly)
        {
            stopwatch.Stop();
            return new ImportResult
            {
                Success = true,
                WorldId = Guid.Empty,
                TotalEntitiesCreated = 0,
                TotalEntitiesFailed = 0,
                TotalEntitiesSkipped = 0,
                CreatedByType = new Dictionary<string, int>(),
                Errors = [],
                Duration = stopwatch.Elapsed
            };
        }

        var manifest = validationResult.Manifest!;

        // Creating world phase
        ReportProgress(progress, ImportPhase.CreatingWorld, manifest.TotalEntityCount, 0, 0, manifest.World.Name);
        var worldResponse = await _apiClient.CreateWorldAsync(
            new CreateWorldRequest { Name = manifest.World.Name, Description = manifest.World.Description },
            cancellationToken).ConfigureAwait(false);
        var worldId = worldResponse.Id;

        // Creating entities phase
        ReportProgress(progress, ImportPhase.CreatingEntities, manifest.TotalEntityCount, 0, 0, "Creating entities...");

        var createdCount = 0;
        var failedCount = 0;
        var skippedCount = 0;
        var createdByType = new Dictionary<string, int>();
        var errors = new List<EntityImportError>();
        var skippedLocalIds = new HashSet<string>(StringComparer.Ordinal);

        for (var depth = 0; depth <= manifest.MaxDepth; depth++)
        {
            if (!manifest.EntitiesByDepth.TryGetValue(depth, out var entitiesAtDepth))
            {
                continue;
            }

            var toProcess = new List<ResolvedEntity>();
            foreach (var entity in entitiesAtDepth)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (skippedLocalIds.Contains(entity.Definition.LocalId))
                {
                    // Already pre-counted as a skip from a parent's failure
                    continue;
                }

                if (entity.Definition.ParentLocalId is not null &&
                    skippedLocalIds.Contains(entity.Definition.ParentLocalId))
                {
                    var descendantIds = new List<string>();
                    CollectDescendantLocalIds(entity, descendantIds);
                    skippedCount += AddSkippedLocalIds(
                        skippedLocalIds,
                        [entity.Definition.LocalId, .. descendantIds]);
                    continue;
                }

                toProcess.Add(entity);
            }

            var concurrency = Math.Max(1, options.MaxConcurrency);
            using var semaphore = new SemaphoreSlim(concurrency);
            var tasks = toProcess.Select(async entity =>
            {
                await semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
                try
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var request = new CreateEntityRequest
                    {
                        Name = entity.Definition.Name,
                        Description = entity.Definition.Description,
                        EntityType = entity.Definition.EntityType,
                        ParentId = entity.ResolvedParentId,
                        Tags = entity.Definition.Tags,
                        Attributes = entity.Definition.Properties,
                        SchemaVersion = 1
                    };

                    await _apiClient.CreateEntityAsync(worldId, request, cancellationToken).ConfigureAwait(false);

                    lock (createdByType)
                    {
                        createdCount++;
                        if (!createdByType.TryGetValue(entity.Definition.EntityType, out var count))
                        {
                            count = 0;
                        }
                        createdByType[entity.Definition.EntityType] = count + 1;
                    }
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    var descendantIds = new List<string>();
                    CollectDescendantLocalIds(entity, descendantIds);

                    lock (skippedLocalIds)
                    {
                        failedCount++;
                        skippedLocalIds.Add(entity.Definition.LocalId);
                        skippedCount += AddSkippedLocalIds(skippedLocalIds, descendantIds);

                        errors.Add(new EntityImportError
                        {
                            LocalId = entity.Definition.LocalId,
                            EntityName = entity.Definition.Name,
                            ErrorMessage = ex.Message,
                            FilePath = entity.Definition.SourceFilePath,
                            SkippedDescendantLocalIds = descendantIds
                        });
                    }
                }
                finally
                {
                    semaphore.Release();
                }
            });

            await Task.WhenAll(tasks).ConfigureAwait(false);

            ReportProgress(progress, ImportPhase.CreatingEntities, manifest.TotalEntityCount, createdCount + failedCount + skippedCount, depth, "Processing...");
        }

        stopwatch.Stop();

        // Complete phase
        ReportProgress(progress, ImportPhase.Complete, manifest.TotalEntityCount, manifest.TotalEntityCount, manifest.MaxDepth, "Complete");

        return new ImportResult
        {
            Success = failedCount == 0,
            WorldId = worldId,
            TotalEntitiesCreated = createdCount,
            TotalEntitiesFailed = failedCount,
            TotalEntitiesSkipped = skippedCount,
            CreatedByType = createdByType,
            Errors = errors,
            Duration = stopwatch.Elapsed
        };
    }

    private static void ReportProgress(
        IProgress<ImportProgress>? progress,
        ImportPhase phase,
        int total,
        int completed,
        int depth,
        string entityName)
    {
        progress?.Report(new ImportProgress
        {
            TotalEntities = total,
            CompletedEntities = completed,
            CurrentDepth = depth,
            CurrentEntityName = entityName,
            Phase = phase
        });
    }

    private static int AddSkippedLocalIds(
        ISet<string> skippedLocalIds,
        IEnumerable<string> localIds)
    {
        var addedCount = 0;

        foreach (var localId in localIds)
        {
            if (skippedLocalIds.Add(localId))
            {
                addedCount++;
            }
        }

        return addedCount;
    }

    private static void CollectDescendantLocalIds(ResolvedEntity entity, ICollection<string> ids)
    {
        foreach (var child in entity.Children)
        {
            ids.Add(child.Definition.LocalId);
            CollectDescendantLocalIds(child, ids);
        }
    }
}
