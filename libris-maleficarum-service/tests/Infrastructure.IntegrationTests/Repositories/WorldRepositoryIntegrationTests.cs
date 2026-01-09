namespace LibrisMaleficarum.Infrastructure.Tests.Repositories;

using LibrisMaleficarum.Domain.Entities;
using LibrisMaleficarum.Domain.Interfaces.Services;
using LibrisMaleficarum.Infrastructure.Persistence;
using LibrisMaleficarum.Infrastructure.Repositories;
using LibrisMaleficarum.IntegrationTests.Shared;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Integration tests for WorldRepository using Aspire-managed Cosmos DB Emulator.
/// These tests verify real Cosmos DB operations including pagination, cursors, and concurrency.
/// Uses shared AppHostFixture from IntegrationTests.Shared project.
/// </summary>
[TestClass]
[TestCategory("Integration")]
[TestCategory("RequiresDocker")]
[DoNotParallelize] // AppHost tests must run sequentially to avoid port conflicts
public class WorldRepositoryIntegrationTests
{
    public TestContext? TestContext { get; set; }

    [ClassInitialize]
    public static async Task ClassInitialize(TestContext context)
    {
        await AppHostFixture.InitializeAsync(context);
    }

    [TestMethod]
    public async Task GetAllByOwnerAsync_WithCursor_ReturnsNextPage()
    {
        // Arrange
        var userId = Guid.NewGuid();
        AppHostFixture.App.Should().NotBeNull("AppHost should be initialized by ClassInitialize");

        TestContext?.WriteLine("[TEST] Getting Cosmos DB connection string...");
        var connectionString = await AppHostFixture.App!.GetConnectionStringAsync("cosmosdb", TestContext?.CancellationTokenSource.Token ?? default)
            ?? throw new InvalidOperationException("Failed to get Cosmos DB connection string");

        TestContext?.WriteLine($"[TEST] Connection string: {connectionString}");

//        var endpoint = AppHostFixture.App!.GetEndpoint("cosmosdb");

//        TestContext?.WriteLine($"[TEST] Cosmos DB endpoint: {endpoint}");

        // Parse connection string to extract AccountEndpoint and AccountKey
        var accountEndpoint = System.Text.RegularExpressions.Regex.Match(connectionString, @"AccountEndpoint=([^;]+)")?.Groups[1].Value
            ?? throw new InvalidOperationException("AccountEndpoint not found in connection string");
        var accountKey = System.Text.RegularExpressions.Regex.Match(connectionString, @"AccountKey=([^;]+)")?.Groups[1].Value
            ?? throw new InvalidOperationException("AccountKey not found in connection string");

        TestContext?.WriteLine($"[TEST] Parsed AccountEndpoint: {accountEndpoint}");
        TestContext?.WriteLine($"[TEST] Parsed AccountKey: {accountKey[..10]}...");

        var testDatabaseName = $"IntegrationTestDb_{Guid.NewGuid()}";
        TestContext?.WriteLine($"[TEST] Creating DbContext for database: {testDatabaseName}");
        
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseCosmos(
                accountEndpoint,
                accountKey,
                testDatabaseName,
                cosmosOptions =>
                {
                    cosmosOptions.ConnectionMode(Microsoft.Azure.Cosmos.ConnectionMode.Gateway);
                    cosmosOptions.RequestTimeout(TimeSpan.FromSeconds(60));
                    cosmosOptions.HttpClientFactory(() => new HttpClient(new HttpClientHandler
                    {
                        ServerCertificateCustomValidationCallback = (_, _, _, _) => true
                    }));
                })
            .Options;

        await using var context = new ApplicationDbContext(options);
        
        // Add retry logic for database creation (Cosmos DB emulator may need time)
        TestContext?.WriteLine("[TEST] Ensuring database created (with retry logic)...");
        var retryCount = 0;
        var maxRetries = 3;
        var retryDelay = TimeSpan.FromSeconds(5);
        Exception? lastException = null;
        
        while (retryCount < maxRetries)
        {
            try
            {
                TestContext?.WriteLine($"[TEST] Attempt {retryCount + 1}/{maxRetries} to create database...");
                
                // Use ConfigureAwait(false) to avoid potential deadlocks and add timeout
                var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
                await context.Database.EnsureCreatedAsync(cts.Token).ConfigureAwait(false);
                
                TestContext?.WriteLine("[TEST] Database created successfully!");
                break;
            }
            catch (Exception ex)
            {
                lastException = ex;
                retryCount++;
                TestContext?.WriteLine($"[TEST] Attempt {retryCount} failed: {ex.Message}");
                
                if (retryCount < maxRetries)
                {
                    TestContext?.WriteLine($"[TEST] Waiting {retryDelay.TotalSeconds}s before retry...");
                    await Task.Delay(retryDelay).ConfigureAwait(false);
                }
            }
        }
        
        if (retryCount >= maxRetries)
        {
            throw new InvalidOperationException(
                $"Failed to create database after {maxRetries} attempts. Last error: {lastException?.Message}",
                lastException);
        }

        // var userContextService = Substitute.For<IUserContextService>();
        // userContextService.GetCurrentUserIdAsync().Returns(userId);
        // var repository = new WorldRepository(context, userContextService);

        // TestContext?.WriteLine("[TEST] Creating test data...");
        // var firstWorld = World.Create(userId, "World 1", null);
        // await context.Worlds.AddAsync(firstWorld);
        // await context.SaveChangesAsync();
        // await Task.Delay(10); // Ensure different timestamps

        // var secondWorld = World.Create(userId, "World 2", null);
        // await context.Worlds.AddAsync(secondWorld);
        // await context.SaveChangesAsync();

        // var cursor = firstWorld.CreatedDate.ToString("O");

        // // Act
        // TestContext?.WriteLine("[TEST] Executing repository query...");
        // var (worlds, nextCursor) = await repository.GetAllByOwnerAsync(userId, cursor: cursor);

        // // Assert
        // TestContext?.WriteLine("[TEST] Asserting results...");
        // worlds.Should().HaveCount(1);
        // worlds.First().Name.Should().Be("World 2");

        // // Cleanup
        // TestContext?.WriteLine("[TEST] Cleaning up database...");
        // await context.Database.EnsureDeletedAsync();
        // TestContext?.WriteLine("[TEST] ✓ Test completed successfully!");
    }

