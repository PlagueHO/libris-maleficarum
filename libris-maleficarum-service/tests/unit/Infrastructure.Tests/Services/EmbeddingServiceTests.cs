namespace LibrisMaleficarum.Infrastructure.Tests.Services;

using FluentAssertions;
using LibrisMaleficarum.Infrastructure.Configuration;
using LibrisMaleficarum.Infrastructure.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using OpenAI.Embeddings;

/// <summary>
/// Unit tests for EmbeddingService.
/// Tests null/empty input handling, constructor guards, and batch edge cases.
/// </summary>
[TestClass]
[TestCategory("Unit")]
public class EmbeddingServiceTests
{
    private ILogger<EmbeddingService> _logger = null!;
    private IOptions<SearchOptions> _options = null!;

    private const int DefaultEmbeddingDimensions = 1536;

    [TestInitialize]
    public void Setup()
    {
        _logger = Substitute.For<ILogger<EmbeddingService>>();
        _options = Options.Create(new SearchOptions
        {
            EmbeddingDimensions = DefaultEmbeddingDimensions,
            EmbeddingModelName = "text-embedding-3-small",
            IndexName = "test-index",
            MaxBatchSize = 100,
            ChangeFeedPollIntervalMs = 1000
        });
    }

    #region Constructor Null Checks

    [TestMethod]
    public void Constructor_NullEmbeddingClient_ThrowsArgumentNullException()
    {
        // Arrange & Act
        var act = () => new EmbeddingService(null!, _options, _logger);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("embeddingClient");
    }

    [TestMethod]
    public void Constructor_NullOptions_ThrowsArgumentNullException()
    {
        // Arrange
        // EmbeddingClient is a concrete class; we only need to verify the null guard fires
        // before the client is used, so passing null for the client check separately.
        // Here we verify the options null guard by providing a non-null client placeholder.
        // Since the constructor checks parameters in order, we need a non-null EmbeddingClient.
        // We use a real instance pointed at a dummy endpoint.
        var embeddingClient = CreateDummyEmbeddingClient();

        // Act
        var act = () => new EmbeddingService(embeddingClient, null!, _logger);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("options");
    }

    [TestMethod]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        // Arrange
        var embeddingClient = CreateDummyEmbeddingClient();

        // Act
        var act = () => new EmbeddingService(embeddingClient, _options, null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    #endregion

    #region GenerateEmbeddingAsync - Null/Empty Text

    [TestMethod]
    public async Task GenerateEmbeddingAsync_NullText_ReturnsZeroVector()
    {
        // Arrange
        var service = CreateServiceWithDummyClient();

        // Act
        var result = await service.GenerateEmbeddingAsync(null!);

        // Assert
        result.Length.Should().Be(DefaultEmbeddingDimensions);
        result.ToArray().Should().AllSatisfy(f => f.Should().Be(0f));
    }

    [TestMethod]
    public async Task GenerateEmbeddingAsync_EmptyText_ReturnsZeroVector()
    {
        // Arrange
        var service = CreateServiceWithDummyClient();

        // Act
        var result = await service.GenerateEmbeddingAsync(string.Empty);

        // Assert
        result.Length.Should().Be(DefaultEmbeddingDimensions);
        result.ToArray().Should().AllSatisfy(f => f.Should().Be(0f));
    }

    [TestMethod]
    public async Task GenerateEmbeddingAsync_WhitespaceText_ReturnsZeroVector()
    {
        // Arrange
        var service = CreateServiceWithDummyClient();

        // Act
        var result = await service.GenerateEmbeddingAsync("   ");

        // Assert
        result.Length.Should().Be(DefaultEmbeddingDimensions);
        result.ToArray().Should().AllSatisfy(f => f.Should().Be(0f));
    }

    [TestMethod]
    public async Task GenerateEmbeddingAsync_NullText_LogsWarning()
    {
        // Arrange
        var service = CreateServiceWithDummyClient();

        // Act
        await service.GenerateEmbeddingAsync(null!);

        // Assert
        _logger.Received(1).Log(
            LogLevel.Warning,
            Arg.Any<EventId>(),
            Arg.Is<object>(o => o.ToString()!.Contains("Empty or null text")),
            Arg.Any<Exception?>(),
            Arg.Any<Func<object, Exception?, string>>());
    }

    [TestMethod]
    public async Task GenerateEmbeddingAsync_EmptyText_LogsWarning()
    {
        // Arrange
        var service = CreateServiceWithDummyClient();

        // Act
        await service.GenerateEmbeddingAsync(string.Empty);

        // Assert
        _logger.Received(1).Log(
            LogLevel.Warning,
            Arg.Any<EventId>(),
            Arg.Is<object>(o => o.ToString()!.Contains("Empty or null text")),
            Arg.Any<Exception?>(),
            Arg.Any<Func<object, Exception?, string>>());
    }

    #endregion

    #region GenerateEmbeddingsBatchAsync - Edge Cases

    [TestMethod]
    public async Task GenerateEmbeddingsBatchAsync_EmptyList_ReturnsEmpty()
    {
        // Arrange
        var service = CreateServiceWithDummyClient();

        // Act
        var result = await service.GenerateEmbeddingsBatchAsync([]);

        // Assert
        result.Should().BeEmpty();
    }

    [TestMethod]
    public async Task GenerateEmbeddingsBatchAsync_NullTexts_ThrowsArgumentNullException()
    {
        // Arrange
        var service = CreateServiceWithDummyClient();

        // Act
        var act = () => service.GenerateEmbeddingsBatchAsync(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("texts");
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Creates a dummy EmbeddingClient using the Azure OpenAI client.
    /// This client points at a non-existent endpoint and is only suitable
    /// for constructor validation and code paths that don't call the API.
    /// </summary>
    private static EmbeddingClient CreateDummyEmbeddingClient()
    {
        var azureClient = new Azure.AI.OpenAI.AzureOpenAIClient(
            new Uri("https://test.openai.azure.com"),
            new System.ClientModel.ApiKeyCredential("test-key"));

        return azureClient.GetEmbeddingClient("text-embedding-3-small");
    }

    private EmbeddingService CreateServiceWithDummyClient()
    {
        var embeddingClient = CreateDummyEmbeddingClient();
        return new EmbeddingService(embeddingClient, _options, _logger);
    }

    #endregion
}
