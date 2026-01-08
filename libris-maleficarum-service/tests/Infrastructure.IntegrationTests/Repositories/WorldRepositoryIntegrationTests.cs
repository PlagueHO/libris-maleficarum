namespace LibrisMaleficarum.Infrastructure.Tests.Repositories;

using Aspire.Hosting;
using Aspire.Hosting.Testing;
using LibrisMaleficarum.Domain.Entities;
using LibrisMaleficarum.Domain.Interfaces.Services;
using LibrisMaleficarum.Infrastructure.Persistence;
using LibrisMaleficarum.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using NSubstitute;

/// <summary>
/// Integration tests for WorldRepository using Aspire-managed Cosmos DB Emulator.
/// These tests verify real Cosmos DB operations including pagination, cursors, and concurrency.
/// Pattern follows: https://devblogs.microsoft.com/dotnet/getting-started-with-testing-and-dotnet-aspire/
/// Each test creates its own AppHost instance for reliability.
/// </summary>
[TestClass]
[TestCategory("Integration")]
[TestCategory("RequiresDocker")]
[DoNotParallelize] // AppHost tests must run sequentially to avoid port conflicts
public class WorldRepositoryIntegrationTests
{
    public TestContext? TestContext { get; set; }

    // TEMPORARILY DISABLED - Pending shared AppHost fixture implementation
    // [TestMethod]
    // [TestCategory("Integration")]
    // public async Task GetAllByOwnerAsync_WithCursor_ReturnsNextPage()
    // {
    // var userId = Guid.NewGuid();
    // 
    // TestContext?.DisplayMessage(MessageLevel.Informational, "Starting test: GetAllByOwnerAsync_WithCursor_ReturnsNextPage");
    // TestContext?.WriteLine("[TEST] Step 1: Creating AppHost builder...");
    // // Arrange - Create and start AppHost
    // var appHost = await DistributedApplicationTestingBuilder
    // .CreateAsync<Projects.LibrisMaleficarum_AppHost>();
    // 
    // TestContext?.WriteLine("[TEST] Step 2: Building AppHost...");
    // await using var app = await appHost.BuildAsync();
    // 
    // TestContext?.WriteLine("[TEST] Step 3: Starting AppHost...");
    // await app.StartAsync();

    // TestContext?.WriteLine("[TEST] Step 4: Waiting for Cosmos DB to be ready (30s)...");
    // // Wait for Cosmos DB to be ready (emulator takes time to start)
    // await Task.Delay(TimeSpan.FromSeconds(30));

    // TestContext?.WriteLine("[TEST] Step 5: Getting connection string...");
    // // Get connection string and create context
    // var connectionString = await app.GetConnectionStringAsync("cosmosdb")
    // ?? throw new InvalidOperationException("Failed to get Cosmos DB connection string");
    // 
    // TestContext?.WriteLine($"[TEST] Step 6: Connection string obtained: {connectionString.Substring(0, Math.Min(50, connectionString.Length))}...");

    // var testDatabaseName = $"IntegrationTestDb_{Guid.NewGuid()}";
    // TestContext?.WriteLine($"[TEST] Step 7: Creating DbContext for database: {testDatabaseName}");
    // var options = new DbContextOptionsBuilder<ApplicationDbContext>()
    // .UseCosmos(
    // connectionString,
    // testDatabaseName,
    // cosmosOptions =>
    // {
    // cosmosOptions.ConnectionMode(Microsoft.Azure.Cosmos.ConnectionMode.Gateway);
    // cosmosOptions.RequestTimeout(TimeSpan.FromSeconds(60));
    // cosmosOptions.HttpClientFactory(() => new HttpClient(new HttpClientHandler
    // {
    // ServerCertificateCustomValidationCallback = (_, _, _, _) => true
    // }));
    // })
    // .Options;

    // TestContext?.WriteLine("[TEST] Step 8: Creating ApplicationDbContext...");
    // await using var context = new ApplicationDbContext(options);
    // 
    // TestContext?.WriteLine("[TEST] Step 9: Ensuring database created (with retry logic)...");
    // // Retry logic for database creation (Cosmos DB emulator might still be starting)
    // var retryCount = 0;
    // var maxRetries = 5;
    // var retryDelay = TimeSpan.FromSeconds(10);
    // Exception? lastException = null;
    // 
    // while (retryCount < maxRetries)
    // {
    // try
    // {
    // TestContext?.WriteLine($"[TEST] Step 9.{retryCount + 1}: Attempting EnsureCreatedAsync (attempt {retryCount + 1}/{maxRetries})...");
    // await context.Database.EnsureCreatedAsync();
    // TestContext?.WriteLine("[TEST] Step 10: Database created successfully!");
    // break;
    // }
    // catch (Exception ex)
    // {
    // lastException = ex;
    // retryCount++;
    // TestContext?.WriteLine($"[TEST] Step 9.{retryCount}: Failed (attempt {retryCount}/{maxRetries}): {ex.Message}");
    // 
    // if (retryCount < maxRetries)
    // {
    // TestContext?.WriteLine($"[TEST] Waiting {retryDelay.TotalSeconds}s before retry...");
    // await Task.Delay(retryDelay);
    // }
    // }
    // }
    // 
    // if (retryCount >= maxRetries)
    // {
    // throw new InvalidOperationException(
    // $"Failed to create database after {maxRetries} attempts. Last error: {lastException?.Message}",
    // lastException);
    // }

