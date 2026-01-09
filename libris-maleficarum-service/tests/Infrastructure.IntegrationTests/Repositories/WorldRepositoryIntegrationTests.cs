namespace LibrisMaleficarum.Infrastructure.Tests.Repositories;

using LibrisMaleficarum.Domain.Entities;
using LibrisMaleficarum.Domain.Exceptions;
using LibrisMaleficarum.Domain.Interfaces.Services;
using LibrisMaleficarum.Infrastructure.Persistence;
using LibrisMaleficarum.Infrastructure.Repositories;
using LibrisMaleficarum.IntegrationTests.Shared;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Integration tests for WorldRepository using Aspire-managed Cosmos DB Emulator.
/// These tests verify real Cosmos DB operations including CRUD, pagination, authorization, and soft delete.
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

    /// <summary>
    /// Creates a configured ApplicationDbContext for integration testing.
    /// Each test should use a unique database name to ensure isolation.
    /// </summary>
    private static ApplicationDbContext CreateDbContext(string databaseName)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseCosmos(
                AppHostFixture.CosmosDbAccountEndpoint,
                AppHostFixture.CosmosDbAccountKey,
                databaseName,
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

        return new ApplicationDbContext(options);
    }

    [TestMethod]
    public async Task CreateAsync_CreatesWorldWithCurrentUser()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var testDatabaseName = $"IntegrationTestDb_{Guid.NewGuid()}";

        await using var context = CreateDbContext(testDatabaseName);
        await context.Database.EnsureCreatedAsync();

        var userContextService = Substitute.For<IUserContextService>();
        userContextService.GetCurrentUserIdAsync().Returns(userId);
        var repository = new WorldRepository(context, userContextService);

        var worldToCreate = World.Create(Guid.NewGuid(), "Test World", "Test Description");

        // Act
        var createdWorld = await repository.CreateAsync(worldToCreate);

        // Assert
        createdWorld.Should().NotBeNull();
        createdWorld.Name.Should().Be("Test World");
        createdWorld.Description.Should().Be("Test Description");
        createdWorld.OwnerId.Should().Be(userId, "World should be owned by current user");
        createdWorld.IsDeleted.Should().BeFalse();

        // Cleanup
        await context.Database.EnsureDeletedAsync();
    }

    [TestMethod]
    public async Task GetByIdAsync_WithValidId_ReturnsWorld()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var testDatabaseName = $"IntegrationTestDb_{Guid.NewGuid()}";

        await using var context = CreateDbContext(testDatabaseName);
        await context.Database.EnsureCreatedAsync();

        var userContextService = Substitute.For<IUserContextService>();
        userContextService.GetCurrentUserIdAsync().Returns(userId);
        var repository = new WorldRepository(context, userContextService);

        var world = World.Create(userId, "Test World", "Test Description");
        await context.Worlds.AddAsync(world);
        await context.SaveChangesAsync();

        // Act
        var retrievedWorld = await repository.GetByIdAsync(world.Id);

        // Assert
        retrievedWorld.Should().NotBeNull();
        retrievedWorld!.Id.Should().Be(world.Id);
        retrievedWorld.Name.Should().Be("Test World");
        retrievedWorld.OwnerId.Should().Be(userId);

        // Cleanup
        await context.Database.EnsureDeletedAsync();
    }

    [TestMethod]
    public async Task GetByIdAsync_WithUnauthorizedUser_ThrowsUnauthorizedWorldAccessException()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var unauthorizedUserId = Guid.NewGuid();
        var testDatabaseName = $"IntegrationTestDb_{Guid.NewGuid()}";

        await using var context = CreateDbContext(testDatabaseName);
        await context.Database.EnsureCreatedAsync();

        var world = World.Create(ownerId, "Owner's World", null);
        await context.Worlds.AddAsync(world);
        await context.SaveChangesAsync();

        var userContextService = Substitute.For<IUserContextService>();
        userContextService.GetCurrentUserIdAsync().Returns(unauthorizedUserId);
        var repository = new WorldRepository(context, userContextService);

        // Act & Assert
        var act = async () => await repository.GetByIdAsync(world.Id);
        await act.Should().ThrowAsync<UnauthorizedWorldAccessException>()
            .WithMessage($"*{unauthorizedUserId}*{world.Id}*");

        // Cleanup
        await context.Database.EnsureDeletedAsync();
    }

    [TestMethod]
    public async Task GetByIdAsync_WithDeletedWorld_ReturnsNull()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var testDatabaseName = $"IntegrationTestDb_{Guid.NewGuid()}";

        await using var context = CreateDbContext(testDatabaseName);
        await context.Database.EnsureCreatedAsync();

        var userContextService = Substitute.For<IUserContextService>();
        userContextService.GetCurrentUserIdAsync().Returns(userId);
        var repository = new WorldRepository(context, userContextService);

        var world = World.Create(userId, "Test World", null);
        await context.Worlds.AddAsync(world);
        await context.SaveChangesAsync();

        world.SoftDelete();
        await context.SaveChangesAsync();

        // Act
        var retrievedWorld = await repository.GetByIdAsync(world.Id);

        // Assert
        retrievedWorld.Should().BeNull("Soft-deleted worlds should not be returned");

        // Cleanup
        await context.Database.EnsureDeletedAsync();
    }

    [TestMethod]
    public async Task GetAllByOwnerAsync_WithoutCursor_ReturnsAllWorlds()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var testDatabaseName = $"IntegrationTestDb_{Guid.NewGuid()}";

        await using var context = CreateDbContext(testDatabaseName);
        await context.Database.EnsureCreatedAsync();

        var userContextService = Substitute.For<IUserContextService>();
        userContextService.GetCurrentUserIdAsync().Returns(userId);
        var repository = new WorldRepository(context, userContextService);

        var world1 = World.Create(userId, "World 1", null);
        var world2 = World.Create(userId, "World 2", null);
        var world3 = World.Create(userId, "World 3", null);
        await context.Worlds.AddRangeAsync(world1, world2, world3);
        await context.SaveChangesAsync();

        // Act
        var (worlds, nextCursor) = await repository.GetAllByOwnerAsync(userId);

        // Assert
        worlds.Should().HaveCount(3);
        worlds.Should().ContainSingle(w => w.Name == "World 1");
        worlds.Should().ContainSingle(w => w.Name == "World 2");
        worlds.Should().ContainSingle(w => w.Name == "World 3");
        nextCursor.Should().BeNull("No more pages exist");

        // Cleanup
        await context.Database.EnsureDeletedAsync();
    }

    [TestMethod]
    public async Task GetAllByOwnerAsync_WithCursor_ReturnsNextPage()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var testDatabaseName = $"IntegrationTestDb_{Guid.NewGuid()}";

        await using var context = CreateDbContext(testDatabaseName);
        await context.Database.EnsureCreatedAsync();

        var userContextService = Substitute.For<IUserContextService>();
        userContextService.GetCurrentUserIdAsync().Returns(userId);
        var repository = new WorldRepository(context, userContextService);

        var firstWorld = World.Create(userId, "World 1", null);
        await context.Worlds.AddAsync(firstWorld);
        await context.SaveChangesAsync();
        await Task.Delay(100); // Ensure different timestamps

        var secondWorld = World.Create(userId, "World 2", null);
        await context.Worlds.AddAsync(secondWorld);
        await context.SaveChangesAsync();

        var cursor = firstWorld.CreatedDate.ToString("O");

        // Act
        var (worlds, nextCursor) = await repository.GetAllByOwnerAsync(userId, cursor: cursor);

        // Assert
        worlds.Should().HaveCount(1);
        worlds.First().Name.Should().Be("World 2");

        // Cleanup
        await context.Database.EnsureDeletedAsync();
    }

    [TestMethod]
    public async Task GetAllByOwnerAsync_WithLimit_ReturnsLimitedResults()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var testDatabaseName = $"IntegrationTestDb_{Guid.NewGuid()}";

        await using var context = CreateDbContext(testDatabaseName);
        await context.Database.EnsureCreatedAsync();

        var userContextService = Substitute.For<IUserContextService>();
        userContextService.GetCurrentUserIdAsync().Returns(userId);
        var repository = new WorldRepository(context, userContextService);

        // Create 5 worlds
        for (int i = 1; i <= 5; i++)
        {
            var world = World.Create(userId, $"World {i}", null);
            await context.Worlds.AddAsync(world);
            await context.SaveChangesAsync();
            if (i < 5) await Task.Delay(10); // Ensure different timestamps
        }

        // Act
        var (worlds, nextCursor) = await repository.GetAllByOwnerAsync(userId, limit: 3);

        // Assert
        worlds.Should().HaveCount(3, "Should respect the limit parameter");
        nextCursor.Should().NotBeNullOrEmpty("Should have a cursor for the next page");

        // Cleanup
        await context.Database.EnsureDeletedAsync();
    }

    [TestMethod]
    public async Task GetAllByOwnerAsync_ExcludesDeletedWorlds()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var testDatabaseName = $"IntegrationTestDb_{Guid.NewGuid()}";

        await using var context = CreateDbContext(testDatabaseName);
        await context.Database.EnsureCreatedAsync();

        var userContextService = Substitute.For<IUserContextService>();
        userContextService.GetCurrentUserIdAsync().Returns(userId);
        var repository = new WorldRepository(context, userContextService);

        var activeWorld = World.Create(userId, "Active World", null);
        var deletedWorld = World.Create(userId, "Deleted World", null);
        deletedWorld.SoftDelete();

        await context.Worlds.AddRangeAsync(activeWorld, deletedWorld);
        await context.SaveChangesAsync();

        // Act
        var (worlds, _) = await repository.GetAllByOwnerAsync(userId);

        // Assert
        worlds.Should().HaveCount(1);
        worlds.First().Name.Should().Be("Active World");

        // Cleanup
        await context.Database.EnsureDeletedAsync();
    }

    [TestMethod]
    public async Task UpdateAsync_WithValidData_UpdatesWorld()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var testDatabaseName = $"IntegrationTestDb_{Guid.NewGuid()}";

        await using var context = CreateDbContext(testDatabaseName);
        await context.Database.EnsureCreatedAsync();

        var userContextService = Substitute.For<IUserContextService>();
        userContextService.GetCurrentUserIdAsync().Returns(userId);
        var repository = new WorldRepository(context, userContextService);

        var world = World.Create(userId, "Original Name", "Original Description");
        await context.Worlds.AddAsync(world);
        await context.SaveChangesAsync();

        // Detach to simulate retrieval in a different context
        context.Entry(world).State = EntityState.Detached;

        // Modify the world
        world.Update("Updated Name", "Updated Description");

        // Act
        var updatedWorld = await repository.UpdateAsync(world);

        // Assert
        updatedWorld.Should().NotBeNull();
        updatedWorld.Name.Should().Be("Updated Name");
        updatedWorld.Description.Should().Be("Updated Description");
        updatedWorld.ModifiedDate.Should().BeAfter(updatedWorld.CreatedDate);

        // Cleanup
        await context.Database.EnsureDeletedAsync();
    }

    [TestMethod]
    public async Task UpdateAsync_WithUnauthorizedUser_ThrowsUnauthorizedWorldAccessException()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var unauthorizedUserId = Guid.NewGuid();
        var testDatabaseName = $"IntegrationTestDb_{Guid.NewGuid()}";

        await using var context = CreateDbContext(testDatabaseName);
        await context.Database.EnsureCreatedAsync();

        var world = World.Create(ownerId, "Owner's World", null);
        await context.Worlds.AddAsync(world);
        await context.SaveChangesAsync();

        var userContextService = Substitute.For<IUserContextService>();
        userContextService.GetCurrentUserIdAsync().Returns(unauthorizedUserId);
        var repository = new WorldRepository(context, userContextService);

        // Detach to simulate retrieval in a different context
        context.Entry(world).State = EntityState.Detached;
        world.Update("Hacked Name", "Hacked Description");

        // Act & Assert
        var act = async () => await repository.UpdateAsync(world);
        await act.Should().ThrowAsync<UnauthorizedWorldAccessException>()
            .WithMessage($"*{unauthorizedUserId}*{world.Id}*");

        // Cleanup
        await context.Database.EnsureDeletedAsync();
    }

    [TestMethod]
    public async Task DeleteAsync_WithValidId_SoftDeletesWorld()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var testDatabaseName = $"IntegrationTestDb_{Guid.NewGuid()}";

        await using var context = CreateDbContext(testDatabaseName);
        await context.Database.EnsureCreatedAsync();

        var userContextService = Substitute.For<IUserContextService>();
        userContextService.GetCurrentUserIdAsync().Returns(userId);
        var repository = new WorldRepository(context, userContextService);

        var world = World.Create(userId, "Test World", null);
        await context.Worlds.AddAsync(world);
        await context.SaveChangesAsync();

        // Act
        await repository.DeleteAsync(world.Id);

        // Assert
        var deletedWorld = await context.Worlds.FirstOrDefaultAsync(w => w.Id == world.Id);
        deletedWorld.Should().NotBeNull("World should still exist in database");
        deletedWorld!.IsDeleted.Should().BeTrue("World should be soft-deleted");
        deletedWorld.ModifiedDate.Should().BeAfter(deletedWorld.CreatedDate, "ModifiedDate should be updated on delete");

        // Cleanup
        await context.Database.EnsureDeletedAsync();
    }

    [TestMethod]
    public async Task DeleteAsync_WithUnauthorizedUser_ThrowsUnauthorizedWorldAccessException()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var unauthorizedUserId = Guid.NewGuid();
        var testDatabaseName = $"IntegrationTestDb_{Guid.NewGuid()}";

        await using var context = CreateDbContext(testDatabaseName);
        await context.Database.EnsureCreatedAsync();

        var world = World.Create(ownerId, "Owner's World", null);
        await context.Worlds.AddAsync(world);
        await context.SaveChangesAsync();

        var userContextService = Substitute.For<IUserContextService>();
        userContextService.GetCurrentUserIdAsync().Returns(unauthorizedUserId);
        var repository = new WorldRepository(context, userContextService);

        // Act & Assert
        var act = async () => await repository.DeleteAsync(world.Id);
        await act.Should().ThrowAsync<UnauthorizedWorldAccessException>()
            .WithMessage($"*{unauthorizedUserId}*{world.Id}*");

        // Cleanup
        await context.Database.EnsureDeletedAsync();
    }

    [TestMethod]
    public async Task DeleteAsync_WithNonExistentWorld_ThrowsWorldNotFoundException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var nonExistentWorldId = Guid.NewGuid();
        var testDatabaseName = $"IntegrationTestDb_{Guid.NewGuid()}";

        await using var context = CreateDbContext(testDatabaseName);
        await context.Database.EnsureCreatedAsync();

        var userContextService = Substitute.For<IUserContextService>();
        userContextService.GetCurrentUserIdAsync().Returns(userId);
        var repository = new WorldRepository(context, userContextService);

        // Act & Assert
        var act = async () => await repository.DeleteAsync(nonExistentWorldId);
        await act.Should().ThrowAsync<WorldNotFoundException>()
            .WithMessage($"*{nonExistentWorldId}*");

        // Cleanup
        await context.Database.EnsureDeletedAsync();
    }

    // TODO: Add test for optimistic concurrency - requires proper ETag implementation in entity model
    // The _etag property is managed by Cosmos DB but not exposed in the entity model
    // Need to add ETag property to World entity and configure it as a concurrency token
}
