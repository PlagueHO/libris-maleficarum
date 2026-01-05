namespace LibrisMaleficarum.Infrastructure.Tests.Repositories;

using FluentAssertions;
using LibrisMaleficarum.Domain.Entities;
using LibrisMaleficarum.Domain.Exceptions;
using LibrisMaleficarum.Domain.Interfaces.Repositories;
using LibrisMaleficarum.Domain.Interfaces.Services;
using LibrisMaleficarum.Domain.ValueObjects;
using LibrisMaleficarum.Infrastructure.Persistence;
using LibrisMaleficarum.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using NSubstitute;

/// <summary>
/// Unit tests for WorldEntityRepository.
/// Tests CRUD operations, hierarchy management, authorization, and pagination.
/// </summary>
[TestClass]
public class WorldEntityRepositoryTests
{
    private const string COSMOS_EMULATOR_DOCKER_COMMAND =
        "docker run -d -p 57790:8081 -p 57789:1234 --name cosmosdb-emulator " +
        "mcr.microsoft.com/cosmosdb/linux/azure-cosmos-emulator:vnext-preview";

    private ApplicationDbContext _context = null!;
    private IUserContextService _userContextService = null!;
    private IWorldRepository _worldRepository = null!;
    private WorldEntityRepository _repository = null!;
    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _otherUserId = Guid.NewGuid();
    private readonly Guid _worldId = Guid.NewGuid();
    private readonly string _testDatabaseName = $"TestDb_{Guid.NewGuid()}";

    [TestInitialize]
    public void Setup()
    {
        var options = GetDbContextOptions();
        _context = new ApplicationDbContext(options);
        _userContextService = Substitute.For<IUserContextService>();
        _worldRepository = Substitute.For<IWorldRepository>();

        _userContextService.GetCurrentUserIdAsync().Returns(_userId);
        ConfigureAuthorizedWorld();

        _repository = new WorldEntityRepository(_context, _userContextService, _worldRepository);
    }

