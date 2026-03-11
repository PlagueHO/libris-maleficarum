namespace LibrisMaleficarum.Import.Services;

using System.Text.Json;
using LibrisMaleficarum.Import.Interfaces;
using LibrisMaleficarum.Import.Models;
using LibrisMaleficarum.Import.Validation;

/// <summary>
/// Validates parsed import source content and produces a resolved manifest.
/// </summary>
public sealed class ImportValidator : IImportValidator
{
    /// <inheritdoc />
    public ImportValidationResult Validate(ImportSourceContent content)
    {
        var errors = new List<ImportValidationError>(content.ParseErrors);

        if (content.World is null)
        {
            errors.Add(new ImportValidationError
            {
                FilePath = "world.json",
                Code = ImportValidationErrorCodes.WorldMissing,
                Message = "The world definition is missing."
            });

            return new ImportValidationResult
            {
                Errors = errors,
                Warnings = [],
                Manifest = null
            };
        }

        // Validate world
        if (string.IsNullOrEmpty(content.World.Name))
        {
            errors.Add(new ImportValidationError
            {
                FilePath = "world.json",
                Code = ImportValidationErrorCodes.WorldMissingName,
                Message = "The world definition is missing the required Name property."
            });
        }

        // Validate individual entities
        var seenLocalIds = new HashSet<string>(StringComparer.Ordinal);
        var localIdSet = new HashSet<string>(content.Entities
            .Where(e => !string.IsNullOrEmpty(e.LocalId))
            .Select(e => e.LocalId), StringComparer.Ordinal);

        foreach (var entity in content.Entities)
        {
            var filePath = entity.SourceFilePath ?? "unknown";

            // LocalId
            if (string.IsNullOrEmpty(entity.LocalId))
            {
                errors.Add(new ImportValidationError
                {
                    FilePath = filePath,
                    Code = ImportValidationErrorCodes.EntityMissingLocalId,
                    Message = "Entity is missing the required LocalId property."
                });
            }
            else if (!seenLocalIds.Add(entity.LocalId))
            {
                errors.Add(new ImportValidationError
                {
                    FilePath = filePath,
                    Code = ImportValidationErrorCodes.EntityDuplicateLocalId,
                    Message = $"Duplicate LocalId: '{entity.LocalId}'."
                });
            }

            // Name
            if (string.IsNullOrEmpty(entity.Name))
            {
                errors.Add(new ImportValidationError
                {
                    FilePath = filePath,
                    Code = ImportValidationErrorCodes.EntityMissingName,
                    Message = "Entity is missing the required Name property."
                });
            }
            else if (entity.Name.Length > 200)
            {
                errors.Add(new ImportValidationError
                {
                    FilePath = filePath,
                    Code = ImportValidationErrorCodes.EntityNameTooLong,
                    Message = $"Entity name exceeds 200 characters ({entity.Name.Length})."
                });
            }

            // EntityType
            if (string.IsNullOrEmpty(entity.EntityType))
            {
                errors.Add(new ImportValidationError
                {
                    FilePath = filePath,
                    Code = ImportValidationErrorCodes.EntityMissingType,
                    Message = "Entity is missing the required EntityType property."
                });
            }
            else if (!Enum.TryParse<Domain.ValueObjects.EntityType>(entity.EntityType, ignoreCase: true, out _))
            {
                errors.Add(new ImportValidationError
                {
                    FilePath = filePath,
                    Code = ImportValidationErrorCodes.EntityInvalidType,
                    Message = $"Entity has an invalid EntityType: '{entity.EntityType}'."
                });
            }

            // Description length
            if (entity.Description is not null && entity.Description.Length > 5000)
            {
                errors.Add(new ImportValidationError
                {
                    FilePath = filePath,
                    Code = ImportValidationErrorCodes.EntityDescTooLong,
                    Message = $"Entity description exceeds 5000 characters ({entity.Description.Length})."
                });
            }

            // Tags
            if (entity.Tags is not null)
            {
                if (entity.Tags.Count > 20)
                {
                    errors.Add(new ImportValidationError
                    {
                        FilePath = filePath,
                        Code = ImportValidationErrorCodes.EntityTooManyTags,
                        Message = $"Entity has {entity.Tags.Count} tags, exceeding the maximum of 20."
                    });
                }

                foreach (var tag in entity.Tags)
                {
                    if (tag.Length > 50)
                    {
                        errors.Add(new ImportValidationError
                        {
                            FilePath = filePath,
                            Code = ImportValidationErrorCodes.EntityTagTooLong,
                            Message = $"Entity tag exceeds 50 characters ({tag.Length})."
                        });
                    }
                }
            }

            // Properties size
            if (entity.Properties is not null)
            {
                var serializedSize = JsonSerializer.SerializeToUtf8Bytes(entity.Properties).Length;
                if (serializedSize > 102_400)
                {
                    errors.Add(new ImportValidationError
                    {
                        FilePath = filePath,
                        Code = ImportValidationErrorCodes.EntityPropsTooLarge,
                        Message = $"Entity properties exceed 100KB ({serializedSize} bytes)."
                    });
                }
            }
        }

        // Validate relationships: dangling parents
        foreach (var entity in content.Entities)
        {
            if (!string.IsNullOrEmpty(entity.ParentLocalId) && !localIdSet.Contains(entity.ParentLocalId))
            {
                errors.Add(new ImportValidationError
                {
                    FilePath = entity.SourceFilePath ?? "unknown",
                    Code = ImportValidationErrorCodes.EntityDanglingParent,
                    Message = $"Entity '{entity.LocalId}' references non-existent parent '{entity.ParentLocalId}'."
                });
            }
        }

        // Cycle detection via DFS
        if (DetectCycles(content.Entities))
        {
            errors.Add(new ImportValidationError
            {
                FilePath = "entities",
                Code = ImportValidationErrorCodes.EntityCycleDetected,
                Message = "A cycle was detected in the entity parent-child hierarchy."
            });
        }

        if (errors.Count > 0)
        {
            return new ImportValidationResult
            {
                Errors = errors,
                Warnings = [],
                Manifest = null
            };
        }

        // Build manifest
        var manifest = BuildManifest(content);

        return new ImportValidationResult
        {
            Errors = errors,
            Warnings = [],
            Manifest = manifest
        };
    }

