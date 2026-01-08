namespace LibrisMaleficarum.Infrastructure.IntegrationTests;

using LibrisMaleficarum.IntegrationTests.Shared;

/// <summary>
/// Minimal Aspire AppHost integration tests to verify basic functionality.
/// Uses a shared AppHost instance across all tests via AppHostFixture.
/// </summary>
[TestClass]
[TestCategory("Integration")]
[TestCategory("RequiresDocker")]
[DoNotParallelize] // AppHost tests must run sequentially to avoid port conflicts
public class MinimalAppHostTests
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
        AppHostFixture.App.Should().NotBeNull();
        TestContext?.WriteLine("[TEST] ✓ AppHost is available and running!");
    }

    [TestMethod]
    public async Task AppHost_CosmosDbConnectionString_CanBeRetrieved()
    {
        // Arrange
        AppHostFixture.App.Should().NotBeNull("AppHost should be initialized by ClassInitialize");

        // Act
        TestContext?.WriteLine("[TEST] Retrieving Cosmos DB connection string...");
        var connectionString = await AppHostFixture.App!.GetConnectionStringAsync("cosmosdb", TestContext?.CancellationTokenSource.Token ?? default);
        TestContext?.WriteLine($"[TEST] Connection string retrieved: {connectionString?[..50]}...");

        // Assert
        connectionString.Should().NotBeNullOrWhiteSpace();
        connectionString.Should().Contain("AccountEndpoint=http://"); // Emulator uses HTTP
        TestContext?.WriteLine("[TEST] ✓ Connection string test passed!");
    }
}
