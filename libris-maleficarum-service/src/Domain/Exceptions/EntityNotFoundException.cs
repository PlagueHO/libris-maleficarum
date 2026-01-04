namespace LibrisMaleficarum.Domain.Exceptions;

/// <summary>
/// Exception thrown when a requested entity is not found.
/// </summary>
public class EntityNotFoundException : Exception
{
    /// <summary>
    /// Gets the identifier of the entity that was not found.
    /// </summary>
    public Guid EntityId { get; }

    /// <summary>
    /// Gets the identifier of the world where the entity was searched.
    /// </summary>
    public Guid WorldId { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="EntityNotFoundException"/> class.
    /// </summary>
    /// <param name="worldId">The world identifier.</param>
    /// <param name="entityId">The entity identifier.</param>
    public EntityNotFoundException(Guid worldId, Guid entityId)
        : base($"Entity with ID '{entityId}' not found in world '{worldId}'.")
    {
        WorldId = worldId;
        EntityId = entityId;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="EntityNotFoundException"/> class with a custom message.
    /// </summary>
    /// <param name="worldId">The world identifier.</param>
    /// <param name="entityId">The entity identifier.</param>
    /// <param name="message">The exception message.</param>
    public EntityNotFoundException(Guid worldId, Guid entityId, string message)
        : base(message)
    {
        WorldId = worldId;
        EntityId = entityId;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="EntityNotFoundException"/> class with a custom message and inner exception.
    /// </summary>
    /// <param name="worldId">The world identifier.</param>
    /// <param name="entityId">The entity identifier.</param>
    /// <param name="message">The exception message.</param>
    /// <param name="innerException">The inner exception.</param>
    public EntityNotFoundException(Guid worldId, Guid entityId, string message, Exception innerException)
        : base(message, innerException)
    {
        WorldId = worldId;
        EntityId = entityId;
    }
}
