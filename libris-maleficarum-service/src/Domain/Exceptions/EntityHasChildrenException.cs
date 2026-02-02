namespace LibrisMaleficarum.Domain.Exceptions;

/// <summary>
/// Exception thrown when attempting to delete an entity with children without cascade.
/// </summary>
public class EntityHasChildrenException : Exception
{
    /// <summary>
    /// Gets the identifier of the entity that has children.
    /// </summary>
    public Guid EntityId { get; }

    /// <summary>
    /// Gets the name of the entity that has children.
    /// </summary>
    public string EntityName { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="EntityHasChildrenException"/> class.
    /// </summary>
    /// <param name="entityId">The entity identifier.</param>
    /// <param name="entityName">The entity name.</param>
    public EntityHasChildrenException(Guid entityId, string entityName)
        : base($"Cannot delete entity '{entityName}' (ID: '{entityId}') without cascade - it has child entities. Use cascade=true to delete all descendants.")
    {
        EntityId = entityId;
        EntityName = entityName;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="EntityHasChildrenException"/> class with a custom message.
    /// </summary>
    /// <param name="entityId">The entity identifier.</param>
    /// <param name="entityName">The entity name.</param>
    /// <param name="message">The exception message.</param>
    public EntityHasChildrenException(Guid entityId, string entityName, string message)
        : base(message)
    {
        EntityId = entityId;
        EntityName = entityName;
    }
}
