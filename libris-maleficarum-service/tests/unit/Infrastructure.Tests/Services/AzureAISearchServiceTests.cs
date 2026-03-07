namespace LibrisMaleficarum.Infrastructure.Tests.Services;

using Azure;
using Azure.Search.Documents;
using Azure.Search.Documents.Indexes;
using FluentAssertions;
using LibrisMaleficarum.Domain.Interfaces.Services;
using LibrisMaleficarum.Infrastructure.Configuration;
using LibrisMaleficarum.Infrastructure.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using AppSearchOptions = LibrisMaleficarum.Infrastructure.Configuration.SearchOptions;

/// <summary>
/// Unit tests for AzureAISearchService.
/// Tests constructor null guards and empty-input edge cases.
/// Methods that require a live Azure Search service are not tested here.
/// </summary>
[TestClass]
[TestCategory("Unit")]
public class AzureAISearchServiceTests
{
    private SearchIndexClient _searchIndexClient = null!;
    private SearchClient _searchClient = null!;
    private IEmbeddingService _embeddingService = null!;
    private ITelemetryService _telemetryService = null!;
    private IOptions<AppSearchOptions> _options = null!;
    private ILogger<AzureAISearchService> _logger = null!;

    [TestInitialize]
    public void Setup()
    {
        _searchIndexClient = new SearchIndexClient(
            new Uri("https://test.search.windows.net"),
            new AzureKeyCredential("test-key"));
        _searchClient = new SearchClient(
            new Uri("https://test.search.windows.net"),
            "test-index",
            new AzureKeyCredential("test-key"));
        _embeddingService = Substitute.For<IEmbeddingService>();
        _telemetryService = Substitute.For<ITelemetryService>();
        _options = Options.Create(new AppSearchOptions
        {
            EmbeddingDimensions = 1536,
            EmbeddingModelName = "text-embedding-3-small",
            IndexName = "test-index",
            MaxBatchSize = 100,
            ChangeFeedPollIntervalMs = 1000
        });
        _logger = Substitute.For<ILogger<AzureAISearchService>>();
    }

    #region Constructor Null Checks

    [TestMethod]
    public void Constructor_NullIndexClient_ThrowsArgumentNullException()
    {
        // Arrange & Act
        var act = () => new AzureAISearchService(
            null!, _searchClient, _embeddingService, _telemetryService, _options, _logger);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("indexClient");
    }

    [TestMethod]
    public void Constructor_NullSearchClient_ThrowsArgumentNullException()
    {
        // Arrange & Act
        var act = () => new AzureAISearchService(
            _searchIndexClient, null!, _embeddingService, _telemetryService, _options, _logger);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("searchClient");
    }

    [TestMethod]
    public void Constructor_NullEmbeddingService_ThrowsArgumentNullException()
    {
        // Arrange & Act
        var act = () => new AzureAISearchService(
            _searchIndexClient, _searchClient, null!, _telemetryService, _options, _logger);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("embeddingService");
    }

    [TestMethod]
    public void Constructor_NullTelemetryService_ThrowsArgumentNullException()
    {
        // Arrange & Act
        var act = () => new AzureAISearchService(
            _searchIndexClient, _searchClient, _embeddingService, null!, _options, _logger);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("telemetryService");
    }

    [TestMethod]
    public void Constructor_NullOptions_ThrowsArgumentNullException()
    {
        // Arrange & Act
        var act = () => new AzureAISearchService(
            _searchIndexClient, _searchClient, _embeddingService, _telemetryService, null!, _logger);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("options");
    }

    [TestMethod]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        // Arrange & Act
        var act = () => new AzureAISearchService(
            _searchIndexClient, _searchClient, _embeddingService, _telemetryService, _options, null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    [TestMethod]
    public void Constructor_ValidParameters_CreatesInstance()
    {
        // Arrange & Act
        var service = new AzureAISearchService(
            _searchIndexClient, _searchClient, _embeddingService, _telemetryService, _options, _logger);

        // Assert
        service.Should().NotBeNull();
    }

    #endregion

    #region IndexDocumentsBatchAsync - Edge Cases

    [TestMethod]
    public async Task IndexDocumentsBatchAsync_EmptyList_ReturnsWithoutCalling()
    {
        // Arrange
        var service = new AzureAISearchService(
            _searchIndexClient, _searchClient, _embeddingService, _telemetryService, _options, _logger);

        // Act — passing an empty list should return immediately
        await service.IndexDocumentsBatchAsync([], CancellationToken.None);

        // Assert — telemetry should NOT have been called since no documents were indexed
        _telemetryService.DidNotReceive().RecordDocumentIndexed(Arg.Any<string>());
    }

    #endregion

    #region RemoveDocumentsBatchAsync - Edge Cases

    [TestMethod]
    public async Task RemoveDocumentsBatchAsync_EmptyList_ReturnsWithoutCalling()
    {
        // Arrange
        var service = new AzureAISearchService(
            _searchIndexClient, _searchClient, _embeddingService, _telemetryService, _options, _logger);

        // Act — passing an empty list should return immediately
        await service.RemoveDocumentsBatchAsync([], CancellationToken.None);

        // Assert — logger should NOT have logged removal since no documents were removed
        _logger.DidNotReceive().Log(
            LogLevel.Information,
            Arg.Any<EventId>(),
            Arg.Is<object>(o => o.ToString()!.Contains("Removed")),
            Arg.Any<Exception?>(),
            Arg.Any<Func<object, Exception?, string>>());
    }

    #endregion
}
