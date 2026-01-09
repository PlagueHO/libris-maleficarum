namespace LibrisMaleficarum.Infrastructure.Tests.Repositories;

using LibrisMaleficarum.Domain.Entities;
using LibrisMaleficarum.Domain.Exceptions;
using LibrisMaleficarum.Domain.Interfaces.Repositories;
using LibrisMaleficarum.Domain.Interfaces.Services;
using LibrisMaleficarum.Domain.ValueObjects;
using LibrisMaleficarum.Infrastructure.Persistence;
using LibrisMaleficarum.Infrastructure.Repositories;
using LibrisMaleficarum.IntegrationTests.Shared;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Integration tests for WorldEntityRepository using Aspire-managed Cosmos DB Emulator.
/// These tests verify real Cosmos DB operations including CRUD, hierarchy navigation, filtering, pagination, and authorization.
/// Uses shared AppHostFixture from IntegrationTests.Shared project.
/// </summary>
[TestClass]
[TestCategory("Integration")]
[TestCategory("RequiresDocker")]
[DoNotParallelize] // AppHost tests must run sequentially to avoid port conflicts
public class WorldEntityRepositoryIntegrationTests
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
    public async Task CreateAsync_CreatesEntityWithCurrentUser()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var worldId = Guid.NewGuid();
        var testDatabaseName = $"IntegrationTestDb_{Guid.NewGuid()}";

        await using var context = CreateDbContext(testDatabaseName);
        await context.Database.EnsureCreatedAsync();

        var userContextService = Substitute.For<IUserContextService>();
        userContextService.GetCurrentUserIdAsync().Returns(userId);

        // Create world first
        var world = World.Create(userId, "Test World", null);
        await context.Worlds.AddAsync(world);
        await context.SaveChangesAsync();

        var worldRepository = Substitute.For<IWorldRepository>();
        worldRepository.GetByIdAsync(world.Id, Arg.Any<CancellationToken>()).Returns(world);

        var repository = new WorldEntityRepository(context, userContextService, worldRepository);

        var entityToCreate = WorldEntity.Create(world.Id, EntityType.Character, "Test Character", "Description", null, new List<string> { "hero" });

        // Act
        var createdEntity = await repository.CreateAsync(entityToCreate);

        // Assert
        createdEntity.Should().NotBeNull();
        createdEntity.Name.Should().Be("Test Character");
        createdEntity.Description.Should().Be("Description");
        createdEntity.EntityType.Should().Be(EntityType.Character);
        createdEntity.WorldId.Should().Be(world.Id);
        createdEntity.Tags.Should().Contain("hero");
        createdEntity.IsDeleted.Should().BeFalse();

        // Cleanup
        await context.Database.EnsureDeletedAsync();
    }

    [TestMethod]
    public async Task GetByIdAsync_WithValidId_ReturnsEntity()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var testDatabaseName = $"IntegrationTestDb_{Guid.NewGuid()}";

        await using var context = CreateDbContext(testDatabaseName);
        await context.Database.EnsureCreatedAsync();

        var userContextService = Substitute.For<IUserContextService>();
        userContextService.GetCurrentUserIdAsync().Returns(userId);

        // Create world
        var world = World.Create(userId, "Test World", null);
        await context.Worlds.AddAsync(world);
        await context.SaveChangesAsync();

        var worldRepository = Substitute.For<IWorldRepository>();
        worldRepository.GetByIdAsync(world.Id, Arg.Any<CancellationToken>()).Returns(world);

        var repository = new WorldEntityRepository(context, userContextService, worldRepository);

        var entity = WorldEntity.Create(world.Id, EntityType.Location, "Test Location", null, null, null);
        await context.WorldEntities.AddAsync(entity);
        await context.SaveChangesAsync();

        // Act
        var retrievedEntity = await repository.GetByIdAsync(world.Id, entity.Id);

        // Assert
        retrievedEntity.Should().NotBeNull();
        retrievedEntity!.Id.Should().Be(entity.Id);
        retrievedEntity.Name.Should().Be("Test Location");
        retrievedEntity.EntityType.Should().Be(EntityType.Location);

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

        // Create world owned by different user
        var world = World.Create(ownerId, "Owner's World", null);
        await context.Worlds.AddAsync(world);
        await context.SaveChangesAsync();

        var entity = WorldEntity.Create(world.Id, EntityType.Character, "Test Character", null, null, null);
        await context.WorldEntities.AddAsync(entity);
        await context.SaveChangesAsync();

        var userContextService = Substitute.For<IUserContextService>();
        userContextService.GetCurrentUserIdAsync().Returns(unauthorizedUserId);

        var worldRepository = Substitute.For<IWorldRepository>();
        worldRepository.GetByIdAsync(world.Id, Arg.Any<CancellationToken>()).Returns(world);

        var repository = new WorldEntityRepository(context, userContextService, worldRepository);

        // Act & Assert
        var act = async () => await repository.GetByIdAsync(world.Id, entity.Id);
        await act.Should().ThrowAsync<UnauthorizedWorldAccessException>()
            .WithMessage($"*{unauthorizedUserId}*{world.Id}*");

        // Cleanup
        await context.Database.EnsureDeletedAsync();
    }

    [TestMethod]
    public async Task GetByIdAsync_WithDeletedEntity_ReturnsNull()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var testDatabaseName = $"IntegrationTestDb_{Guid.NewGuid()}";

        await using var context = CreateDbContext(testDatabaseName);
        await context.Database.EnsureCreatedAsync();

        var userContextService = Substitute.For<IUserContextService>();
        userContextService.GetCurrentUserIdAsync().Returns(userId);

        // Create world
        var world = World.Create(userId, "Test World", null);
        await context.Worlds.AddAsync(world);
        await context.SaveChangesAsync();

        var worldRepository = Substitute.For<IWorldRepository>();
        worldRepository.GetByIdAsync(world.Id, Arg.Any<CancellationToken>()).Returns(world);

        var repository = new WorldEntityRepository(context, userContextService, worldRepository);

        var entity = WorldEntity.Create(world.Id, EntityType.Character, "Test Character", null, null, null);
        await context.WorldEntities.AddAsync(entity);
        await context.SaveChangesAsync();

        entity.SoftDelete();
        await context.SaveChangesAsync();

        // Act
        var retrievedEntity = await repository.GetByIdAsync(world.Id, entity.Id);

        // Assert
        retrievedEntity.Should().BeNull("Soft-deleted entities should not be returned");

        // Cleanup
        await context.Database.EnsureDeletedAsync();
    }

    [TestMethod]
    public async Task GetAllByWorldAsync_WithoutFilters_ReturnsAllEntities()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var testDatabaseName = $"IntegrationTestDb_{Guid.NewGuid()}";

        await using var context = CreateDbContext(testDatabaseName);
        await context.Database.EnsureCreatedAsync();

        var userContextService = Substitute.For<IUserContextService>();
        userContextService.GetCurrentUserIdAsync().Returns(userId);

        // Create world
        var world = World.Create(userId, "Test World", null);
        await context.Worlds.AddAsync(world);
        await context.SaveChangesAsync();

        var worldRepository = Substitute.For<IWorldRepository>();
        worldRepository.GetByIdAsync(world.Id, Arg.Any<CancellationToken>()).Returns(world);

        var repository = new WorldEntityRepository(context, userContextService, worldRepository);

        var entity1 = WorldEntity.Create(world.Id, EntityType.Character, "Character 1", null, null, null);
        var entity2 = WorldEntity.Create(world.Id, EntityType.Location, "Location 1", null, null, null);
        var entity3 = WorldEntity.Create(world.Id, EntityType.Item, "Item 1", null, null, null);
        await context.WorldEntities.AddRangeAsync(entity1, entity2, entity3);
        await context.SaveChangesAsync();

        // Act
        var (entities, nextCursor) = await repository.GetAllByWorldAsync(world.Id);

        // Assert
        entities.Should().HaveCount(3);
        entities.Should().ContainSingle(e => e.Name == "Character 1");
        entities.Should().ContainSingle(e => e.Name == "Location 1");
        entities.Should().ContainSingle(e => e.Name == "Item 1");
        nextCursor.Should().BeNull("No more pages exist");

        // Cleanup
        await context.Database.EnsureDeletedAsync();
    }

    [TestMethod]
    public async Task GetAllByWorldAsync_WithEntityTypeFilter_ReturnsMatchingEntities()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var testDatabaseName = $"IntegrationTestDb_{Guid.NewGuid()}";

        await using var context = CreateDbContext(testDatabaseName);
        await context.Database.EnsureCreatedAsync();

        var userContextService = Substitute.For<IUserContextService>();
        userContextService.GetCurrentUserIdAsync().Returns(userId);

        // Create world
        var world = World.Create(userId, "Test World", null);
        await context.Worlds.AddAsync(world);
        await context.SaveChangesAsync();

        var worldRepository = Substitute.For<IWorldRepository>();
        worldRepository.GetByIdAsync(world.Id, Arg.Any<CancellationToken>()).Returns(world);

        var repository = new WorldEntityRepository(context, userContextService, worldRepository);

        var entity1 = WorldEntity.Create(world.Id, EntityType.Character, "Character 1", null, null, null);
        var entity2 = WorldEntity.Create(world.Id, EntityType.Character, "Character 2", null, null, null);
        var entity3 = WorldEntity.Create(world.Id, EntityType.Location, "Location 1", null, null, null);
        await context.WorldEntities.AddRangeAsync(entity1, entity2, entity3);
        await context.SaveChangesAsync();

        // Act
        var (entities, _) = await repository.GetAllByWorldAsync(world.Id, entityType: EntityType.Character);

        // Assert
        entities.Should().HaveCount(2);
        entities.All(e => e.EntityType == EntityType.Character).Should().BeTrue();

        // Cleanup
        await context.Database.EnsureDeletedAsync();
    }

    [TestMethod]
    public async Task GetAllByWorldAsync_WithTagsFilter_ReturnsMatchingEntities()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var testDatabaseName = $"IntegrationTestDb_{Guid.NewGuid()}";

        await using var context = CreateDbContext(testDatabaseName);
        await context.Database.EnsureCreatedAsync();

        var userContextService = Substitute.For<IUserContextService>();
        userContextService.GetCurrentUserIdAsync().Returns(userId);

        // Create world
        var world = World.Create(userId, "Test World", null);
        await context.Worlds.AddAsync(world);
        await context.SaveChangesAsync();

        var worldRepository = Substitute.For<IWorldRepository>();
        worldRepository.GetByIdAsync(world.Id, Arg.Any<CancellationToken>()).Returns(world);

        var repository = new WorldEntityRepository(context, userContextService, worldRepository);

        var entity1 = WorldEntity.Create(world.Id, EntityType.Character, "Hero 1", null, null, new List<string> { "hero", "warrior" });
        var entity2 = WorldEntity.Create(world.Id, EntityType.Character, "Villain 1", null, null, new List<string> { "villain", "mage" });
        var entity3 = WorldEntity.Create(world.Id, EntityType.Character, "Hero 2", null, null, new List<string> { "hero", "mage" });
        await context.WorldEntities.AddRangeAsync(entity1, entity2, entity3);
        await context.SaveChangesAsync();

        // Act
        var (entities, _) = await repository.GetAllByWorldAsync(world.Id, tags: new List<string> { "hero" });

        // Assert
        entities.Should().HaveCount(2);
        entities.All(e => e.Tags.Any(t => t.ToLower().Contains("hero"))).Should().BeTrue();

        // Cleanup
        await context.Database.EnsureDeletedAsync();
    }

    [TestMethod]
    public async Task GetAllByWorldAsync_WithCursor_ReturnsNextPage()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var testDatabaseName = $"IntegrationTestDb_{Guid.NewGuid()}";

        await using var context = CreateDbContext(testDatabaseName);
        await context.Database.EnsureCreatedAsync();

        var userContextService = Substitute.For<IUserContextService>();
        userContextService.GetCurrentUserIdAsync().Returns(userId);

        // Create world
        var world = World.Create(userId, "Test World", null);
        await context.Worlds.AddAsync(world);
        await context.SaveChangesAsync();

        var worldRepository = Substitute.For<IWorldRepository>();
        worldRepository.GetByIdAsync(world.Id, Arg.Any<CancellationToken>()).Returns(world);

        var repository = new WorldEntityRepository(context, userContextService, worldRepository);

        var firstEntity = WorldEntity.Create(world.Id, EntityType.Location, "Entity 1", null, null, null);
        await context.WorldEntities.AddAsync(firstEntity);
        await context.SaveChangesAsync();
        
        // Ensure different timestamps - use longer delay for Cosmos DB
        await Task.Delay(1000);

        var secondEntity = WorldEntity.Create(world.Id, EntityType.Location, "Entity 2", null, null, null);
        await context.WorldEntities.AddAsync(secondEntity);
        await context.SaveChangesAsync();

        var cursor = firstEntity.CreatedDate.ToString("O");

        // Act
        var (entities, nextCursor) = await repository.GetAllByWorldAsync(world.Id, cursor: cursor);

        // Assert
        entities.Should().HaveCount(1, "Only entities created after the cursor should be returned");
        entities.First().Name.Should().Be("Entity 2");

        // Cleanup
        await context.Database.EnsureDeletedAsync();
    }

    [TestMethod]
    public async Task GetAllByWorldAsync_WithLimit_ReturnsLimitedResults()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var testDatabaseName = $"IntegrationTestDb_{Guid.NewGuid()}";

        await using var context = CreateDbContext(testDatabaseName);
        await context.Database.EnsureCreatedAsync();

        var userContextService = Substitute.For<IUserContextService>();
        userContextService.GetCurrentUserIdAsync().Returns(userId);

        // Create world
        var world = World.Create(userId, "Test World", null);
        await context.Worlds.AddAsync(world);
        await context.SaveChangesAsync();

        var worldRepository = Substitute.For<IWorldRepository>();
        worldRepository.GetByIdAsync(world.Id, Arg.Any<CancellationToken>()).Returns(world);

        var repository = new WorldEntityRepository(context, userContextService, worldRepository);

        // Create 5 entities
        for (int i = 1; i <= 5; i++)
        {
            var entity = WorldEntity.Create(world.Id, EntityType.Character, $"Character {i}", null, null, null);
            await context.WorldEntities.AddAsync(entity);
            await context.SaveChangesAsync();
            if (i < 5) await Task.Delay(10); // Ensure different timestamps
        }

        // Act
        var (entities, nextCursor) = await repository.GetAllByWorldAsync(world.Id, limit: 3);

        // Assert
        entities.Should().HaveCount(3, "Should respect the limit parameter");
        nextCursor.Should().NotBeNullOrEmpty("Should have a cursor for the next page");

        // Cleanup
        await context.Database.EnsureDeletedAsync();
    }

    [TestMethod]
    public async Task GetAllByWorldAsync_ExcludesDeletedEntities()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var testDatabaseName = $"IntegrationTestDb_{Guid.NewGuid()}";

        await using var context = CreateDbContext(testDatabaseName);
        await context.Database.EnsureCreatedAsync();

        var userContextService = Substitute.For<IUserContextService>();
        userContextService.GetCurrentUserIdAsync().Returns(userId);

        // Create world
        var world = World.Create(userId, "Test World", null);
        await context.Worlds.AddAsync(world);
        await context.SaveChangesAsync();

        var worldRepository = Substitute.For<IWorldRepository>();
        worldRepository.GetByIdAsync(world.Id, Arg.Any<CancellationToken>()).Returns(world);

        var repository = new WorldEntityRepository(context, userContextService, worldRepository);

        var activeEntity = WorldEntity.Create(world.Id, EntityType.Character, "Active Character", null, null, null);
        var deletedEntity = WorldEntity.Create(world.Id, EntityType.Character, "Deleted Character", null, null, null);
        deletedEntity.SoftDelete();

        await context.WorldEntities.AddRangeAsync(activeEntity, deletedEntity);
        await context.SaveChangesAsync();

        // Act
        var (entities, _) = await repository.GetAllByWorldAsync(world.Id);

        // Assert
        entities.Should().HaveCount(1);
        entities.First().Name.Should().Be("Active Character");

        // Cleanup
        await context.Database.EnsureDeletedAsync();
    }

    [TestMethod]
    public async Task GetChildrenAsync_ReturnsChildEntities()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var testDatabaseName = $"IntegrationTestDb_{Guid.NewGuid()}";

        await using var context = CreateDbContext(testDatabaseName);
        await context.Database.EnsureCreatedAsync();

        var userContextService = Substitute.For<IUserContextService>();
        userContextService.GetCurrentUserIdAsync().Returns(userId);

        // Create world
        var world = World.Create(userId, "Test World", null);
        await context.Worlds.AddAsync(world);
        await context.SaveChangesAsync();

        var worldRepository = Substitute.For<IWorldRepository>();
        worldRepository.GetByIdAsync(world.Id, Arg.Any<CancellationToken>()).Returns(world);

        var repository = new WorldEntityRepository(context, userContextService, worldRepository);

        // Create parent and children
        var parent = WorldEntity.Create(world.Id, EntityType.Location, "Continent", null, null, null);
        await context.WorldEntities.AddAsync(parent);
        await context.SaveChangesAsync();

        var child1 = WorldEntity.Create(world.Id, EntityType.Location, "Country 1", null, parent.Id, null);
        var child2 = WorldEntity.Create(world.Id, EntityType.Location, "Country 2", null, parent.Id, null);
        var child3 = WorldEntity.Create(world.Id, EntityType.Location, "Country 3", null, parent.Id, null);
        await context.WorldEntities.AddRangeAsync(child1, child2, child3);
        await context.SaveChangesAsync();

        // Act
        var children = await repository.GetChildrenAsync(world.Id, parent.Id);

        // Assert
        children.Should().HaveCount(3);
        children.All(c => c.ParentId == parent.Id).Should().BeTrue();

        // Cleanup
        await context.Database.EnsureDeletedAsync();
    }

    [TestMethod]
    public async Task GetChildrenAsync_ExcludesDeletedChildren()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var testDatabaseName = $"IntegrationTestDb_{Guid.NewGuid()}";

        await using var context = CreateDbContext(testDatabaseName);
        await context.Database.EnsureCreatedAsync();

        var userContextService = Substitute.For<IUserContextService>();
        userContextService.GetCurrentUserIdAsync().Returns(userId);

        // Create world
        var world = World.Create(userId, "Test World", null);
        await context.Worlds.AddAsync(world);
        await context.SaveChangesAsync();

        var worldRepository = Substitute.For<IWorldRepository>();
        worldRepository.GetByIdAsync(world.Id, Arg.Any<CancellationToken>()).Returns(world);

        var repository = new WorldEntityRepository(context, userContextService, worldRepository);

        // Create parent and children
        var parent = WorldEntity.Create(world.Id, EntityType.Location, "Parent", null, null, null);
        await context.WorldEntities.AddAsync(parent);
        await context.SaveChangesAsync();

        var activeChild = WorldEntity.Create(world.Id, EntityType.Location, "Active Child", null, parent.Id, null);
        var deletedChild = WorldEntity.Create(world.Id, EntityType.Location, "Deleted Child", null, parent.Id, null);
        deletedChild.SoftDelete();
        await context.WorldEntities.AddRangeAsync(activeChild, deletedChild);
        await context.SaveChangesAsync();

        // Act
        var children = await repository.GetChildrenAsync(world.Id, parent.Id);

        // Assert
        children.Should().HaveCount(1);
        children.First().Name.Should().Be("Active Child");

        // Cleanup
        await context.Database.EnsureDeletedAsync();
    }

    [TestMethod]
    public async Task UpdateAsync_WithValidData_UpdatesEntity()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var testDatabaseName = $"IntegrationTestDb_{Guid.NewGuid()}";

        await using var context = CreateDbContext(testDatabaseName);
        await context.Database.EnsureCreatedAsync();

        var userContextService = Substitute.For<IUserContextService>();
        userContextService.GetCurrentUserIdAsync().Returns(userId);

        // Create world
        var world = World.Create(userId, "Test World", null);
        await context.Worlds.AddAsync(world);
        await context.SaveChangesAsync();

        var worldRepository = Substitute.For<IWorldRepository>();
        worldRepository.GetByIdAsync(world.Id, Arg.Any<CancellationToken>()).Returns(world);

        var repository = new WorldEntityRepository(context, userContextService, worldRepository);

        var entity = WorldEntity.Create(world.Id, EntityType.Character, "Original Name", "Original Description", null, null);
        await context.WorldEntities.AddAsync(entity);
        await context.SaveChangesAsync();

        // Detach to simulate retrieval in a different context
        context.Entry(entity).State = EntityState.Detached;

        // Modify the entity
        entity.Update("Updated Name", "Updated Description", EntityType.Character, null, null, null);

        // Act
        var updatedEntity = await repository.UpdateAsync(entity);

        // Assert
        updatedEntity.Should().NotBeNull();
        updatedEntity.Name.Should().Be("Updated Name");
        updatedEntity.Description.Should().Be("Updated Description");
        updatedEntity.ModifiedDate.Should().BeAfter(updatedEntity.CreatedDate);

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

        // Create world owned by different user
        var world = World.Create(ownerId, "Owner's World", null);
        await context.Worlds.AddAsync(world);
        await context.SaveChangesAsync();

        var entity = WorldEntity.Create(world.Id, EntityType.Character, "Test Character", null, null, null);
        await context.WorldEntities.AddAsync(entity);
        await context.SaveChangesAsync();

        var userContextService = Substitute.For<IUserContextService>();
        userContextService.GetCurrentUserIdAsync().Returns(unauthorizedUserId);

        var worldRepository = Substitute.For<IWorldRepository>();
        worldRepository.GetByIdAsync(world.Id, Arg.Any<CancellationToken>()).Returns(world);

        var repository = new WorldEntityRepository(context, userContextService, worldRepository);

        // Detach and modify
        context.Entry(entity).State = EntityState.Detached;
        entity.Update("Updated Name", null, EntityType.Character, null, null, null);

        // Act & Assert
        var act = async () => await repository.UpdateAsync(entity);
        await act.Should().ThrowAsync<UnauthorizedWorldAccessException>()
            .WithMessage($"*{unauthorizedUserId}*{world.Id}*");

        // Cleanup
        await context.Database.EnsureDeletedAsync();
    }

    [TestMethod]
    public async Task DeleteAsync_WithValidId_SoftDeletesEntity()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var testDatabaseName = $"IntegrationTestDb_{Guid.NewGuid()}";

        await using var context = CreateDbContext(testDatabaseName);
        await context.Database.EnsureCreatedAsync();

        var userContextService = Substitute.For<IUserContextService>();
        userContextService.GetCurrentUserIdAsync().Returns(userId);

        // Create world
        var world = World.Create(userId, "Test World", null);
        await context.Worlds.AddAsync(world);
        await context.SaveChangesAsync();

        var worldRepository = Substitute.For<IWorldRepository>();
        worldRepository.GetByIdAsync(world.Id, Arg.Any<CancellationToken>()).Returns(world);

        var repository = new WorldEntityRepository(context, userContextService, worldRepository);

        var entity = WorldEntity.Create(world.Id, EntityType.Character, "Test Character", null, null, null);
        await context.WorldEntities.AddAsync(entity);
        await context.SaveChangesAsync();

        // Act
        await repository.DeleteAsync(world.Id, entity.Id);

        // Assert
        var deletedEntity = await context.WorldEntities.FindAsync(entity.Id);
        deletedEntity.Should().NotBeNull();
        deletedEntity!.IsDeleted.Should().BeTrue();

        // Cleanup
        await context.Database.EnsureDeletedAsync();
    }

    [TestMethod]
    public async Task DeleteAsync_WithCascade_DeletesChildrenRecursively()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var testDatabaseName = $"IntegrationTestDb_{Guid.NewGuid()}";

        await using var context = CreateDbContext(testDatabaseName);
        await context.Database.EnsureCreatedAsync();

        var userContextService = Substitute.For<IUserContextService>();
        userContextService.GetCurrentUserIdAsync().Returns(userId);

        // Create world
        var world = World.Create(userId, "Test World", null);
        await context.Worlds.AddAsync(world);
        await context.SaveChangesAsync();

        var worldRepository = Substitute.For<IWorldRepository>();
        worldRepository.GetByIdAsync(world.Id, Arg.Any<CancellationToken>()).Returns(world);

        var repository = new WorldEntityRepository(context, userContextService, worldRepository);

        // Create hierarchy: parent -> child -> grandchild
        var parent = WorldEntity.Create(world.Id, EntityType.Location, "Parent", null, null, null);
        await context.WorldEntities.AddAsync(parent);
        await context.SaveChangesAsync();

        var child = WorldEntity.Create(world.Id, EntityType.Location, "Child", null, parent.Id, null);
        await context.WorldEntities.AddAsync(child);
        await context.SaveChangesAsync();

        var grandchild = WorldEntity.Create(world.Id, EntityType.Location, "Grandchild", null, child.Id, null);
        await context.WorldEntities.AddAsync(grandchild);
        await context.SaveChangesAsync();

        // Act
        await repository.DeleteAsync(world.Id, parent.Id, cascade: true);

        // Assert
        var deletedParent = await context.WorldEntities.FindAsync(parent.Id);
        var deletedChild = await context.WorldEntities.FindAsync(child.Id);
        var deletedGrandchild = await context.WorldEntities.FindAsync(grandchild.Id);

        deletedParent!.IsDeleted.Should().BeTrue();
        deletedChild!.IsDeleted.Should().BeTrue();
        deletedGrandchild!.IsDeleted.Should().BeTrue();

        // Cleanup
        await context.Database.EnsureDeletedAsync();
    }

    [TestMethod]
    public async Task DeleteAsync_WithoutCascade_ThrowsWhenChildrenExist()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var testDatabaseName = $"IntegrationTestDb_{Guid.NewGuid()}";

        await using var context = CreateDbContext(testDatabaseName);
        await context.Database.EnsureCreatedAsync();

        var userContextService = Substitute.For<IUserContextService>();
        userContextService.GetCurrentUserIdAsync().Returns(userId);

        // Create world
        var world = World.Create(userId, "Test World", null);
        await context.Worlds.AddAsync(world);
        await context.SaveChangesAsync();

        var worldRepository = Substitute.For<IWorldRepository>();
        worldRepository.GetByIdAsync(world.Id, Arg.Any<CancellationToken>()).Returns(world);

        var repository = new WorldEntityRepository(context, userContextService, worldRepository);

        // Create parent with child
        var parent = WorldEntity.Create(world.Id, EntityType.Location, "Parent", null, null, null);
        await context.WorldEntities.AddAsync(parent);
        await context.SaveChangesAsync();

        var child = WorldEntity.Create(world.Id, EntityType.Location, "Child", null, parent.Id, null);
        await context.WorldEntities.AddAsync(child);
        await context.SaveChangesAsync();

        // Act & Assert
        var act = async () => await repository.DeleteAsync(world.Id, parent.Id, cascade: false);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage($"*{parent.Id}*child entities*cascade=true*");

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

        // Create world owned by different user
        var world = World.Create(ownerId, "Owner's World", null);
        await context.Worlds.AddAsync(world);
        await context.SaveChangesAsync();

        var entity = WorldEntity.Create(world.Id, EntityType.Character, "Test Character", null, null, null);
        await context.WorldEntities.AddAsync(entity);
        await context.SaveChangesAsync();

        var userContextService = Substitute.For<IUserContextService>();
        userContextService.GetCurrentUserIdAsync().Returns(unauthorizedUserId);

        var worldRepository = Substitute.For<IWorldRepository>();
        worldRepository.GetByIdAsync(world.Id, Arg.Any<CancellationToken>()).Returns(world);

        var repository = new WorldEntityRepository(context, userContextService, worldRepository);

        // Act & Assert
        var act = async () => await repository.DeleteAsync(world.Id, entity.Id);
        await act.Should().ThrowAsync<UnauthorizedWorldAccessException>()
            .WithMessage($"*{unauthorizedUserId}*{world.Id}*");

        // Cleanup
        await context.Database.EnsureDeletedAsync();
    }

    [TestMethod]
    public async Task DeleteAsync_WithNonExistentEntity_ThrowsEntityNotFoundException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var testDatabaseName = $"IntegrationTestDb_{Guid.NewGuid()}";

        await using var context = CreateDbContext(testDatabaseName);
        await context.Database.EnsureCreatedAsync();

        var userContextService = Substitute.For<IUserContextService>();
        userContextService.GetCurrentUserIdAsync().Returns(userId);

        // Create world
        var world = World.Create(userId, "Test World", null);
        await context.Worlds.AddAsync(world);
        await context.SaveChangesAsync();

        var worldRepository = Substitute.For<IWorldRepository>();
        worldRepository.GetByIdAsync(world.Id, Arg.Any<CancellationToken>()).Returns(world);

        var repository = new WorldEntityRepository(context, userContextService, worldRepository);

        var nonExistentEntityId = Guid.NewGuid();

        // Act & Assert
        var act = async () => await repository.DeleteAsync(world.Id, nonExistentEntityId);
        await act.Should().ThrowAsync<EntityNotFoundException>()
            .WithMessage($"*{nonExistentEntityId}*{world.Id}*");

        // Cleanup
        await context.Database.EnsureDeletedAsync();
    }

    // TODO: Add test for optimistic concurrency with ETag validation
    // TODO: Add test for circular reference prevention when updating ParentId
}
