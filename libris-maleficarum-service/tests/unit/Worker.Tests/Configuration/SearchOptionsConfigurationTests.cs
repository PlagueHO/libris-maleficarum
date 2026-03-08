namespace LibrisMaleficarum.Worker.Tests.Configuration;

using LibrisMaleficarum.Infrastructure.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

/// <summary>
/// Unit tests for SearchOptions configuration binding in the SearchIndexWorker.
/// Verifies that configuration values bind correctly from appsettings.
/// </summary>
[TestClass]
[TestCategory("Unit")]
public class SearchOptionsConfigurationTests
{
    [TestMethod]
    public void SearchOptions_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var options = new SearchOptions();

        // Assert
        options.IndexName.Should().Be("worldentity-index");
        options.EmbeddingModelName.Should().Be("text-embedding-3-small");
        options.EmbeddingDimensions.Should().Be(1536);
        options.MaxBatchSize.Should().Be(100);
        options.ChangeFeedPollIntervalMs.Should().Be(1000);
    }

    [TestMethod]
    public void SearchOptions_BindsFromConfiguration_Correctly()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Search:IndexName"] = "custom-index",
                ["Search:EmbeddingModelName"] = "custom-model",
                ["Search:EmbeddingDimensions"] = "768",
                ["Search:MaxBatchSize"] = "50",
                ["Search:ChangeFeedPollIntervalMs"] = "5000",
            })
            .Build();

        var services = new ServiceCollection();
        services.Configure<SearchOptions>(configuration.GetSection(SearchOptions.SectionName));
        var provider = services.BuildServiceProvider();

        // Act
        var options = provider.GetRequiredService<IOptions<SearchOptions>>().Value;

        // Assert
        options.IndexName.Should().Be("custom-index");
        options.EmbeddingModelName.Should().Be("custom-model");
        options.EmbeddingDimensions.Should().Be(768);
        options.MaxBatchSize.Should().Be(50);
        options.ChangeFeedPollIntervalMs.Should().Be(5000);
    }

    [TestMethod]
    public void SearchOptions_PartialConfiguration_UsesDefaultsForMissing()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Search:IndexName"] = "partial-index",
            })
            .Build();

        var services = new ServiceCollection();
        services.Configure<SearchOptions>(configuration.GetSection(SearchOptions.SectionName));
        var provider = services.BuildServiceProvider();

        // Act
        var options = provider.GetRequiredService<IOptions<SearchOptions>>().Value;

        // Assert
        options.IndexName.Should().Be("partial-index");
        options.EmbeddingModelName.Should().Be("text-embedding-3-small");
        options.EmbeddingDimensions.Should().Be(1536);
        options.MaxBatchSize.Should().Be(100);
        options.ChangeFeedPollIntervalMs.Should().Be(1000);
    }

    [TestMethod]
    public void SearchOptions_EmptyConfiguration_UsesAllDefaults()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();

        var services = new ServiceCollection();
        services.Configure<SearchOptions>(configuration.GetSection(SearchOptions.SectionName));
        var provider = services.BuildServiceProvider();

        // Act
        var options = provider.GetRequiredService<IOptions<SearchOptions>>().Value;

        // Assert
        options.IndexName.Should().Be("worldentity-index");
        options.EmbeddingModelName.Should().Be("text-embedding-3-small");
        options.EmbeddingDimensions.Should().Be(1536);
        options.MaxBatchSize.Should().Be(100);
        options.ChangeFeedPollIntervalMs.Should().Be(1000);
    }

    [TestMethod]
    public void SearchOptions_SectionName_IsSearch()
    {
        // Assert
        SearchOptions.SectionName.Should().Be("Search");
    }
}
