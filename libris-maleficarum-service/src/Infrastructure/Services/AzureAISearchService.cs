namespace LibrisMaleficarum.Infrastructure.Services;

using Azure;
using Azure.Search.Documents;
using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Indexes.Models;
using Azure.Search.Documents.Models;
using LibrisMaleficarum.Domain.Interfaces.Services;
using LibrisMaleficarum.Domain.Models;
using LibrisMaleficarum.Infrastructure.Configuration;
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
    private readonly SearchIndexClient _indexClient;
    private readonly SearchClient _searchClient;
    private readonly IEmbeddingService _embeddingService;
    private readonly ITelemetryService _telemetryService;
    private readonly AppSearchOptions _options;
    private readonly ILogger<AzureAISearchService> _logger;

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
                new SimpleField("createdDate", SearchFieldDataType.DateTimeOffset) { IsFilterable = true, IsSortable = true },
                new SimpleField("modifiedDate", SearchFieldDataType.DateTimeOffset) { IsFilterable = true, IsSortable = true },
                new SimpleField("path", SearchFieldDataType.Collection(SearchFieldDataType.String)) { IsFilterable = true },
                new SimpleField("depth", SearchFieldDataType.Int32) { IsFilterable = true, IsSortable = true, IsFacetable = true },
                new SearchableField("attributes"),
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
                            new SemanticField("attributes")
                        },
                        KeywordsFields =
                        {
                            new SemanticField("tags")
                        }
                    })
                }
            }
        };

        await _indexClient.CreateOrUpdateIndexAsync(index, cancellationToken: cancellationToken);
        _logger.LogInformation("Search index '{IndexName}' ensured", _options.IndexName);
    }

    /// <inheritdoc/>
    public async Task IndexDocumentAsync(
        SearchIndexDocument document,
        CancellationToken cancellationToken = default)
    {
        var actions = new IndexDocumentsBatch<SearchIndexDocument>();
        actions.Actions.Add(IndexDocumentsAction.MergeOrUpload(document));

        await _searchClient.IndexDocumentsAsync(actions, cancellationToken: cancellationToken);
        _telemetryService.RecordDocumentIndexed(document.EntityType);
    }

    /// <inheritdoc/>
    public async Task IndexDocumentsBatchAsync(
        IEnumerable<SearchIndexDocument> documents,
        CancellationToken cancellationToken = default)
    {
        var docList = documents.ToList();
        if (docList.Count == 0) return;

        var actions = new IndexDocumentsBatch<SearchIndexDocument>();
        foreach (var doc in docList)
        {
            actions.Actions.Add(IndexDocumentsAction.MergeOrUpload(doc));
        }

        await _searchClient.IndexDocumentsAsync(actions, cancellationToken: cancellationToken);

        foreach (var doc in docList)
        {
            _telemetryService.RecordDocumentIndexed(doc.EntityType);
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
                .Select(tag => $"tags/any(t: t eq '{tag}')")
                .ToList();
            filters.Add($"({string.Join(" or ", tagFilters)})");
        }

        if (!string.IsNullOrEmpty(request.NameFilter))
        {
            filters.Add($"search.ismatch('{request.NameFilter}*', 'name')");
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
            Select = { "id", "worldId", "entityType", "name", "description", "tags", "parentId", "ownerId", "createdDate", "modifiedDate" }
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
                var queryVector = await _embeddingService.GenerateEmbeddingAsync(
                    request.Query, cancellationToken);
                searchOptions.VectorSearch = new VectorSearchOptions
                {
                    Queries =
                    {
                        new VectorizedQuery(queryVector)
                        {
                            KNearestNeighborsCount = limit,
                            Fields = { "contentVector" }
                        }
                    }
                };
                break;
            }

            case Domain.Models.SearchMode.Hybrid:
            default:
            {
                // Hybrid search — text + vector
                var queryVector = await _embeddingService.GenerateEmbeddingAsync(
                    request.Query, cancellationToken);
                searchOptions.VectorSearch = new VectorSearchOptions
                {
                    Queries =
                    {
                        new VectorizedQuery(queryVector)
                        {
                            KNearestNeighborsCount = limit,
                            Fields = { "contentVector" }
                        }
                    }
                };
                break;
            }
        }

        // For text and hybrid modes, use the query string
        var searchText = request.Mode == Domain.Models.SearchMode.Vector ? null : request.Query;

        var response = await _searchClient.SearchAsync<SearchIndexDocument>(
            searchText,
            searchOptions,
            cancellationToken);

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
                Tags = doc.Tags,
                OwnerId = doc.OwnerId,
                CreatedDate = doc.CreatedDate,
                ModifiedDate = doc.ModifiedDate
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

        _logger.LogDebug("Search raw query: {Query}", request.Query);

        return new SearchResultSet
        {
            Results = results,
            TotalCount = (int)(response.Value.TotalCount ?? results.Count),
            Offset = offset,
            Limit = limit
        };
    }
}
