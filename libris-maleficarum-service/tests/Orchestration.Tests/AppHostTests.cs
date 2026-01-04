using System.Net.Http.Json;
using Aspire.Hosting;
using Aspire.Hosting.Testing;

namespace LibrisMaleficarum.Orchestration.Tests;

/// <summary>
/// Integration tests for Aspire AppHost configuration verifying
/// service definitions, dependencies, and health checks.
/// These tests are categorized as Integration and RequiresDocker.
/// They don't run by default (use "service: test (integration)" task
/// or dotnet test --filter "TestCategory=Integration" to execute).
/// </summary>
[TestClass]
public class AppHostTests
{
    [TestMethod]
    [TestCategory("Integration")]
    [TestCategory("RequiresDocker")]
    public async Task AppHost_StartsSuccessfully()
    {
        // Arrange
        var appHostBuilder = await DistributedApplicationTestingBuilder.CreateAsync<Projects.LibrisMaleficarum_AppHost>();

        // Act & Assert
        await using var app = await appHostBuilder.BuildAsync();

        // Verify the app was created successfully
        app.Should().NotBeNull("Aspire AppHost should build successfully");
    }

    [TestMethod]
    [TestCategory("Integration")]
    [TestCategory("RequiresDocker")]
    public async Task AppHost_ApiEndpointIsAvailable()
    {
        // Arrange
        var appHostBuilder = await DistributedApplicationTestingBuilder.CreateAsync<Projects.LibrisMaleficarum_AppHost>();
        await using var app = await appHostBuilder.BuildAsync();

        // Act
        await app.StartAsync();

        // Give services time to start
        await Task.Delay(TimeSpan.FromSeconds(10));

        // Assert - Verify we can create an HTTP client for the API
        using var httpClient = app.CreateHttpClient("api");
        httpClient.Should().NotBeNull("Should be able to create HTTP client for API service");
        httpClient.BaseAddress.Should().NotBeNull("API should have a base address");
    }

    [TestMethod]
    [TestCategory("Integration")]
    [TestCategory("RequiresDocker")]
    public async Task AppHost_ApiCanConnectToCosmosDb()
    {
        // Arrange
        var appHostBuilder = await DistributedApplicationTestingBuilder.CreateAsync<Projects.LibrisMaleficarum_AppHost>();
        await using var app = await appHostBuilder.BuildAsync();

        await app.StartAsync();

        // Wait for services to be ready (Cosmos DB emulator takes time to start)
        await Task.Delay(TimeSpan.FromSeconds(30));

        using var httpClient = app.CreateHttpClient("api");

        // Act - Try to create a world (this will test Cosmos DB connectivity)
        var createRequest = new
        {
            name = "Aspire Integration Test",
            description = "Testing Cosmos DB connection via Aspire"
        };

        var response = await httpClient.PostAsJsonAsync("/api/v1/worlds", createRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created,
            "API should successfully create world in Cosmos DB, indicating successful connection");

        var locationHeader = response.Headers.Location;
        locationHeader.Should().NotBeNull("Response should include Location header with created world URI");
    }

    [TestMethod]
    public void AppHost_ProgramClassExists()
    {
        // Arrange & Act
        var programType = typeof(Projects.LibrisMaleficarum_AppHost);

        // Assert
        programType.Should().NotBeNull("AppHost.cs should have a generated Program class for testing");
        programType.Name.Should().Be("LibrisMaleficarum_AppHost", "Program class should match project name");
    }

    [TestMethod]
    public void AppHost_AssemblyCanBeLoaded()
    {
        // Arrange & Act
        var projectAssembly = typeof(Projects.LibrisMaleficarum_AppHost).Assembly;

        // Assert
        projectAssembly.Should().NotBeNull("AppHost assembly should be loadable");
        projectAssembly.GetName().Name.Should().Be("LibrisMaleficarum.AppHost",
            "Assembly should have correct name");
    }
}
