namespace LibrisMaleficarum.Domain.Configuration;

/// <summary>
/// Configuration options for delete operations.
/// </summary>
public class DeleteOperationOptions
{
    /// <summary>
    /// Section name in configuration file.
    /// </summary>
    public const string SectionName = "DeleteOperation";

    /// <summary>
    /// Maximum number of concurrent delete operations per user per world (default: 5).
    /// </summary>
    public int MaxConcurrentPerUserPerWorld { get; set; } = 5;

    /// <summary>
    /// Number of seconds to wait before retrying after rate limit exceeded (default: 30).
    /// </summary>
    public int RetryAfterSeconds { get; set; } = 30;

    /// <summary>
    /// Polling interval in milliseconds for background processor (default: 500).
    /// </summary>
    public int PollingIntervalMs { get; set; } = 500;

    /// <summary>
    /// Maximum batch size for processing operations (default: 50).
    /// </summary>
    public int MaxBatchSize { get; set; } = 50;

    /// <summary>
    /// Rate limit for background processing (entities per second, default: 50).
    /// </summary>
    public int RateLimitPerSecond { get; set; } = 50;

    /// <summary>
    /// Operation TTL in hours (default: 24).
    /// </summary>
    public int OperationTtlHours { get; set; } = 24;
}
