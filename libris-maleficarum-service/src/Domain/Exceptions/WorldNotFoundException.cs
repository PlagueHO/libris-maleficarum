namespace LibrisMaleficarum.Domain.Exceptions;

/// <summary>
/// Exception thrown when a world is not found.
/// </summary>
public class WorldNotFoundException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="WorldNotFoundException"/> class.
    /// </summary>
    public WorldNotFoundException()
        : base("The requested world was not found.")
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="WorldNotFoundException"/> class with a specified world ID.
    /// </summary>
    /// <param name="worldId">The unique identifier of the world that was not found.</param>
    public WorldNotFoundException(Guid worldId)
        : base($"World with ID '{worldId}' was not found.")
    {
        WorldId = worldId;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="WorldNotFoundException"/> class with a specified error message.
    /// </summary>
    /// <param name="message">The error message.</param>
    public WorldNotFoundException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="WorldNotFoundException"/> class with a specified error message and inner exception.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The inner exception.</param>
    public WorldNotFoundException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    /// <summary>
    /// Gets the unique identifier of the world that was not found.
    /// </summary>
    public Guid? WorldId { get; }
}
