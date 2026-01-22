namespace LibrisMaleficarum.Domain.Exceptions;

/// <summary>
/// Exception thrown when schema version validation fails.
/// </summary>
public class SchemaVersionException : Exception
{
    /// <summary>
    /// Gets the machine-readable error code for the validation failure.
    /// </summary>
    public string ErrorCode { get; }

    /// <summary>
    /// Gets or sets the schema version requested in the API call.
    /// </summary>
    public int? RequestedVersion { get; set; }

    /// <summary>
    /// Gets or sets the current schema version of the entity (for update operations).
    /// </summary>
    public int? CurrentVersion { get; set; }

    /// <summary>
    /// Gets or sets the minimum supported schema version for the entity type.
    /// </summary>
    public int? MinSupportedVersion { get; set; }

    /// <summary>
    /// Gets or sets the maximum supported schema version for the entity type.
    /// </summary>
    public int? MaxSupportedVersion { get; set; }

    /// <summary>
    /// Gets or sets the entity type name.
    /// </summary>
    public string? EntityType { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="SchemaVersionException"/> class.
    /// </summary>
    /// <param name="errorCode">The machine-readable error code.</param>
    /// <param name="message">The human-readable error message.</param>
    public SchemaVersionException(string errorCode, string message)
        : base(message)
    {
        ErrorCode = errorCode;
    }
}
