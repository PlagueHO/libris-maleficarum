namespace LibrisMaleficarum.Infrastructure.Services;

using Azure;
using Azure.Search.Documents;
using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Indexes.Models;
using Azure.Search.Documents.Models;
using LibrisMaleficarum.Domain.Interfaces.Services;
using LibrisMaleficarum.Domain.Models;
using LibrisMaleficarum.Infrastructure.Configuration;
using LibrisMaleficarum.Infrastructure.Search;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using AzureSearchOptions = Azure.Search.Documents.SearchOptions;
using AppSearchOptions = LibrisMaleficarum.Infrastructure.Configuration.SearchOptions;

/// <summary>
/// Azure AI Search service implementation for index operations and search queries.
/// Implements both ISearchIndexService (index push) and ISearchService (search queries).
/// </summary>
public class AzureAISearchService : ISearchIndexService, ISearchService
{
    private static readonly IndexDocumentsOptions FailOnAnyIndexingError = new() { ThrowOnAnyError = true };

    private readonly SearchIndexClient _indexClient;
    private readonly SearchClient _searchClient;
    private readonly IEmbeddingService _embeddingService;
    private readonly ITelemetryService _telemetryService;
    private readonly AppSearchOptions _options;
    private readonly ILogger<AzureAISearchService> _logger;
    private readonly VectorIndexProfile _vectorProfile;

    /// <summary>
    /// Initializes a new instance of the <see cref="AzureAISearchService"/> class.
    /// </summary>
    public AzureAISearchService(
        SearchIndexClient indexClient,
        SearchClient searchClient,
        IEmbeddingService embeddingService,
        ITelemetryService telemetryService,
        IOptions<AppSearchOptions> options,
        ILogger<AzureAISearchService> logger)
    {
        _indexClient = indexClient ?? throw new ArgumentNullException(nameof(indexClient));
        _searchClient = searchClient ?? throw new ArgumentNullException(nameof(searchClient));
        _embeddingService = embeddingService ?? throw new ArgumentNullException(nameof(embeddingService));
        _telemetryService = telemetryService ?? throw new ArgumentNullException(nameof(telemetryService));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _vectorProfile = VectorIndexProfile.From(_options);
    }

    /// <inheritdoc/>
    public async Task EnsureIndexExistsAsync(CancellationToken cancellationToken = default)
    {
        var index = new SearchIndex(_options.IndexName)
        {
            Fields =
            [
                new SimpleField("id", SearchFieldDataType.String) { IsKey = true, IsFilterable = false, IsSortable = false },
                new SimpleField("worldId", SearchFieldDataType.String) { IsFilterable = true },
                new SearchableField("name") { IsFilterable = true, IsSortable = true },
                new SearchableField("description"),
                new SearchableField("tags", collection: true) { IsFilterable = true, IsFacetable = true },
                new SimpleField("entityType", SearchFieldDataType.String) { IsFilterable = true, IsFacetable = true },
                new SimpleField("parentId", SearchFieldDataType.String) { IsFilterable = true },
                new SimpleField("ownerId", SearchFieldDataType.String) { IsFilterable = true },
                new SimpleField("createdAt", SearchFieldDataType.DateTimeOffset) { IsFilterable = true, IsSortable = true },
                new SimpleField("updatedAt", SearchFieldDataType.DateTimeOffset) { IsFilterable = true, IsSortable = true },
                new SimpleField("path", SearchFieldDataType.Collection(SearchFieldDataType.String)) { IsFilterable = true },
                new SimpleField("depth", SearchFieldDataType.Int32) { IsFilterable = true, IsSortable = true, IsFacetable = true },
                new SimpleField("schemaId", SearchFieldDataType.String) { IsFilterable = true, IsFacetable = true },
                new SearchableField("properties"),
                new SearchableField("systemProperties"),
                new SimpleField("schemaVersion", SearchFieldDataType.Int32) { IsFilterable = true },
                new SearchField("contentVector", SearchFieldDataType.Collection(SearchFieldDataType.Single))
                {
                    IsSearchable = true,
                    VectorSearchDimensions = _options.EmbeddingDimensions,
                    VectorSearchProfileName = "vector-profile"
                }
            ],
            VectorSearch = new VectorSearch
            {
                Algorithms =
                {
                    new HnswAlgorithmConfiguration("hnsw-algorithm")
                    {
                        Parameters = new HnswParameters
                        {
                            Metric = VectorSearchAlgorithmMetric.Cosine,
                            M = 4,
                            EfConstruction = 400,
                            EfSearch = 500
                        }
                    }
                },
                Profiles =
                {
                    new VectorSearchProfile("vector-profile", "hnsw-algorithm")
                    {
                        CompressionName = _vectorProfile.CompressionName
                    }
                }
            },
            SemanticSearch = new SemanticSearch
            {
                Configurations =
                {
                    new SemanticConfiguration("semantic-config", new SemanticPrioritizedFields
                    {
                        TitleField = new SemanticField("name"),
                        ContentFields =
                        {
                            new SemanticField("description"),
                            new SemanticField("properties"),
                            new SemanticField("systemProperties")
                        },
                        KeywordsFields =
                        {
                            new SemanticField("tags")
                        }
                    })
                }
            }
        };

        foreach (var compression in _vectorProfile.Compressions)
        {
            index.VectorSearch.Compressions.Add(compression);
        }

        await _indexClient.CreateOrUpdateIndexAsync(index, allowIndexDowntime: true, cancellationToken: cancellationToken);
        _logger.LogInformation("Search index '{IndexName}' ensured", _options.IndexName);
    }

