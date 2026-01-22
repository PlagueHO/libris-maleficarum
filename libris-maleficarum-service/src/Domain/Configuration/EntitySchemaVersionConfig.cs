namespace LibrisMaleficarum.Domain.Configuration;

/// <summary>
/// Configuration for supported schema version ranges per entity type.
/// Used to validate incoming schema versions in API requests.
/// </summary>
public class EntitySchemaVersionConfig
{
    /// <summary>
    /// Gets or sets the schema version ranges per entity type.
    /// </summary>
    public Dictionary<string, SchemaVersionRange> EntityTypes { get; set; } = [];

    /// <summary>
    /// Gets the schema version range for a given entity type.
    /// </summary>
    /// <param name="entityType">The entity type name.</param>
    /// <returns>The version range, or { Min: 1, Max: 1 } if not configured.</returns>
    public SchemaVersionRange GetVersionRange(string entityType)
    {
        return EntityTypes.TryGetValue(entityType, out var range)
            ? range
            : new SchemaVersionRange { MinVersion = 1, MaxVersion = 1 };
    }
}

/// <summary>
/// Represents the minimum and maximum supported schema versions for an entity type.
/// </summary>
public class SchemaVersionRange
{
    /// <summary>
    /// Gets or sets the minimum supported schema version (inclusive).
    /// </summary>
    public int MinVersion { get; set; } = 1;

    /// <summary>
    /// Gets or sets the maximum supported schema version (inclusive).
    /// </summary>
    public int MaxVersion { get; set; } = 1;
}
