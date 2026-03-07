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
}
