using System.Net.Http.Json;
using Aspire.Hosting;
using Aspire.Hosting.Testing;

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
    private static IDistributedApplicationTestingBuilder? s_appHostBuilder;
    private static DistributedApplication? s_app;

    public TestContext? TestContext { get; set; }

    [ClassInitialize]
    public static async Task ClassInitialize(TestContext context)
    {
        context.WriteLine("[FIXTURE] Creating AppHost builder...");
        s_appHostBuilder = await DistributedApplicationTestingBuilder
            .CreateAsync<Projects.LibrisMaleficarum_AppHost>();
        context.WriteLine("[FIXTURE] AppHost builder created");

        context.WriteLine("[FIXTURE] Building AppHost...");
        s_app = await s_appHostBuilder.BuildAsync();
        context.WriteLine("[FIXTURE] AppHost built");

        context.WriteLine("[FIXTURE] Starting AppHost...");
        await s_app.StartAsync();
        context.WriteLine("[FIXTURE] AppHost started");

        context.WriteLine("[FIXTURE] Waiting 30 seconds for Cosmos DB emulator to be ready...");
        await Task.Delay(TimeSpan.FromSeconds(30));
        context.WriteLine("[FIXTURE] ✓ Fixture initialization complete!");
    }

    [ClassCleanup]
    public static async Task ClassCleanup()
    {
        if (s_app is not null)
        {
            await s_app.DisposeAsync();
        }
    }

    [TestMethod]
    public void AppHost_IsCreatedAndStarted()
    {
        // Assert
        s_app.Should().NotBeNull("Aspire AppHost should build and start successfully");
        TestContext?.WriteLine("[TEST] ✓ AppHost is available and running");
    }

    [TestMethod]
    public void AppHost_ApiEndpointIsAvailable()
    {
        // Arrange
        s_app.Should().NotBeNull("AppHost should be initialized by ClassInitialize");

        // Act
        TestContext?.WriteLine("[TEST] Creating HTTP client for API service...");
        using var httpClient = s_app!.CreateHttpClient("api");
        
        // Assert
        httpClient.Should().NotBeNull("Should be able to create HTTP client for API service");
        httpClient.BaseAddress.Should().NotBeNull("API should have a base address");
        
        TestContext?.WriteLine($"[TEST] ✓ API endpoint available at: {httpClient.BaseAddress}");
    }

    [TestMethod]
    public async Task AppHost_ApiHealthEndpointWorks()
    {
        // Arrange
        s_app.Should().NotBeNull("AppHost should be initialized by ClassInitialize");

        // Use HTTP endpoint for testing (HTTPS has cert validation issues in test environment)
        TestContext?.WriteLine("[TEST] Getting HTTP endpoint for API...");
        var httpEndpoint = s_app!.GetEndpoint("api", "http");
        
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