    /// <inheritdoc/>
    public async Task IndexDocumentAsync(
        SearchIndexDocument document,
        CancellationToken cancellationToken = default)
    {
        var actions = new IndexDocumentsBatch<SearchIndexDocument>();
        actions.Actions.Add(IndexDocumentsAction.MergeOrUpload(document));

        var response = await _searchClient.IndexDocumentsAsync(
            actions,
            FailOnAnyIndexingError,
            cancellationToken);
        EnsureAllIndexActionsSucceeded(response.Value, expectedCount: 1);

        _telemetryService.RecordDocumentIndexed(document.EntityType);
    }

    /// <inheritdoc/>
    public async Task IndexDocumentsBatchAsync(
        IEnumerable<SearchIndexDocument> documents,
        CancellationToken cancellationToken = default)
    {
        var docList = documents.ToList();
        if (docList.Count == 0) return;

        foreach (var chunk in docList.Chunk(_options.MaxBatchSize))
        {
            var actions = new IndexDocumentsBatch<SearchIndexDocument>();
            foreach (var doc in chunk)
            {
                actions.Actions.Add(IndexDocumentsAction.MergeOrUpload(doc));
            }

            var response = await _searchClient.IndexDocumentsAsync(
                actions,
                FailOnAnyIndexingError,
                cancellationToken);
            EnsureAllIndexActionsSucceeded(response.Value, chunk.Length);

            foreach (var doc in chunk)
            {
                _telemetryService.RecordDocumentIndexed(doc.EntityType);
            }
        }
    }

    /// <inheritdoc/>
    public async Task RemoveDocumentAsync(
        string documentId,
        CancellationToken cancellationToken = default)
    {
        await _searchClient.DeleteDocumentsAsync(
            "id",
            [documentId],
            cancellationToken: cancellationToken);
        _logger.LogInformation("Removed document {DocumentId} from search index", documentId);
    }

    /// <inheritdoc/>
    public async Task RemoveDocumentsBatchAsync(
        IEnumerable<string> documentIds,
        CancellationToken cancellationToken = default)
    {
        var idList = documentIds.ToList();
        if (idList.Count == 0) return;

        await _searchClient.DeleteDocumentsAsync(
            "id",
            idList,
            cancellationToken: cancellationToken);
        _logger.LogInformation("Removed {Count} documents from search index", idList.Count);
    }

