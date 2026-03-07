namespace LibrisMaleficarum.Infrastructure.Services;

using Azure;
using Azure.AI.OpenAI;
using LibrisMaleficarum.Domain.Interfaces.Services;
using LibrisMaleficarum.Infrastructure.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenAI.Embeddings;

/// <summary>
/// Generates vector embeddings using Azure AI Services (OpenAI SDK).
/// </summary>
public class EmbeddingService : IEmbeddingService
{
    private readonly EmbeddingClient _embeddingClient;
    private readonly SearchOptions _options;
    private readonly ILogger<EmbeddingService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="EmbeddingService"/> class.
    /// </summary>
    /// <param name="embeddingClient">The OpenAI embedding client.</param>
    /// <param name="options">The search configuration options.</param>
    /// <param name="logger">The logger.</param>
    public EmbeddingService(
        EmbeddingClient embeddingClient,
        IOptions<SearchOptions> options,
        ILogger<EmbeddingService> logger)
    {
        _embeddingClient = embeddingClient ?? throw new ArgumentNullException(nameof(embeddingClient));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public async Task<ReadOnlyMemory<float>> GenerateEmbeddingAsync(
        string text,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            _logger.LogWarning("Empty or null text provided for embedding generation; returning zero vector");
            return new ReadOnlyMemory<float>(new float[_options.EmbeddingDimensions]);
        }

        var embeddingOptions = new EmbeddingGenerationOptions
        {
            Dimensions = _options.EmbeddingDimensions
        };

        var response = await _embeddingClient.GenerateEmbeddingAsync(
            text,
            embeddingOptions,
            cancellationToken);

        return response.Value.ToFloats();
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<ReadOnlyMemory<float>>> GenerateEmbeddingsBatchAsync(
        IEnumerable<string> texts,
        CancellationToken cancellationToken = default)
    {
        var textList = texts?.ToList() ?? throw new ArgumentNullException(nameof(texts));

        if (textList.Count == 0)
        {
            return [];
        }

        var embeddingOptions = new EmbeddingGenerationOptions
        {
            Dimensions = _options.EmbeddingDimensions
        };

        var response = await _embeddingClient.GenerateEmbeddingsAsync(
            textList,
            embeddingOptions,
            cancellationToken);

        return response.Value
            .Select(e => e.ToFloats())
            .ToList()
            .AsReadOnly();
    }
}
