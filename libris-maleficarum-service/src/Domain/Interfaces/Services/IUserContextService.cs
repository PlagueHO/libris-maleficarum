namespace LibrisMaleficarum.Domain.Interfaces.Services;

/// <summary>
/// Provides access to the current user context for authorization and ownership validation.
/// </summary>
public interface IUserContextService
{
    /// <summary>
    /// Gets the unique identifier of the currently authenticated user.
    /// </summary>
    /// <returns>
    /// A <see cref="Task"/> representing the asynchronous operation, containing the user's unique identifier.
    /// </returns>
    Task<Guid> GetCurrentUserIdAsync();
}