    private DbContextOptions<ApplicationDbContext> GetDbContextOptions()
    {
        // Check if Cosmos DB connection string is available (for integration tests)
        var cosmosConnectionString = Environment.GetEnvironmentVariable("COSMOS_CONNECTION_STRING");
        if (!string.IsNullOrEmpty(cosmosConnectionString))
        {
            var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
            optionsBuilder.UseCosmos(
                cosmosConnectionString,
                _testDatabaseName,
                cosmosOptions =>
                {
                    // Configure HttpClientFactory to skip SSL validation for local emulator
                    cosmosOptions.HttpClientFactory(() =>
                    {
                        var handler = new HttpClientHandler
                        {
                            ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
                        };
                        return new HttpClient(handler);
                    });
                });
            return optionsBuilder.Options;
        }

        // Default to InMemory for unit tests
        return new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: _testDatabaseName)
            .Options;
    }

    /// <summary>
    /// Checks if Cosmos DB Emulator is available for integration tests.
    /// If not available, marks the test as inconclusive (skipped).
    /// </summary>
    private void RequireCosmosDbEmulator()
    {
        var cosmosConnectionString = Environment.GetEnvironmentVariable("COSMOS_CONNECTION_STRING");

        if (string.IsNullOrEmpty(cosmosConnectionString))
        {
            Assert.Inconclusive(
                "Cosmos DB Emulator is not configured. Set COSMOS_CONNECTION_STRING environment variable.\n" +
                "Example: $env:COSMOS_CONNECTION_STRING = \"AccountEndpoint=http://localhost:57790/;AccountKey=C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==\"");
        }

        // Quick connectivity check
        try
        {
            // Extract endpoint from connection string
            var endpointMatch = System.Text.RegularExpressions.Regex.Match(
                cosmosConnectionString,
                @"AccountEndpoint=([^;]+)");

            if (!endpointMatch.Success)
            {
                Assert.Inconclusive("Invalid Cosmos DB connection string format.");
                return;
            }

            var endpoint = endpointMatch.Groups[1].Value;
            using var httpClient = new HttpClient(new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
            })
            {
                Timeout = TimeSpan.FromSeconds(2)
            };

            var response = httpClient.GetAsync(endpoint).GetAwaiter().GetResult();
            // If we get any response (even 401), the emulator is running
        }
        catch (Exception ex) when (ex is HttpRequestException || ex is TaskCanceledException)
        {
            Assert.Inconclusive(
                "Cosmos DB Emulator is not reachable. Start it with:\n" +
                COSMOS_EMULATOR_DOCKER_COMMAND + "\n" +
                $"Error: {ex.Message}");
        }
    }

    [TestCleanup]
    public void Cleanup()
    {
        _context.Dispose();
    }

    private void ConfigureAuthorizedWorld()
    {
        var world = World.Create(_userId, "Test World", null);
        var worldType = typeof(World);
        var idProperty = worldType.GetProperty("Id")!;
        idProperty.SetValue(world, _worldId);

        _worldRepository.GetByIdAsync(_worldId, Arg.Any<CancellationToken>())
            .Returns(world);
    }

    private void ConfigureUnauthorizedWorld()
    {
        var world = World.Create(_otherUserId, "Other User's World", null);
        var worldType = typeof(World);
        var idProperty = worldType.GetProperty("Id")!;
        idProperty.SetValue(world, _worldId);

        _worldRepository.GetByIdAsync(_worldId, Arg.Any<CancellationToken>())
            .Returns(world);
    }

    private async Task<WorldEntity> CreateEntityInDatabase(
        string name,
        EntityType type = EntityType.Location,
        Guid? parentId = null)
    {
        var entity = WorldEntity.Create(_worldId, type, name, null, parentId);
        await _context.WorldEntities.AddAsync(entity);
        await _context.SaveChangesAsync();
        return entity;
    }

    #region CreateAsync Tests

    [TestMethod]
    public async Task CreateAsync_WithValidEntity_CreatesAndReturnsEntity()
    {
        // Arrange
        var entity = WorldEntity.Create(_worldId, EntityType.Character, "Test Entity", "A test character");

        // Act
        var result = await _repository.CreateAsync(entity);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("Test Entity");
        result.EntityType.Should().Be(EntityType.Character);
        result.Description.Should().Be("A test character");
        result.Id.Should().NotBe(Guid.Empty);

        var savedEntity = await _context.WorldEntities.FirstOrDefaultAsync(e => e.Name == "Test Entity");
        savedEntity.Should().NotBeNull();
    }

    [TestMethod]
    public async Task CreateAsync_WithParentEntity_AssignsParentCorrectly()
    {
        // Arrange
        var parent = await CreateEntityInDatabase("Parent Location", EntityType.Location);
        var child = WorldEntity.Create(_worldId, EntityType.Location, "Child Location", null, parent.Id);

        // Act
        var result = await _repository.CreateAsync(child);

        // Assert
        result.ParentId.Should().Be(parent.Id);
    }

    [TestMethod]
    public async Task CreateAsync_WithNonexistentParent_ThrowsEntityNotFoundException()
    {
        // Arrange
        var nonExistentParentId = Guid.NewGuid();
        var entity = WorldEntity.Create(_worldId, EntityType.Location, "Test", null, nonExistentParentId);

        // Act & Assert
        await Assert.ThrowsExactlyAsync<EntityNotFoundException>(
            async () => await _repository.CreateAsync(entity));
    }

    [TestMethod]
    public async Task CreateAsync_WithNonexistentWorld_ThrowsWorldNotFoundException()
    {
        // Arrange
        _worldRepository.GetByIdAsync(_worldId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<World?>(null));

        var entity = WorldEntity.Create(_worldId, EntityType.Location, "Test");

        // Act & Assert
        await Assert.ThrowsExactlyAsync<WorldNotFoundException>(
            async () => await _repository.CreateAsync(entity));
    }

    [TestMethod]
    public async Task CreateAsync_WithUnauthorizedWorld_ThrowsUnauthorizedWorldAccessException()
    {
        // Arrange
        ConfigureUnauthorizedWorld();
        var entity = WorldEntity.Create(_worldId, EntityType.Character, "Test");

        // Act & Assert
        await Assert.ThrowsExactlyAsync<UnauthorizedWorldAccessException>(
            async () => await _repository.CreateAsync(entity));
    }

    #endregion

    #region GetByIdAsync Tests

    [TestMethod]
    public async Task GetByIdAsync_WithExistingEntity_ReturnsEntity()
    {
        // Arrange
        var entity = await CreateEntityInDatabase("Existing Entity", EntityType.Item);

        // Act
        var result = await _repository.GetByIdAsync(_worldId, entity.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(entity.Id);
        result.Name.Should().Be("Existing Entity");
    }

    [TestMethod]
    public async Task GetByIdAsync_WithNonexistentEntity_ReturnsNull()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var result = await _repository.GetByIdAsync(_worldId, nonExistentId);

        // Assert
        result.Should().BeNull();
    }

    [TestMethod]
    public async Task GetByIdAsync_WithDeletedEntity_ReturnsNull()
    {
        // Arrange
        var entity = await CreateEntityInDatabase("Deleted Entity");
        entity.SoftDelete();
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByIdAsync(_worldId, entity.Id);

        // Assert
        result.Should().BeNull();
    }

    [TestMethod]
    public async Task GetByIdAsync_WithUnauthorizedWorld_ThrowsUnauthorizedWorldAccessException()
    {
        // Arrange
        ConfigureUnauthorizedWorld();
        var entity = await CreateEntityInDatabase("Test Entity");

        // Act & Assert
        await Assert.ThrowsExactlyAsync<UnauthorizedWorldAccessException>(
            async () => await _repository.GetByIdAsync(_worldId, entity.Id));
    }

    #endregion

    #region GetAllByWorldAsync Tests

    [TestMethod]
    public async Task GetAllByWorldAsync_WithMultipleEntities_ReturnsAll()
    {
        // Arrange
        await CreateEntityInDatabase("Entity 1", EntityType.Location);
        await CreateEntityInDatabase("Entity 2", EntityType.Character);
        await CreateEntityInDatabase("Entity 3", EntityType.Item);

        // Act
        var (entities, nextCursor) = await _repository.GetAllByWorldAsync(_worldId);

        // Assert
        entities.Should().HaveCount(3);
        nextCursor.Should().BeNull();
    }

    [TestMethod]
    public async Task GetAllByWorldAsync_WithEntityTypeFilter_ReturnsFilteredEntities()
    {
        // Arrange
        await CreateEntityInDatabase("Location 1", EntityType.Location);
        await CreateEntityInDatabase("Character 1", EntityType.Character);
        await CreateEntityInDatabase("Location 2", EntityType.Location);

        // Act
        var (entities, _) = await _repository.GetAllByWorldAsync(_worldId, entityType: EntityType.Location);

        // Assert
        entities.Should().HaveCount(2);
        entities.All(e => e.EntityType == EntityType.Location).Should().BeTrue();
    }

    [TestMethod]
    [TestCategory("Integration")]
    [TestCategory("RequiresCosmosDB")]
    public async Task GetAllByWorldAsync_WithTagsFilter_ReturnsMatchingEntities()
    {
        RequireCosmosDbEmulator();

        // Arrange
        var entity1 = WorldEntity.Create(_worldId, EntityType.Character, "Tagged Entity 1", null, null, new List<string> { "hero", "warrior" });
        var entity2 = WorldEntity.Create(_worldId, EntityType.Character, "Tagged Entity 2", null, null, new List<string> { "villain", "mage" });
        var entity3 = WorldEntity.Create(_worldId, EntityType.Character, "Tagged Entity 3", null, null, new List<string> { "hero", "mage" });
        await _context.WorldEntities.AddRangeAsync(entity1, entity2, entity3);
        await _context.SaveChangesAsync();

        // Act
        var (entities, _) = await _repository.GetAllByWorldAsync(_worldId, tags: new List<string> { "hero" });

        // Assert
        entities.Should().HaveCount(2);
        entities.All(e => e.Tags.Any(t => t.ToLower().Contains("hero"))).Should().BeTrue();
    }

    [TestMethod]
    public async Task GetAllByWorldAsync_WithPagination_ReturnsLimitedResults()
    {
        // Arrange
        for (int i = 1; i <= 10; i++)
        {
            await CreateEntityInDatabase($"Entity {i}", EntityType.Location);
            await Task.Delay(1);
        }

        // Act
        var (entities, nextCursor) = await _repository.GetAllByWorldAsync(_worldId, limit: 5);

        // Assert
        entities.Should().HaveCount(5);
        nextCursor.Should().NotBeNullOrEmpty();
    }

    [TestMethod]
    [TestCategory("Integration")]
    [TestCategory("RequiresCosmosDB")]
    public async Task GetAllByWorldAsync_WithCursor_ReturnsNextPage()
    {
        RequireCosmosDbEmulator();

        // Arrange
        var firstEntity = await CreateEntityInDatabase("Entity 1");
        await Task.Delay(10);
        var secondEntity = await CreateEntityInDatabase("Entity 2");

        var cursor = firstEntity.CreatedDate.ToString("O");

        // Act
        var (entities, _) = await _repository.GetAllByWorldAsync(_worldId, cursor: cursor);

        // Assert
        entities.Should().HaveCount(1);
        entities.First().Name.Should().Be("Entity 2");
    }

    [TestMethod]
    public async Task GetAllByWorldAsync_ExcludesDeletedEntities()
    {
        // Arrange
        await CreateEntityInDatabase("Active Entity");
        var deleted = await CreateEntityInDatabase("Deleted Entity");
        deleted.SoftDelete();
        await _context.SaveChangesAsync();

        // Act
        var (entities, _) = await _repository.GetAllByWorldAsync(_worldId);

        // Assert
        entities.Should().HaveCount(1);
        entities.First().Name.Should().Be("Active Entity");
    }

    #endregion

    #region GetChildrenAsync Tests

    [TestMethod]
    public async Task GetChildrenAsync_WithChildren_ReturnsAllChildren()
    {
        // Arrange
        var parent = await CreateEntityInDatabase("Parent");
        var child1 = await CreateEntityInDatabase("Child 1", EntityType.Location, parent.Id);
        var child2 = await CreateEntityInDatabase("Child 2", EntityType.Location, parent.Id);
        await CreateEntityInDatabase("Other Entity"); // Should not be returned

        // Act
        var children = await _repository.GetChildrenAsync(_worldId, parent.Id);

        // Assert
        children.Should().HaveCount(2);
        children.Select(c => c.Name).Should().Contain(new[] { "Child 1", "Child 2" });
    }

    [TestMethod]
    public async Task GetChildrenAsync_WithNoChildren_ReturnsEmpty()
    {
        // Arrange
        var parent = await CreateEntityInDatabase("Parent");

        // Act
        var children = await _repository.GetChildrenAsync(_worldId, parent.Id);

        // Assert
        children.Should().BeEmpty();
    }

    [TestMethod]
    public async Task GetChildrenAsync_ExcludesDeletedChildren()
    {
        // Arrange
        var parent = await CreateEntityInDatabase("Parent");
        var activeChild = await CreateEntityInDatabase("Active Child", EntityType.Location, parent.Id);
        var deletedChild = await CreateEntityInDatabase("Deleted Child", EntityType.Location, parent.Id);
        deletedChild.SoftDelete();
        await _context.SaveChangesAsync();

        // Act
        var children = await _repository.GetChildrenAsync(_worldId, parent.Id);

        // Assert
        children.Should().HaveCount(1);
        children.First().Name.Should().Be("Active Child");
    }

    #endregion

    #region UpdateAsync Tests

    [TestMethod]
    public async Task UpdateAsync_WithValidEntity_UpdatesEntity()
    {
        // Arrange
        var entity = await CreateEntityInDatabase("Original Name");
        entity.Update("Updated Name", "Updated Description", EntityType.Location, null, null, null);

        // Act
        var result = await _repository.UpdateAsync(entity);

        // Assert
        result.Name.Should().Be("Updated Name");
        result.Description.Should().Be("Updated Description");

        var savedEntity = await _context.WorldEntities.FirstOrDefaultAsync(e => e.Id == entity.Id);
        savedEntity!.Name.Should().Be("Updated Name");
    }

    [TestMethod]
    public async Task UpdateAsync_WithNonexistentEntity_ThrowsEntityNotFoundException()
    {
        // Arrange
        var entity = WorldEntity.Create(_worldId, EntityType.Location, "Test");

        // Act & Assert
        await Assert.ThrowsExactlyAsync<EntityNotFoundException>(
            async () => await _repository.UpdateAsync(entity));
    }

    [TestMethod]
    [TestCategory("Integration")]
    [TestCategory("RequiresCosmosDB")]
    public async Task UpdateAsync_WithInvalidETag_ThrowsInvalidOperationException()
    {
        RequireCosmosDbEmulator();

        // Arrange
        var entity = await CreateEntityInDatabase("Test Entity");
        entity.Update("New Name", null, EntityType.Location, null, null, null);

        // Act & Assert
        var exception = await Assert.ThrowsExactlyAsync<InvalidOperationException>(
            async () => await _repository.UpdateAsync(entity, etag: "invalid-etag"));

        exception.Message.Should().Contain("ETag mismatch");
    }

    [TestMethod]
    public async Task UpdateAsync_WithCircularParentReference_ThrowsInvalidOperationException()
    {
        // Arrange
        var parent = await CreateEntityInDatabase("Parent");
        var child = await CreateEntityInDatabase("Child", EntityType.Location, parent.Id);

        // Try to make parent a child of child (circular reference)
        parent.Update(parent.Name, parent.Description, parent.EntityType, child.Id, null, null);

        // Act & Assert
        var exception = await Assert.ThrowsExactlyAsync<InvalidOperationException>(
            async () => await _repository.UpdateAsync(parent));

        exception.Message.Should().Contain("Circular reference");
    }

    [TestMethod]
    public async Task UpdateAsync_WithValidParentChange_UpdatesParent()
    {
        // Arrange
        var newParent = await CreateEntityInDatabase("New Parent");
        var entity = await CreateEntityInDatabase("Entity");
        entity.Update(entity.Name, entity.Description, entity.EntityType, newParent.Id, null, null);

        // Act
        var result = await _repository.UpdateAsync(entity);

        // Assert
        result.ParentId.Should().Be(newParent.Id);
    }

    #endregion

    #region DeleteAsync Tests

    [TestMethod]
    public async Task DeleteAsync_WithValidEntity_SoftDeletesEntity()
    {
        // Arrange
        var entity = await CreateEntityInDatabase("Entity to Delete");

        // Act
        await _repository.DeleteAsync(_worldId, entity.Id);

        // Assert
        var deletedEntity = await _context.WorldEntities.FirstOrDefaultAsync(e => e.Id == entity.Id);
        deletedEntity.Should().NotBeNull();
        deletedEntity!.IsDeleted.Should().BeTrue();
    }

    [TestMethod]
    public async Task DeleteAsync_WithNonexistentEntity_ThrowsEntityNotFoundException()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act & Assert
        await Assert.ThrowsExactlyAsync<EntityNotFoundException>(
            async () => await _repository.DeleteAsync(_worldId, nonExistentId));
    }

    [TestMethod]
    public async Task DeleteAsync_WithChildren_ThrowsInvalidOperationException()
    {
        // Arrange
        var parent = await CreateEntityInDatabase("Parent");
        await CreateEntityInDatabase("Child", EntityType.Location, parent.Id);

        // Act & Assert
        var exception = await Assert.ThrowsExactlyAsync<InvalidOperationException>(
            async () => await _repository.DeleteAsync(_worldId, parent.Id, cascade: false));

        exception.Message.Should().Contain("has").And.Contain("child entities");
    }

    [TestMethod]
    public async Task DeleteAsync_WithCascade_DeletesAllDescendants()
    {
        // Arrange
        var parent = await CreateEntityInDatabase("Parent");
        var child1 = await CreateEntityInDatabase("Child 1", EntityType.Location, parent.Id);
        var grandchild = await CreateEntityInDatabase("Grandchild", EntityType.Location, child1.Id);
        var child2 = await CreateEntityInDatabase("Child 2", EntityType.Location, parent.Id);

        // Act
        await _repository.DeleteAsync(_worldId, parent.Id, cascade: true);

        // Assert
        var allEntities = await _context.WorldEntities.ToListAsync();
        allEntities.Should().HaveCount(4);
        allEntities.Should().OnlyContain(e => e.IsDeleted);
    }

    [TestMethod]
    public async Task DeleteAsync_WithoutChildren_DeletesSuccessfully()
    {
        // Arrange
        var entity = await CreateEntityInDatabase("Standalone Entity");

        // Act
        await _repository.DeleteAsync(_worldId, entity.Id, cascade: false);

        // Assert
        var deletedEntity = await _context.WorldEntities.FirstOrDefaultAsync(e => e.Id == entity.Id);
        deletedEntity!.IsDeleted.Should().BeTrue();
    }

    #endregion
}
