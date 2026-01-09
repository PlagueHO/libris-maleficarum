namespace LibrisMaleficarum.IntegrationTests.Shared;

using Aspire.Hosting;
using Aspire.Hosting.Testing;

/// <summary>
/// Shared fixture for managing the Aspire AppHost lifecycle across integration tests.
/// This class creates a single AppHost instance that is shared across all integration test classes
/// to avoid Docker resource conflicts and improve test performance.
/// </summary>
public static partial class AppHostFixture
{
    private static IDistributedApplicationTestingBuilder? s_appHostBuilder;
    private static DistributedApplication? s_app;
    private static readonly SemaphoreSlim s_initializationLock = new(1, 1);
    private static bool s_isInitialized;
    private static string? s_cosmosDbAccountEndpoint;
    private static string? s_cosmosDbAccountKey;
    private static string? s_cosmosDbConnectionString;

    /// <summary>
    /// Gets the shared DistributedApplication instance.
    /// </summary>
    public static DistributedApplication? App => s_app;

    /// <summary>
    /// Gets the Cosmos DB account endpoint URL.
    /// </summary>
    public static string CosmosDbAccountEndpoint => s_cosmosDbAccountEndpoint 
        ?? throw new InvalidOperationException("AppHost has not been initialized. Call InitializeAsync first.");

    /// <summary>
    /// Gets the Cosmos DB account key.
    /// </summary>
    public static string CosmosDbAccountKey => s_cosmosDbAccountKey 
        ?? throw new InvalidOperationException("AppHost has not been initialized. Call InitializeAsync first.");

    /// <summary>
    /// Gets the Cosmos DB connection string.
    /// </summary>
    public static string CosmosDbConnectionString => s_cosmosDbConnectionString 
        ?? throw new InvalidOperationException("AppHost has not been initialized. Call InitializeAsync first.");

    /// <summary>
    /// Initializes the AppHost if not already initialized.
    /// This method is thread-safe and will only initialize once.
    /// </summary>
    /// <param name="testContext">The test context for logging initialization progress.</param>
    public static async Task InitializeAsync(TestContext testContext)
    {
        if (s_isInitialized)
        {
            testContext.WriteLine("[FIXTURE] AppHost already initialized, reusing existing instance");
            return;
        }

        await s_initializationLock.WaitAsync();
        try
        {
            // Double-check after acquiring lock
            if (s_isInitialized)
            {
                testContext.WriteLine("[FIXTURE] AppHost already initialized by another thread");
                return;
            }

            testContext.WriteLine("[FIXTURE] Creating AppHost builder...");
            // Disable port randomization to ensure consistent port assignment (8081 for Cosmos DB)
            // See: https://aspire.dev/testing/overview/#disable-port-randomization
            s_appHostBuilder = await DistributedApplicationTestingBuilder
                .CreateAsync<Projects.LibrisMaleficarum_AppHost>(
                    [
                        "DcpPublisher:RandomizePorts=false"
                    ]);
            testContext.WriteLine("[FIXTURE] AppHost builder created (port randomization disabled)");

            testContext.WriteLine("[FIXTURE] Building AppHost...");
            s_app = await s_appHostBuilder.BuildAsync();
            testContext.WriteLine("[FIXTURE] AppHost built");

            testContext.WriteLine("[FIXTURE] Starting AppHost...");
            await s_app.StartAsync();
            testContext.WriteLine("[FIXTURE] ✓ AppHost started");

            testContext.WriteLine("[FIXTURE] Waiting for Cosmos DB emulator to be healthy...");
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(60));
            await s_app.ResourceNotifications.WaitForResourceHealthyAsync("cosmosdb", cts.Token);
            testContext.WriteLine("[FIXTURE] ✓ Cosmos DB emulator is healthy!");

            // Display connection information for diagnostics and store for reuse
            s_cosmosDbConnectionString = await s_app.GetConnectionStringAsync("cosmosdb", cts.Token);
            testContext.WriteLine($"[FIXTURE] Connection string: {s_cosmosDbConnectionString}");
            
            // Parse and store connection details
            s_cosmosDbAccountEndpoint = AccountEndpointRegex().Match(s_cosmosDbConnectionString ?? "")?.Groups[1].Value
                ?? throw new InvalidOperationException("AccountEndpoint not found in Cosmos DB connection string");
            s_cosmosDbAccountKey = AccountKeyRegex().Match(s_cosmosDbConnectionString ?? "")?.Groups[1].Value
                ?? throw new InvalidOperationException("AccountKey not found in Cosmos DB connection string");

            testContext.WriteLine($"[FIXTURE] Account endpoint: {s_cosmosDbAccountEndpoint}");
            testContext.WriteLine($"[FIXTURE] Account key: {s_cosmosDbAccountKey[..Math.Min(10, s_cosmosDbAccountKey.Length)]}...");

            testContext.WriteLine("[FIXTURE] ✓ Fixture initialization complete!");

            s_isInitialized = true;
        }
        finally
        {
            s_initializationLock.Release();
        }
    }

    /// <summary>
    /// Disposes the shared AppHost instance.
    /// This should typically only be called during assembly cleanup.
    /// </summary>
    public static async Task CleanupAsync()
    {
        if (s_app is not null)
        {
            await s_app.DisposeAsync();
            s_app = null;
            s_cosmosDbConnectionString = null;
            s_cosmosDbAccountEndpoint = null;
            s_cosmosDbAccountKey = null;
            s_isInitialized = false;
        }
    }

    [System.Text.RegularExpressions.GeneratedRegex(@"AccountKey=([^;]+)")]
    private static partial System.Text.RegularExpressions.Regex AccountKeyRegex();
    [System.Text.RegularExpressions.GeneratedRegex(@"AccountEndpoint=([^;]+)")]
    private static partial System.Text.RegularExpressions.Regex AccountEndpointRegex();
}
