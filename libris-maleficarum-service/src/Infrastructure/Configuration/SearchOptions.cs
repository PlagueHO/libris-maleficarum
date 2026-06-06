namespace LibrisMaleficarum.Infrastructure.Configuration;

/// <summary>
/// Configuration options for Azure AI Search integration.
/// </summary>
public class SearchOptions
{
    /// <summary>
    /// Configuration section name in appsettings.json.
    /// </summary>
    public const string SectionName = "Search";

    /// <summary>
    /// Gets or sets the Azure AI Foundry embedding deployment name.
    /// Must match the deployment name defined in AppHost (AddModelDeployment first argument).
    /// </summary>
    public string EmbeddingDeploymentName { get; set; } = "embedding";

    /// <summary>
    /// Gets or sets the Azure AI Services embedding model name.
    /// </summary>
    public string EmbeddingModelName { get; set; } = "text-embedding-3-small";

    /// <summary>
    /// Gets or sets the vector embedding dimensions.
    /// </summary>
    public int EmbeddingDimensions { get; set; } = 1536;

    /// <summary>
    /// Gets or sets the Azure AI Search index name.
    /// </summary>
    public string IndexName { get; set; } = "worldentity-index";

    /// <summary>
    /// Gets or sets the maximum number of documents per index batch.
    /// </summary>
    public int MaxBatchSize { get; set; } = 100;

    /// <summary>
    /// Gets or sets the Change Feed poll interval in milliseconds.
    /// </summary>
    public int ChangeFeedPollIntervalMs { get; set; } = 1000;

    /// <summary>
    /// Gets or sets the type of vector compression applied to the search index.
    /// Defaults to <see cref="VectorCompressionKind.ScalarQuantization"/> (int8, ~4× storage reduction).
    /// </summary>
    /// <remarks>
    /// This is a deploy-time decision. Changing compression on an existing index requires
    /// dropping and recreating the index. The Change Feed worker re-indexes from
    /// <see cref="System.DateTime.MinValue"/> so the index is fully repopulated automatically.
    /// </remarks>
    public VectorCompressionKind VectorCompression { get; set; } = VectorCompressionKind.ScalarQuantization;

    /// <summary>
    /// Gets or sets whether rescoring is enabled for compressed vector queries.
    /// Rescoring re-ranks candidates using full-precision vectors for higher recall quality.
    /// Defaults to <c>true</c>.
    /// </summary>
    public bool EnableRescoring { get; set; } = true;

    /// <summary>
    /// Gets or sets the default oversampling factor used during query-time rescoring.
    /// A higher value retrieves more candidates before re-ranking, improving recall at the cost of latency.
    /// Defaults to <c>10.0</c>.
    /// </summary>
    public double DefaultOversampling { get; set; } = 10.0;
}