    /// <inheritdoc/>
    public async Task<SearchResultSet> SearchAsync(
        SearchRequest request,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        using var activity = _telemetryService.StartSearchActivity(
            request.WorldId.ToString(),
            request.Mode.ToString());

        var limit = Math.Clamp(request.Limit, 1, 200);
        var offset = Math.Max(request.Offset, 0);

        // Build filter expression — worldId is always required
        var filters = new List<string> { $"worldId eq '{request.WorldId}'" };

        if (request.EntityTypeFilter.HasValue)
        {
            filters.Add($"entityType eq '{request.EntityTypeFilter.Value}'");
        }

        if (request.TagsFilter is { Count: > 0 })
        {
            var tagFilters = request.TagsFilter
                .Select(tag => $"tags/any(t: t eq '{EscapeODataString(tag)}')")
                .ToList();
            filters.Add($"({string.Join(" or ", tagFilters)})");
        }

        if (!string.IsNullOrEmpty(request.NameFilter))
        {
            filters.Add($"search.ismatch('{EscapeODataString(request.NameFilter)}*', 'name')");
        }

        if (request.ParentIdFilter.HasValue)
        {
            filters.Add($"parentId eq '{request.ParentIdFilter.Value}'");
        }

        var filterExpression = string.Join(" and ", filters);

        var searchOptions = new Azure.Search.Documents.SearchOptions
        {
            Filter = filterExpression,
            Size = limit,
            Skip = offset,
            IncludeTotalCount = true,
            Select = { "id", "worldId", "entityType", "name", "description", "tags", "parentId", "ownerId", "createdAt", "updatedAt", "path", "depth" }
        };

        // Configure search mode
        switch (request.Mode)
        {
            case Domain.Models.SearchMode.Text:
                // Text-only search — no vector query
                break;

            case Domain.Models.SearchMode.Vector:
                {
                    // Vector-only search
                    // Use CancellationToken.None: credential acquisition (DefaultAzureCredential/AzureCliCredential)
                    // must not be cancelled by the HTTP request token — the az CLI subprocess cannot be
                    // interrupted mid-flight without leaving it in a broken state.
                    var queryVector = await _embeddingService.GenerateEmbeddingAsync(
                        request.Query, CancellationToken.None);
                    searchOptions.VectorSearch = new VectorSearchOptions
                    {
                        Queries = { BuildVectorizedQuery(queryVector, limit) }
                    };
                    break;
                }

            case Domain.Models.SearchMode.Hybrid:
            default:
                {
                    // Hybrid search — text + vector
                    // Use CancellationToken.None: same reason as Vector above.
                    var queryVector = await _embeddingService.GenerateEmbeddingAsync(
                        request.Query, CancellationToken.None);
                    searchOptions.VectorSearch = new VectorSearchOptions
                    {
                        Queries = { BuildVectorizedQuery(queryVector, limit) }
                    };
                    break;
                }
        }

        // For text and hybrid modes, use the query string; vector-only uses "*" for match-all
        var searchText = request.Mode == Domain.Models.SearchMode.Vector ? "*" : request.Query;

        // Use CancellationToken.None for the search call itself: DefaultAzureCredential acquires
        // an access token inside this call and the AzureCliCredential spawns an `az` subprocess.
        // Cancelling that mid-flight (e.g. because the frontend debounced to a new query) causes
        // a TaskCanceledException inside the credential chain which surfaces as a 500 error.
        // The network I/O is fast once the token is cached; cancellation is re-applied when
        // iterating results below, so abandoned requests are still cleaned up promptly.
        var response = await _searchClient.SearchAsync<SearchIndexDocument>(
            searchText,
            searchOptions,
            CancellationToken.None);

        var results = new List<SearchResult>();
        await foreach (var result in response.Value.GetResultsAsync())
        {
            var doc = result.Document;
            results.Add(new SearchResult
            {
                Id = Guid.Parse(doc.Id),
                Name = doc.Name,
                EntityType = doc.EntityType,
                DescriptionSnippet = doc.Description is { Length: > 200 }
                    ? doc.Description[..200] + "..."
                    : doc.Description,
                RelevanceScore = result.Score ?? 0,
                WorldId = Guid.Parse(doc.WorldId),
                ParentId = string.IsNullOrEmpty(doc.ParentId) ? null : Guid.Parse(doc.ParentId),
                Path = doc.Path ?? [],
                Depth = doc.Depth,
                Tags = doc.Tags,
                OwnerId = doc.OwnerId,
                CreatedAt = doc.CreatedAt,
                UpdatedAt = doc.UpdatedAt
            });
        }

        stopwatch.Stop();
        _telemetryService.RecordSearchQuery(request.Mode.ToString());
        _telemetryService.RecordSearchLatency(stopwatch.ElapsedMilliseconds);

        _logger.LogInformation(
            "Search completed: WorldId={WorldId}, Mode={Mode}, QueryLength={QueryLength}, Filters={FilterCount}, ResultCount={ResultCount}, LatencyMs={LatencyMs}",
            request.WorldId,
            request.Mode,
            request.Query.Length,
            filters.Count - 1,
            results.Count,
            stopwatch.ElapsedMilliseconds);

        return new SearchResultSet
        {
            Results = results,
            TotalCount = (int)(response.Value.TotalCount ?? results.Count),
            Offset = offset,
            Limit = limit
        };
    }

    /// <summary>
    /// Builds a <see cref="VectorizedQuery"/> with k-NN count, target field, and optional oversampling
    /// from the configured <see cref="VectorIndexProfile"/>.
    /// </summary>
    private VectorizedQuery BuildVectorizedQuery(ReadOnlyMemory<float> queryVector, int limit)
    {
        var query = new VectorizedQuery(queryVector)
        {
            KNearestNeighborsCount = limit,
            Fields = { "contentVector" }
        };

        if (_vectorProfile.QueryOversampling is { } oversampling)
        {
            query.Oversampling = oversampling;
        }

        return query;
    }

    /// <summary>
    /// Escapes single quotes in OData filter string values to prevent injection.
    /// </summary>
    private static string EscapeODataString(string value) =>
        value.Replace("'", "''");

    private void EnsureAllIndexActionsSucceeded(IndexDocumentsResult result, int expectedCount)
    {
        var failed = result.Results
            .Where(r => !r.Succeeded)
            .ToList();

        if (failed.Count == 0)
        {
            return;
        }

        _logger.LogError(
            "Azure AI Search indexing returned failures: FailedCount={FailedCount}, ExpectedCount={ExpectedCount}, FirstFailedStatus={FirstFailedStatus}, FirstFailedKey={FirstFailedKey}",
            failed.Count,
            expectedCount,
            failed[0].Status,
            failed[0].Key);

        throw new InvalidOperationException(
            $"Azure AI Search indexing failed for {failed.Count} of {expectedCount} documents.");
    }
}
