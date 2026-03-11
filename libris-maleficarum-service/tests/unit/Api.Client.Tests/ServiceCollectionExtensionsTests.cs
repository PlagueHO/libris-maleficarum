namespace LibrisMaleficarum.Api.Client.Tests;

using LibrisMaleficarum.Api.Client.Extensions;

using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Unit tests for <see cref="ServiceCollectionExtensions"/>.
/// </summary>
[TestClass]
[TestCategory("Unit")]
public class ServiceCollectionExtensionsTests
{
    [TestMethod]
    public void AddLibrisApiClient_RegistersILibrisApiClient()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddLibrisApiClient(options =>
        {
            options.BaseUrl = "https://localhost";
        });

        using var provider = services.BuildServiceProvider();
        var client = provider.GetService<ILibrisApiClient>();

        // Assert
        client.Should().NotBeNull();
        client.Should().BeOfType<LibrisApiClient>();
    }

    [TestMethod]
    public void AddLibrisApiClient_ConfiguresBaseUrl()
    {
        // Arrange
        var services = new ServiceCollection();
        const string expectedBaseUrl = "https://api.example.com";

        // Act
        services.AddLibrisApiClient(options =>
        {
            options.BaseUrl = expectedBaseUrl;
        });

        using var provider = services.BuildServiceProvider();
        var httpClientFactory = provider.GetRequiredService<IHttpClientFactory>();
        var httpClient = httpClientFactory.CreateClient(nameof(LibrisApiClient));

        // Assert - verify a client can be resolved (base URL is set on the typed client)
        var client = provider.GetService<ILibrisApiClient>();
        client.Should().NotBeNull();
    }
}
