using System.Net.Http.Json;
using Aspire.Hosting;
using Aspire.Hosting.Testing;
using LibrisMaleficarum.IntegrationTests.Shared;

namespace LibrisMaleficarum.Orchestration.IntegrationTests;

/// <summary>
/// Integration tests for Aspire AppHost configuration verifying
/// service definitions, dependencies, and health checks.
/// Uses a shared AppHost instance across all tests in this class.
/// These tests are categorized as Integration and RequiresDocker.
/// They don't run by default (use "service: test (integration)" task
/// or dotnet test --filter "TestCategory=Integration" to execute).
/// </summary>
[TestClass]
[TestCategory("Integration")]
[TestCategory("RequiresDocker")]
[DoNotParallelize] // REQUIRED - prevents port conflicts from parallel AppHost instances
public class AppHostTests
{
    public TestContext? TestContext { get; set; }

    [ClassInitialize]
    public static async Task ClassInitialize(TestContext context)
    {
        await AppHostFixture.InitializeAsync(context);
    }

    [TestMethod]
    public void AppHost_IsCreatedAndStarted()
    {
        // Assert
        AppHostFixture.App.Should().NotBeNull("Aspire AppHost should build and start successfully");
        TestContext?.WriteLine("[TEST] ✓ AppHost is available and running");
    }

    [TestMethod]
    public void AppHost_ApiEndpointIsAvailable()
    {
        // Arrange
        AppHostFixture.App.Should().NotBeNull("AppHost should be initialized by ClassInitialize");

        // Act
        TestContext?.WriteLine("[TEST] Creating HTTP client for API service...");
        using var httpClient = AppHostFixture.App!.CreateHttpClient("api");

        // Assert
        httpClient.Should().NotBeNull("Should be able to create HTTP client for API service");
        httpClient.BaseAddress.Should().NotBeNull("API should have a base address");

        TestContext?.WriteLine($"[TEST] ✓ API endpoint available at: {httpClient.BaseAddress}");
    }

    [TestMethod]
    public async Task AppHost_ApiHealthEndpointWorks()
    {
        // Arrange
        AppHostFixture.App.Should().NotBeNull("AppHost should be initialized by ClassInitialize");

        // Use HTTP endpoint for testing (HTTPS has cert validation issues in test environment)
        TestContext?.WriteLine("[TEST] Getting HTTP endpoint for API...");
        var httpEndpoint = AppHostFixture.App!.GetEndpoint("api", "http");

        using var httpClient = new HttpClient
        {
            BaseAddress = httpEndpoint
        };

        // Act - Call the health endpoint (doesn't require database)
        // This validates: API is running, HTTP works (no SSL), and Aspire orchestration works
        TestContext?.WriteLine($"[TEST] Calling health endpoint at {httpEndpoint}/health...");
        var response = await httpClient.GetAsync("/health");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK,
            "API health endpoint should return OK, indicating successful HTTP connection via Aspire orchestration");

        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Healthy", "Health endpoint should indicate healthy status");

        TestContext?.WriteLine($"[TEST] ✓ Health endpoint returned: {response.StatusCode} - {content}");
    }

    [TestMethod]
    public async Task AppHost_CosmosDbIsHealthy()
    {
        // Arrange
        AppHostFixture.App.Should().NotBeNull("AppHost should be initialized by ClassInitialize");

        // Act - Verify resource is healthy
        TestContext?.WriteLine("[TEST] Checking Cosmos DB resource health...");
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        await AppHostFixture.App!.ResourceNotifications.WaitForResourceHealthyAsync("cosmosdb", cts.Token);

        // Assert - Connection details are available from fixture
        TestContext?.WriteLine("[TEST] Validating Cosmos DB connection details...");
        AppHostFixture.CosmosDbConnectionString.Should().NotBeNullOrWhiteSpace("Cosmos DB should provide a connection string");
        AppHostFixture.CosmosDbConnectionString.Should().Contain("AccountEndpoint=", "Connection string should contain AccountEndpoint");
        AppHostFixture.CosmosDbConnectionString.Should().Contain("AccountKey=", "Connection string should contain AccountKey");

        AppHostFixture.CosmosDbAccountEndpoint.Should().NotBeNullOrWhiteSpace("AccountEndpoint should be available");
        AppHostFixture.CosmosDbAccountEndpoint.Should().StartWith("http", "AccountEndpoint should be a valid HTTP/HTTPS URL");

        AppHostFixture.CosmosDbAccountKey.Should().NotBeNullOrWhiteSpace("AccountKey should be available");

        TestContext?.WriteLine($"[TEST] ✓ Cosmos DB is healthy with endpoint: {AppHostFixture.CosmosDbAccountEndpoint}");
    }

    [TestMethod]
    public async Task AppHost_CosmosDbEndpointResponds()
    {
        // Arrange
        AppHostFixture.App.Should().NotBeNull("AppHost should be initialized by ClassInitialize");

        // Get account endpoint from fixture
        TestContext?.WriteLine($"[TEST] Using Cosmos DB endpoint: {AppHostFixture.CosmosDbAccountEndpoint}");
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

        // Act - Make HTTP request to the Cosmos DB account endpoint
        using var httpClient = new HttpClient(new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (_, _, _, _) => true
        });

        TestContext?.WriteLine("[TEST] Sending GET request to Cosmos DB account endpoint...");
        var response = await httpClient.GetAsync(AppHostFixture.CosmosDbAccountEndpoint, cts.Token);

        // Assert - Validate Cosmos DB is responding
        response.StatusCode.Should().Be(HttpStatusCode.OK, "Cosmos DB emulator should respond to account endpoint requests");

        var content = await response.Content.ReadAsStringAsync(cts.Token);
        content.Should().NotBeNullOrWhiteSpace("Response should have content");

        // Validate response contains expected Cosmos DB metadata
        content.Should().Contain("databaseAccountEndpoint", "Response should contain Cosmos DB account metadata");
        content.Should().Contain("writableLocations", "Response should contain writable locations");
        content.Should().Contain("readableLocations", "Response should contain readable locations");

        TestContext?.WriteLine("[TEST] ✓ Cosmos DB endpoint is responding correctly");
        TestContext?.WriteLine($"[TEST] Response preview: {content[..Math.Min(200, content.Length)]}...");
    }

    [TestMethod]
    public void AppHost_ProgramClassExists()
    {
        // Arrange & Act
        var programType = typeof(Projects.LibrisMaleficarum_AppHost);

        // Assert
        programType.Should().NotBeNull("AppHost.cs should have a generated Program class for testing");
        programType.Name.Should().Be("LibrisMaleficarum_AppHost", "Program class should match project name");
        TestContext?.WriteLine("[TEST] ✓ Program class exists and is accessible");
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
        TestContext?.WriteLine("[TEST] ✓ AppHost assembly loaded successfully");
    }
}
