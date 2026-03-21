namespace LibrisMaleficarum.Import.Validation;

/// <summary>
/// Constants for import validation error codes.
/// </summary>
public static class ImportValidationErrorCodes
{
    // World-level errors

    /// <summary>The world.json file is missing from the import source.</summary>
    public const string WorldMissing = "WORLD_MISSING";

    /// <summary>The world.json file contains invalid JSON.</summary>
    public const string WorldInvalidJson = "WORLD_INVALID_JSON";

    /// <summary>The world definition is missing the required Name property.</summary>
    public const string WorldMissingName = "WORLD_MISSING_NAME";

    // Entity-level errors

    /// <summary>An entity JSON file contains invalid JSON.</summary>
    public const string EntityInvalidJson = "ENTITY_INVALID_JSON";

    /// <summary>An entity definition is missing the required LocalId property.</summary>
    public const string EntityMissingLocalId = "ENTITY_MISSING_LOCAL_ID";

    /// <summary>Multiple entities share the same LocalId value.</summary>
    public const string EntityDuplicateLocalId = "ENTITY_DUPLICATE_LOCAL_ID";

    /// <summary>An entity definition is missing the required Name property.</summary>
    public const string EntityMissingName = "ENTITY_MISSING_NAME";

    /// <summary>An entity definition is missing the required EntityType property.</summary>
    public const string EntityMissingType = "ENTITY_MISSING_TYPE";

    /// <summary>An entity definition specifies an unrecognized EntityType value.</summary>
    public const string EntityInvalidType = "ENTITY_INVALID_TYPE";

    /// <summary>An entity references a ParentLocalId that does not exist in the import source.</summary>
    public const string EntityDanglingParent = "ENTITY_DANGLING_PARENT";

    /// <summary>A cycle was detected in the entity parent-child hierarchy.</summary>
    public const string EntityCycleDetected = "ENTITY_CYCLE_DETECTED";

    /// <summary>An entity name exceeds the maximum allowed length.</summary>
    public const string EntityNameTooLong = "ENTITY_NAME_TOO_LONG";

    /// <summary>An entity description exceeds the maximum allowed length.</summary>
    public const string EntityDescTooLong = "ENTITY_DESC_TOO_LONG";

    /// <summary>An entity has more tags than the maximum allowed count.</summary>
    public const string EntityTooManyTags = "ENTITY_TOO_MANY_TAGS";

    /// <summary>An entity tag exceeds the maximum allowed length.</summary>
    public const string EntityTagTooLong = "ENTITY_TAG_TOO_LONG";

    /// <summary>An entity's custom properties exceed the maximum allowed size.</summary>
    public const string EntityPropsTooLarge = "ENTITY_PROPS_TOO_LARGE";

    // Source-level errors

    /// <summary>The import source path (file or folder) does not exist.</summary>
    public const string SourceNotFound = "SOURCE_NOT_FOUND";

    // Archive-level errors

    /// <summary>The ZIP archive is invalid or corrupted.</summary>
    public const string ZipInvalid = "ZIP_INVALID";

    /// <summary>A ZIP entry attempts path traversal outside the extraction directory.</summary>
    public const string ZipSlipDetected = "ZIP_SLIP_DETECTED";
}
