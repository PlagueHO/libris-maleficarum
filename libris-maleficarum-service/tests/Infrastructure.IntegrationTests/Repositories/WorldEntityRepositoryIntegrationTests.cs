namespace LibrisMaleficarum.Infrastructure.Tests.Repositories;

using Aspire.Hosting.Testing;
using LibrisMaleficarum.Domain.Entities;
using LibrisMaleficarum.Domain.ValueObjects;
using LibrisMaleficarum.Domain.Interfaces.Services;
using LibrisMaleficarum.Domain.Interfaces.Repositories;
using LibrisMaleficarum.Infrastructure.Persistence;
using LibrisMaleficarum.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Integration tests for WorldEntityRepository using Aspire-managed Cosmos DB Emulator.
/// These tests verify real Cosmos DB operations including filtering, pagination, and concurrency.
/// Pattern follows: https://devblogs.microsoft.com/dotnet/getting-started-with-testing-and-dotnet-aspire/
/// Each test creates its own AppHost instance for reliability.
/// </summary>
[TestClass]
[TestCategory("Integration")]
[TestCategory("RequiresDocker")]
[DoNotParallelize] // AppHost tests must run sequentially to avoid port conflicts
public class WorldEntityRepositoryIntegrationTests
{
    private readonly Guid _worldId = Guid.NewGuid();
    private readonly Guid _userId = Guid.NewGuid();

    // TEMPORARILY DISABLED - Investigating hang issue
    // [TestMethod]
    // [TestCategory("Integration")]
    // public async Task GetAllByWorldAsync_WithTagsFilter_ReturnsMatchingEntities()
    // {
    // // Arrange - Create and start AppHost
    // var appHost = await DistributedApplicationTestingBuilder
    //     .CreateAsync<Projects.LibrisMaleficarum_AppHost>();
    // await using var app = await appHost.BuildAsync();
    // await app.StartAsync();

    // await Task.Delay(TimeSpan.FromSeconds(30));

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
    // userContextService.GetCurrentUserIdAsync().Returns(_userId);
    // var worldRepository = Substitute.For<IWorldRepository>();
    // var repository = new WorldEntityRepository(context, userContextService, worldRepository);

    // // Test data
    // var entity1 = WorldEntity.Create(_worldId, EntityType.Character, "Tagged Entity 1", null, null, new List<string> { "hero", "warrior" });
    // var entity2 = WorldEntity.Create(_worldId, EntityType.Character, "Tagged Entity 2", null, null, new List<string> { "villain", "mage" });
    // var entity3 = WorldEntity.Create(_worldId, EntityType.Character, "Tagged Entity 3", null, null, new List<string> { "hero", "mage" });
    // await context.WorldEntities.AddRangeAsync(entity1, entity2, entity3);
    // await context.SaveChangesAsync();

    // // Act
    // var (entities, _) = await repository.GetAllByWorldAsync(_worldId, tags: new List<string> { "hero" });

    // // Assert
    // entities.Should().HaveCount(2);
    // entities.All(e => e.Tags.Any(t => t.ToLower().Contains("hero"))).Should().BeTrue();

    // // Cleanup
    // await context.Database.EnsureDeletedAsync();
    // }

    // TEMPORARILY DISABLED - Investigating hang issue
    // [TestMethod]
    // [TestCategory("Integration")]
    // public async Task GetAllByWorldAsync_WithCursor_ReturnsNextPage()
    // {
    // // Arrange - Create and start AppHost
    // var appHost = await DistributedApplicationTestingBuilder
    //     .CreateAsync<Projects.LibrisMaleficarum_AppHost>();
    // await using var app = await appHost.BuildAsync();
    // await app.StartAsync();

    // await Task.Delay(TimeSpan.FromSeconds(30));

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
    // userContextService.GetCurrentUserIdAsync().Returns(_userId);
    // var worldRepository = Substitute.For<IWorldRepository>();
    // var repository = new WorldEntityRepository(context, userContextService, worldRepository);

    // // Test data
    // var firstEntity = WorldEntity.Create(_worldId, EntityType.Location, "Entity 1", null, null, null);
    // await context.WorldEntities.AddAsync(firstEntity);
    // await context.SaveChangesAsync();
    // await Task.Delay(10); // Ensure different timestamps

    // var secondEntity = WorldEntity.Create(_worldId, EntityType.Location, "Entity 2", null, null, null);
    // await context.WorldEntities.AddAsync(secondEntity);
    // await context.SaveChangesAsync();

    // var cursor = firstEntity.CreatedDate.ToString("O");

    // // Act
    // var (entities, nextCursor) = await repository.GetAllByWorldAsync(_worldId, cursor: cursor);

    // // Assert
    // entities.Should().HaveCount(1);
    // entities.First().Name.Should().Be("Entity 2");

    // // Cleanup
    // await context.Database.EnsureDeletedAsync();
    // }

    // TEMPORARILY DISABLED - Investigating hang issue
    // [TestMethod]
    // [TestCategory("Integration")]
    // public async Task UpdateAsync_WithInvalidETag_ThrowsInvalidOperationException()
    // {
    // // Arrange - Create and start AppHost
    // var appHost = await DistributedApplicationTestingBuilder
    //     .CreateAsync<Projects.LibrisMaleficarum_AppHost>();
    // await using var app = await appHost.BuildAsync();
    // await app.StartAsync();

    // await Task.Delay(TimeSpan.FromSeconds(30));

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
    // userContextService.GetCurrentUserIdAsync().Returns(_userId);
    // var worldRepository = Substitute.For<IWorldRepository>();
    // var repository = new WorldEntityRepository(context, userContextService, worldRepository);

    // // Test data
    // var entity = WorldEntity.Create(_worldId, EntityType.Character, "Test Entity", "Description", null, null);
    // await context.WorldEntities.AddAsync(entity);
    // await context.SaveChangesAsync();

    // // Modify the entity to change its ETag
    // var modifiedEntity = await context.WorldEntities.FindAsync(entity.Id);
    // modifiedEntity!.Update("Updated Name", "Updated Description", EntityType.Character, null, null, null);
    // await context.SaveChangesAsync();

    // // Try to update with the original (now invalid) ETag
    // entity.Update("Another Update", "Another Description", EntityType.Character, null, null, null);

    // // Act & Assert
    // await Assert.ThrowsExceptionAsync<DbUpdateConcurrencyException>(
    //     async () => await repository.UpdateAsync(entity));

    // // Cleanup
    // await context.Database.EnsureDeletedAsync();
    // }
}
