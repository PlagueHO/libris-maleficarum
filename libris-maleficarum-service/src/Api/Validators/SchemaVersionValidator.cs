namespace LibrisMaleficarum.Api.Validators;

using LibrisMaleficarum.Domain.Configuration;
using LibrisMaleficarum.Domain.Exceptions;
using Microsoft.Extensions.Options;

/// <summary>
/// Validates schema version values in API requests against configured min/max ranges.
/// </summary>
public class SchemaVersionValidator
{
    private readonly EntitySchemaVersionConfig _config;

    /// <summary>
    /// Initializes a new instance of the <see cref="SchemaVersionValidator"/> class.
    /// </summary>
    /// <param name="config">The entity schema version configuration.</param>
    public SchemaVersionValidator(IOptions<EntitySchemaVersionConfig> config)
    {
        _config = config.Value;
    }

    /// <summary>
    /// Validates the schema version for a create request.
    /// </summary>
    /// <param name="entityType">The entity type name.</param>
    /// <param name="requestedVersion">The schema version requested (null defaults to 1).</param>
    /// <exception cref="SchemaVersionException">Thrown when validation fails.</exception>
    public void ValidateCreate(string entityType, int? requestedVersion)
    {
        var version = requestedVersion ?? 1;
        var range = _config.GetVersionRange(entityType);

        if (version < 1)
        {
            throw new SchemaVersionException("SCHEMA_VERSION_INVALID", "Schema version must be a positive integer")
            {
                RequestedVersion = version,
                EntityType = entityType,
                MinSupportedVersion = range.MinVersion,
                MaxSupportedVersion = range.MaxVersion
            };
        }

        if (version < range.MinVersion)
        {
            throw new SchemaVersionException("SCHEMA_VERSION_TOO_LOW",
                $"Schema version {version} is below minimum supported version {range.MinVersion} for entity type '{entityType}'")
            {
                RequestedVersion = version,
                EntityType = entityType,
                MinSupportedVersion = range.MinVersion,
                MaxSupportedVersion = range.MaxVersion
            };
        }

        if (version > range.MaxVersion)
        {
            throw new SchemaVersionException("SCHEMA_VERSION_TOO_HIGH",
                $"Schema version {version} exceeds maximum supported version {range.MaxVersion} for entity type '{entityType}'")
            {
                RequestedVersion = version,
                EntityType = entityType,
                MinSupportedVersion = range.MinVersion,
                MaxSupportedVersion = range.MaxVersion
            };
        }
    }

    /// <summary>
    /// Validates the schema version for an update request.
    /// </summary>
    /// <param name="entityType">The entity type name.</param>
    /// <param name="currentVersion">The entity's current schema version.</param>
    /// <param name="requestedVersion">The schema version requested in the update.</param>
    /// <exception cref="SchemaVersionException">Thrown when validation fails.</exception>
    public void ValidateUpdate(string entityType, int currentVersion, int requestedVersion)
    {
        var range = _config.GetVersionRange(entityType);

        if (requestedVersion < currentVersion)
        {
            throw new SchemaVersionException("SCHEMA_DOWNGRADE_NOT_ALLOWED",
                $"Cannot downgrade entity from schema version {currentVersion} to {requestedVersion}")
            {
                RequestedVersion = requestedVersion,
                CurrentVersion = currentVersion,
                EntityType = entityType,
                MinSupportedVersion = range.MinVersion,
                MaxSupportedVersion = range.MaxVersion
            };
        }

        if (requestedVersion > range.MaxVersion)
        {
            throw new SchemaVersionException("SCHEMA_VERSION_TOO_HIGH",
                $"Schema version {requestedVersion} exceeds maximum supported version {range.MaxVersion} for entity type '{entityType}'")
            {
                RequestedVersion = requestedVersion,
                CurrentVersion = currentVersion,
                EntityType = entityType,
                MinSupportedVersion = range.MinVersion,
                MaxSupportedVersion = range.MaxVersion
            };
        }
    }
}
