namespace LibrisMaleficarum.Domain.Exceptions;

/// <summary>
/// Thrown when a user exceeds the rate limit for delete operations.
/// </summary>
public class RateLimitExceededException : Exception
{
    /// <summary>
    /// Gets the number of seconds to wait before retrying.
    /// </summary>
    public int RetryAfterSeconds { get; }

    /// <summary>
    /// Gets the current number of active operations.
    /// </summary>
    public int ActiveOperationCount { get; }

    /// <summary>
    /// Gets the maximum allowed concurrent operations.
    /// </summary>
    public int MaxConcurrentOperations { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="RateLimitExceededException"/> class.
    /// </summary>
    /// <param name="activeCount">The current number of active operations.</param>
    /// <param name="maxConcurrent">The maximum allowed concurrent operations.</param>
    /// <param name="retryAfterSeconds">The number of seconds to wait before retrying (default: 30).</param>
    public RateLimitExceededException(int activeCount, int maxConcurrent, int retryAfterSeconds = 30)
        : base($"Rate limit exceeded: {activeCount}/{maxConcurrent} active delete operations. Retry after {retryAfterSeconds} seconds.")
    {
        ActiveOperationCount = activeCount;
        MaxConcurrentOperations = maxConcurrent;
        RetryAfterSeconds = retryAfterSeconds;
    }
}