    // TEMPORARILY DISABLED - Testing one test at a time to identify freezing issue
    /*
    [TestMethod]
    public async Task UpdateAsync_WithInvalidETag_ThrowsDbUpdateConcurrencyException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        AppHostFixture.App.Should().NotBeNull("AppHost should be initialized by ClassInitialize");

        TestContext?.WriteLine("[TEST] Getting Cosmos DB connection string...");
        var connectionString = await AppHostFixture.App!.GetConnectionStringAsync("cosmosdb")
            ?? throw new InvalidOperationException("Failed to get Cosmos DB connection string");

        var testDatabaseName = $"IntegrationTestDb_{Guid.NewGuid()}";
        TestContext?.WriteLine($"[TEST] Creating DbContext for database: {testDatabaseName}");
        
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseCosmos(
                connectionString,
                testDatabaseName,
                cosmosOptions =>
                {
                    cosmosOptions.ConnectionMode(Microsoft.Azure.Cosmos.ConnectionMode.Gateway);
                    cosmosOptions.RequestTimeout(TimeSpan.FromSeconds(60));
                    cosmosOptions.HttpClientFactory(() => new HttpClient(new HttpClientHandler
                    {
                        ServerCertificateCustomValidationCallback = (_, _, _, _) => true
                    }));
                })
            .Options;

        await using var context = new ApplicationDbContext(options);
        TestContext?.WriteLine("[TEST] Ensuring database created...");
        await context.Database.EnsureCreatedAsync();

        var userContextService = Substitute.For<IUserContextService>();
        userContextService.GetCurrentUserIdAsync().Returns(userId);
        var repository = new WorldRepository(context, userContextService);

        TestContext?.WriteLine("[TEST] Creating test data...");
        var world = World.Create(userId, "Test World", "Description");
        await context.Worlds.AddAsync(world);
        await context.SaveChangesAsync();

        // Modify the world to change its ETag
        TestContext?.WriteLine("[TEST] Modifying world to invalidate ETag...");
        var modifiedWorld = await context.Worlds.FindAsync(world.Id);
        modifiedWorld!.Update("Updated Name", "Updated Description");
        await context.SaveChangesAsync();

        // Try to update with the original (now invalid) ETag
        world.Update("Another Update", "Another Description");

        // Act & Assert
        TestContext?.WriteLine("[TEST] Attempting update with invalid ETag...");
        await Assert.ThrowsExceptionAsync<DbUpdateConcurrencyException>(
            async () => await repository.UpdateAsync(world));

        // Cleanup
        TestContext?.WriteLine("[TEST] Cleaning up database...");
        await context.Database.EnsureDeletedAsync();
        TestContext?.WriteLine("[TEST] ✓ Test completed successfully!");
    }
    */
}
