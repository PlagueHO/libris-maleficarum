namespace LibrisMaleficarum.Api.Client;

using LibrisMaleficarum.Api.Client.Models;

/// <summary>
/// Client interface for interacting with the Libris Maleficarum API.
/// </summary>
public interface ILibrisApiClient
{
    /// <summary>
    /// Creates a new world.
    /// </summary>
    /// <param name="request">The create world request.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The created world response.</returns>
    /// <exception cref="Exceptions.LibrisApiAuthenticationException">Thrown when the API returns 401 or 403.</exception>
    /// <exception cref="Exceptions.LibrisApiException">Thrown when the API returns a non-success status code.</exception>
    Task<WorldResponse> CreateWorldAsync(CreateWorldRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new entity within a world.
    /// </summary>
    /// <param name="worldId">The identifier of the world to create the entity in.</param>
    /// <param name="request">The create entity request.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The created entity response.</returns>
    /// <exception cref="Exceptions.LibrisApiAuthenticationException">Thrown when the API returns 401 or 403.</exception>
    /// <exception cref="Exceptions.LibrisApiException">Thrown when the API returns a non-success status code.</exception>
    Task<EntityResponse> CreateEntityAsync(Guid worldId, CreateEntityRequest request, CancellationToken cancellationToken = default);
}