    private static bool DetectCycles(IReadOnlyList<EntityImportDefinition> entities)
    {
        var parentMap = new Dictionary<string, string?>(StringComparer.Ordinal);
        foreach (var entity in entities)
        {
            if (!string.IsNullOrEmpty(entity.LocalId))
            {
                parentMap[entity.LocalId] = entity.ParentLocalId;
            }
        }

        var visited = new HashSet<string>(StringComparer.Ordinal);
        var inStack = new HashSet<string>(StringComparer.Ordinal);

        foreach (var localId in parentMap.Keys)
        {
            if (HasCycleDfs(localId, parentMap, visited, inStack))
            {
                return true;
            }
        }

        return false;
    }

    private static bool HasCycleDfs(
        string localId,
        Dictionary<string, string?> parentMap,
        HashSet<string> visited,
        HashSet<string> inStack)
    {
        if (inStack.Contains(localId))
        {
            return true;
        }

        if (visited.Contains(localId))
        {
            return false;
        }

        visited.Add(localId);
        inStack.Add(localId);

        if (parentMap.TryGetValue(localId, out var parentLocalId) &&
            !string.IsNullOrEmpty(parentLocalId) &&
            parentMap.ContainsKey(parentLocalId))
        {
            if (HasCycleDfs(parentLocalId, parentMap, visited, inStack))
            {
                return true;
            }
        }

        inStack.Remove(localId);
        return false;
    }

    private static ImportManifest BuildManifest(ImportSourceContent content)
    {
        var idMap = new Dictionary<string, Guid>(StringComparer.Ordinal);
        foreach (var entity in content.Entities)
        {
            idMap[entity.LocalId] = Guid.NewGuid();
        }

        var resolvedMap = new Dictionary<string, ResolvedEntity>(StringComparer.Ordinal);
        var resolvedEntities = new List<ResolvedEntity>();

        foreach (var entity in content.Entities)
        {
            var path = new List<Guid>();
            var current = entity;
            var ancestors = new List<Guid>();

            while (current?.ParentLocalId is not null && idMap.ContainsKey(current.ParentLocalId))
            {
                ancestors.Add(idMap[current.ParentLocalId]);
                current = content.Entities.FirstOrDefault(x => x.LocalId == current.ParentLocalId);
            }

            ancestors.Reverse();
            path.AddRange(ancestors);

            var depth = path.Count;
            Guid? resolvedParentId = entity.ParentLocalId is not null && idMap.TryGetValue(entity.ParentLocalId, out var pid)
                ? pid
                : null;

            var resolved = new ResolvedEntity
            {
                Definition = entity,
                AssignedId = idMap[entity.LocalId],
                ResolvedParentId = resolvedParentId,
                Path = path,
                Depth = depth,
                Children = []
            };

            resolvedEntities.Add(resolved);
            resolvedMap[entity.LocalId] = resolved;
        }

        // Wire up children
        foreach (var resolved in resolvedEntities)
        {
            if (resolved.Definition.ParentLocalId is not null &&
                resolvedMap.TryGetValue(resolved.Definition.ParentLocalId, out var parent))
            {
                parent.Children.Add(resolved);
            }
        }

        var entitiesByDepth = resolvedEntities
            .GroupBy(e => e.Depth)
            .ToDictionary(
                g => g.Key,
                g => (IReadOnlyList<ResolvedEntity>)g.ToList());

        var maxDepth = resolvedEntities.Count > 0
            ? resolvedEntities.Max(e => e.Depth)
            : 0;

        var countsByType = resolvedEntities
            .GroupBy(e => e.Definition.EntityType)
            .ToDictionary(g => g.Key, g => g.Count());

        return new ImportManifest
        {
            World = content.World!,
            Entities = resolvedEntities,
            EntitiesByDepth = entitiesByDepth,
            MaxDepth = maxDepth,
            TotalEntityCount = resolvedEntities.Count,
            CountsByType = countsByType
        };
    }
}
