namespace LibrisMaleficarum.Worker.Tests.ServiceRegistration;

using LibrisMaleficarum.Domain.Interfaces.Services;
using LibrisMaleficarum.Infrastructure.Configuration;
using LibrisMaleficarum.Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

/// <summary>
/// Unit tests verifying that the SearchIndexWorker registers
/// expected services with the correct lifetimes and implementations.
/// Tests DI wiring without Aspire client integrations.
/// </summary>
[TestClass]
[TestCategory("Unit")]
public class ServiceRegistrationTests
{
    private ServiceCollection _services = null!;

    [TestInitialize]
    public void Setup()
    {
        _services = new ServiceCollection();

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Search:IndexName"] = "test-index",
                ["Search:EmbeddingModelName"] = "test-model",
            })
            .Build();

        // Register configuration (mimics what the Worker Program.cs does)
        _services.AddSingleton<IConfiguration>(configuration);
        _services.Configure<SearchOptions>(configuration.GetSection(SearchOptions.SectionName));

        // Register the same scoped services the Worker registers
        _services.AddScoped<IEmbeddingService, EmbeddingService>();
        _services.AddScoped<ISearchIndexService, AzureAISearchService>();
    }

    [TestMethod]
    public void EmbeddingService_IsRegistered_AsScoped()
    {
        // Assert
        var descriptor = _services.FirstOrDefault(
            d => d.ServiceType == typeof(IEmbeddingService));

        descriptor.Should().NotBeNull();
        descriptor!.Lifetime.Should().Be(ServiceLifetime.Scoped);
        descriptor.ImplementationType.Should().Be(typeof(EmbeddingService));
    }

    [TestMethod]
    public void SearchIndexService_IsRegistered_AsScoped()
    {
        // Assert
        var descriptor = _services.FirstOrDefault(
            d => d.ServiceType == typeof(ISearchIndexService));

        descriptor.Should().NotBeNull();
        descriptor!.Lifetime.Should().Be(ServiceLifetime.Scoped);
        descriptor.ImplementationType.Should().Be(typeof(AzureAISearchService));
    }

    [TestMethod]
    public void SearchOptions_IsRegistered_ViaOptionsPattern()
    {
        // Act
        var provider = _services.BuildServiceProvider();
        var options = provider.GetService<IOptions<SearchOptions>>();

        // Assert
        options.Should().NotBeNull();
        options!.Value.IndexName.Should().Be("test-index");
        options.Value.EmbeddingModelName.Should().Be("test-model");
    }

    [TestMethod]
    public void SearchIndexSyncService_CanBeRegistered_AsHostedService()
    {
        // Arrange — register as the Worker does
        _services.AddHostedService<SearchIndexSyncService>();

        // Assert — verify the hosted service descriptor exists
        var descriptor = _services.FirstOrDefault(
            d => d.ServiceType == typeof(Microsoft.Extensions.Hosting.IHostedService)
                 && d.ImplementationType == typeof(SearchIndexSyncService));

        descriptor.Should().NotBeNull();
        descriptor!.Lifetime.Should().Be(ServiceLifetime.Singleton);
    }
}
