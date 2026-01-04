namespace LibrisMaleficarum.Domain.Exceptions;

/// <summary>
/// Exception thrown when a user attempts to access a world they do not own.
/// </summary>
public class UnauthorizedWorldAccessException : UnauthorizedAccessException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UnauthorizedWorldAccessException"/> class.
    /// </summary>
    public UnauthorizedWorldAccessException()
        : base("You do not have permission to access this world.")
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="UnauthorizedWorldAccessException"/> class with specified identifiers.
    /// </summary>
    /// <param name="worldId">The unique identifier of the world.</param>
    /// <param name="userId">The unique identifier of the user attempting access.</param>
    public UnauthorizedWorldAccessException(Guid worldId, Guid userId)
        : base($"User '{userId}' does not have permission to access world '{worldId}'.")
    {
        WorldId = worldId;
        UserId = userId;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="UnauthorizedWorldAccessException"/> class with a specified error message.
    /// </summary>
    /// <param name="message">The error message.</param>
    public UnauthorizedWorldAccessException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="UnauthorizedWorldAccessException"/> class with a specified error message and inner exception.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The inner exception.</param>
    public UnauthorizedWorldAccessException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    /// <summary>
    /// Gets the unique identifier of the world.
    /// </summary>
    public Guid? WorldId { get; }

    /// <summary>
    /// Gets the unique identifier of the user.
    /// </summary>
    public Guid? UserId { get; }
}
