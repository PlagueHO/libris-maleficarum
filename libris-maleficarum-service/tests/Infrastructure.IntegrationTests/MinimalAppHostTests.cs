namespace LibrisMaleficarum.Infrastructure.IntegrationTests;

using Aspire.Hosting;

/// <summary>
/// Minimal Aspire AppHost integration tests to verify basic functionality.
/// Uses a shared AppHost instance across all tests in this class.
/// </summary>
[TestClass]
[TestCategory("Integration")]
[TestCategory("RequiresDocker")]
[DoNotParallelize] // AppHost tests must run sequentially to avoid port conflicts
public class MinimalAppHostTests
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
        s_app.Should().NotBeNull();
        TestContext?.WriteLine("[TEST] ✓ AppHost is available and running!");
    }

    [TestMethod]
    public async Task AppHost_CosmosDbConnectionString_CanBeRetrieved()
    {
        // Arrange
        s_app.Should().NotBeNull("AppHost should be initialized by ClassInitialize");

        // Act
        TestContext?.WriteLine("[TEST] Retrieving Cosmos DB connection string...");
        var connectionString = await s_app!.GetConnectionStringAsync("cosmosdb", TestContext?.CancellationTokenSource.Token ?? default);
        TestContext?.WriteLine($"[TEST] Connection string retrieved: {connectionString?[..50]}...");

        // Assert
        connectionString.Should().NotBeNullOrWhiteSpace();
        connectionString.Should().Contain("AccountEndpoint=http://"); // Emulator uses HTTP
        TestContext?.WriteLine("[TEST] ✓ Connection string test passed!");
    }
}
