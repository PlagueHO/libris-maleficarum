namespace LibrisMaleficarum.IntegrationTests.Shared;

using Aspire.Hosting;
using Aspire.Hosting.Testing;

/// <summary>
/// Shared fixture for managing the Aspire AppHost lifecycle across integration tests.
/// This class creates a single AppHost instance that is shared across all integration test classes
/// to avoid Docker resource conflicts and improve test performance.
/// </summary>
public static class AppHostFixture
{
    private static IDistributedApplicationTestingBuilder? s_appHostBuilder;
    private static DistributedApplication? s_app;
    private static readonly SemaphoreSlim s_initializationLock = new(1, 1);
    private static bool s_isInitialized;

    /// <summary>
    /// Gets the shared DistributedApplication instance.
    /// </summary>
    public static DistributedApplication? App => s_app;

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
            s_appHostBuilder = await DistributedApplicationTestingBuilder
                .CreateAsync<Projects.LibrisMaleficarum_AppHost>();
            testContext.WriteLine("[FIXTURE] AppHost builder created");

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

            // Display connection information for diagnostics
            var connectionString = await s_app.GetConnectionStringAsync("cosmosdb");
            testContext.WriteLine($"[FIXTURE] Connection string: {connectionString}");
            
            var accountEndpoint = System.Text.RegularExpressions.Regex.Match(connectionString ?? "", @"AccountEndpoint=([^;]+)")?.Groups[1].Value;
            if (!string.IsNullOrEmpty(accountEndpoint))
            {
                testContext.WriteLine($"[FIXTURE] Account endpoint: {accountEndpoint}");
            }

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
            s_isInitialized = false;
        }
    }
}
