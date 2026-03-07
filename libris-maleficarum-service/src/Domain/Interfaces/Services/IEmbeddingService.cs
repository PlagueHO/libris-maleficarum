namespace LibrisMaleficarum.Domain.Interfaces.Services;

/// <summary>
/// Abstraction for vector embedding generation via Azure AI Services.
/// </summary>
public interface IEmbeddingService
{
    /// <summary>
    /// Generates a vector embedding for the given text.
    /// </summary>
    /// <param name="text">The text to generate an embedding for.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The vector embedding as a read-only memory of floats.</returns>
    Task<ReadOnlyMemory<float>> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates vector embeddings for a batch of texts.
    /// </summary>
    /// <param name="texts">The texts to generate embeddings for.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A read-only list of vector embeddings.</returns>
    Task<IReadOnlyList<ReadOnlyMemory<float>>> GenerateEmbeddingsBatchAsync(IEnumerable<string> texts, CancellationToken cancellationToken = default);
}