    // TestContext?.WriteLine("[TEST] Step 11: Creating repository...");
    // var userContextService = Substitute.For<IUserContextService>();
    // userContextService.GetCurrentUserIdAsync().Returns(userId);
    // var repository = new WorldRepository(context, userContextService);

    // TestContext?.WriteLine("[TEST] Step 12: Creating test data...");
    // // Test data
    // var firstWorld = World.Create(userId, "World 1", null);
    // await context.Worlds.AddAsync(firstWorld);
    // await context.SaveChangesAsync();
    // await Task.Delay(10); // Ensure different timestamps

    // var secondWorld = World.Create(userId, "World 2", null);
    // await context.Worlds.AddAsync(secondWorld);
    // await context.SaveChangesAsync();

    // var cursor = firstWorld.CreatedDate.ToString("O");

    // TestContext?.WriteLine("[TEST] Step 13: Executing repository query...");
    // // Act
    // var (worlds, nextCursor) = await repository.GetAllByOwnerAsync(userId, cursor: cursor);

    // TestContext?.WriteLine("[TEST] Step 14: Asserting results...");
    // // Assert
    // worlds.Should().HaveCount(1);
    // worlds.First().Name.Should().Be("World 2");

    // TestContext?.WriteLine("[TEST] Step 15: Cleaning up database...");
    // // Cleanup
    // await context.Database.EnsureDeletedAsync();
    // TestContext?.WriteLine("[TEST] ✓ Test completed successfully!");
    // }

    // TEMPORARILY DISABLED - Investigating hang issue
    // [TestMethod]
    // [TestCategory("Integration")]
    // public async Task UpdateAsync_WithInvalidETag_ThrowsInvalidOperationException()
    // {
    // var userId = Guid.NewGuid();

    // // Arrange - Create and start AppHost
    // var appHost = await DistributedApplicationTestingBuilder
    //     .CreateAsync<Projects.LibrisMaleficarum_AppHost>();
    // await using var app = await appHost.BuildAsync();
    // await app.StartAsync();

    // // Wait for Cosmos DB to be ready
    // await Task.Delay(TimeSpan.FromSeconds(30));

    // // Get connection string and create context
    // var connectionString = await app.GetConnectionStringAsync("cosmosdb")
    //     ?? throw new InvalidOperationException("Failed to get Cosmos DB connection string");

    // var testDatabaseName = $"IntegrationTestDb_{Guid.NewGuid()}";
    // var options = new DbContextOptionsBuilder<ApplicationDbContext>()
    //     .UseCosmos(
    //         connectionString,
    //         testDatabaseName,
    //         cosmosOptions =>
    //         {
    //             cosmosOptions.ConnectionMode(Microsoft.Azure.Cosmos.ConnectionMode.Gateway);
    //             cosmosOptions.RequestTimeout(TimeSpan.FromSeconds(60));
    //             cosmosOptions.HttpClientFactory(() => new HttpClient(new HttpClientHandler
    //             {
    //                 ServerCertificateCustomValidationCallback = (_, _, _, _) => true
    //             }));
    //         })
    //     .Options;

    // await using var context = new ApplicationDbContext(options);
    // await context.Database.EnsureCreatedAsync();

    // var userContextService = Substitute.For<IUserContextService>();
    // userContextService.GetCurrentUserIdAsync().Returns(userId);
    // var repository = new WorldRepository(context, userContextService);

    // // Test data
    // var world = World.Create(userId, "Test World", "Description");
    // await context.Worlds.AddAsync(world);
    // await context.SaveChangesAsync();

    // // Modify the world to change its ETag
    // var modifiedWorld = await context.Worlds.FindAsync(world.Id);
    // modifiedWorld!.Update("Updated Name", "Updated Description");
    // await context.SaveChangesAsync();

    // // Try to update with the original (now invalid) ETag
    // world.Update("Another Update", "Another Description");

    // // Act & Assert
    // await Assert.ThrowsExceptionAsync<DbUpdateConcurrencyException>(
    //     async () => await repository.UpdateAsync(world));

    // // Cleanup
    // await context.Database.EnsureDeletedAsync();
    // }
}
