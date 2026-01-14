namespace LibrisMaleficarum.Infrastructure.Tests.Services;

using FluentAssertions;
using LibrisMaleficarum.Domain.Entities;
using LibrisMaleficarum.Domain.Exceptions;
using LibrisMaleficarum.Domain.Interfaces.Repositories;
using LibrisMaleficarum.Domain.Interfaces.Services;
using LibrisMaleficarum.Domain.ValueObjects;
using LibrisMaleficarum.Infrastructure.Persistence;
using LibrisMaleficarum.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using NSubstitute;

/// <summary>
/// Unit tests for SearchService.
/// Tests case-insensitive matching, sorting, and pagination.
/// </summary>
[TestClass]
[TestCategory("Unit")]
public class SearchServiceTests
{
    private ApplicationDbContext _context = null!;
    private IUserContextService _userContextService = null!;
    private IWorldRepository _worldRepository = null!;
    private SearchService _searchService = null!;
    private Guid _worldId;
    private Guid _userId;
    private const string TestOwnerId = "test-owner-id";

    [TestInitialize]
    public void TestInitialize()
    {
        // Setup in-memory database
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);

        _userId = Guid.NewGuid();
        _worldId = Guid.NewGuid();

        // Mock user context service
        _userContextService = Substitute.For<IUserContextService>();
        _userContextService.GetCurrentUserIdAsync()
            .Returns(Task.FromResult(_userId));

        // Mock world repository
        _worldRepository = Substitute.For<IWorldRepository>();
        var mockWorld = World.Create(_userId, "Test World", "Test Description");
        typeof(World).GetProperty("Id")!.SetValue(mockWorld, _worldId);
        _worldRepository.GetByIdAsync(_worldId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<World?>(mockWorld));

        _searchService = new SearchService(_context, _userContextService, _worldRepository);
    }

    [TestCleanup]
    public void TestCleanup()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    [TestMethod]
    public async Task SearchEntitiesAsync_WithNameMatch_ReturnsMatchingEntities()
    {
        // Arrange
        var entity1 = CreateEntity("Dragon Quest", "A quest about dragons");
        var entity2 = CreateEntity("Knight's Tale", "A story of knights");
        var entity3 = CreateEntity("Dragon Slayer", "Slay the dragon");

        await _context.WorldEntities.AddRangeAsync(entity1, entity2, entity3);
        await _context.SaveChangesAsync();

        // Act
        var (results, cursor) = await _searchService.SearchEntitiesAsync(_worldId, "dragon");

        // Assert
        results.Should().HaveCount(2);
        results.Should().Contain(e => e.Id == entity1.Id);
        results.Should().Contain(e => e.Id == entity3.Id);
        results.Should().NotContain(e => e.Id == entity2.Id);
    }

    [TestMethod]
    public async Task SearchEntitiesAsync_WithDescriptionMatch_ReturnsMatchingEntities()
    {
        // Arrange
        var entity1 = CreateEntity("Quest 1", "A quest about dragons");
        var entity2 = CreateEntity("Quest 2", "A quest about knights");

        await _context.WorldEntities.AddRangeAsync(entity1, entity2);
        await _context.SaveChangesAsync();

        // Act
        var (results, cursor) = await _searchService.SearchEntitiesAsync(_worldId, "dragons");

        // Assert
        results.Should().ContainSingle();
        results.First().Id.Should().Be(entity1.Id);
    }

    [TestMethod]
    public async Task SearchEntitiesAsync_WithTagMatch_ReturnsMatchingEntities()
    {
        // Arrange
        var entity1 = CreateEntity("Entity 1", "Description 1", new List<string> { "fantasy", "magic" });
        var entity2 = CreateEntity("Entity 2", "Description 2", new List<string> { "sci-fi", "tech" });

        await _context.WorldEntities.AddRangeAsync(entity1, entity2);
        await _context.SaveChangesAsync();

        // Act
        var (results, cursor) = await _searchService.SearchEntitiesAsync(_worldId, "fantasy");

        // Assert
        results.Should().ContainSingle();
        results.First().Id.Should().Be(entity1.Id);
    }

    [TestMethod]
    public async Task SearchEntitiesAsync_CaseInsensitive_ReturnsMatchingEntities()
    {
        // Arrange
        var entity = CreateEntity("UPPERCASE", "lowercase description");

        await _context.WorldEntities.AddAsync(entity);
        await _context.SaveChangesAsync();

        // Act
        var (results1, _) = await _searchService.SearchEntitiesAsync(_worldId, "uppercase");
        var (results2, _) = await _searchService.SearchEntitiesAsync(_worldId, "LOWERCASE");

        // Assert
        results1.Should().ContainSingle();
        results2.Should().ContainSingle();
    }

    [TestMethod]
    public async Task SearchEntitiesAsync_SortByName_ReturnsSortedResults()
    {
        // Arrange
        var entity1 = CreateEntity("Charlie", "Description");
        var entity2 = CreateEntity("Alice", "Description");
        var entity3 = CreateEntity("Bob", "Description");

        await _context.WorldEntities.AddRangeAsync(entity1, entity2, entity3);
        await _context.SaveChangesAsync();

        // Act - Ascending
        var (resultsAsc, _) = await _searchService.SearchEntitiesAsync(_worldId, "description", sortBy: "name", sortOrder: "asc");

        // Act - Descending
        var (resultsDesc, _) = await _searchService.SearchEntitiesAsync(_worldId, "description", sortBy: "name", sortOrder: "desc");

        // Assert
        var ascList = resultsAsc.ToList();
        ascList[0].Name.Should().Be("Alice");
        ascList[1].Name.Should().Be("Bob");
        ascList[2].Name.Should().Be("Charlie");

        var descList = resultsDesc.ToList();
        descList[0].Name.Should().Be("Charlie");
        descList[1].Name.Should().Be("Bob");
        descList[2].Name.Should().Be("Alice");
    }

    [TestMethod]
    public async Task SearchEntitiesAsync_SortByCreatedDate_ReturnsSortedResults()
    {
        // Arrange
        var entity1 = CreateEntity("Entity 1", "Description", createdDate: DateTime.UtcNow.AddDays(-2));
        var entity2 = CreateEntity("Entity 2", "Description", createdDate: DateTime.UtcNow.AddDays(-1));
        var entity3 = CreateEntity("Entity 3", "Description", createdDate: DateTime.UtcNow);

        await _context.WorldEntities.AddRangeAsync(entity1, entity2, entity3);
        await _context.SaveChangesAsync();

        // Act - Ascending
        var (resultsAsc, _) = await _searchService.SearchEntitiesAsync(_worldId, "description", sortBy: "createdDate", sortOrder: "asc");

        // Act - Descending
        var (resultsDesc, _) = await _searchService.SearchEntitiesAsync(_worldId, "description", sortBy: "createdDate", sortOrder: "desc");

        // Assert
        var ascList = resultsAsc.ToList();
        ascList[0].Id.Should().Be(entity1.Id);
        ascList[1].Id.Should().Be(entity2.Id);
        ascList[2].Id.Should().Be(entity3.Id);

        var descList = resultsDesc.ToList();
        descList[0].Id.Should().Be(entity3.Id);
        descList[1].Id.Should().Be(entity2.Id);
        descList[2].Id.Should().Be(entity1.Id);
    }

    [TestMethod]
    public async Task SearchEntitiesAsync_WithPagination_ReturnsLimitedResults()
    {
        // Arrange
        for (int i = 0; i < 100; i++)
        {
            await _context.WorldEntities.AddAsync(CreateEntity($"Entity {i}", "Description"));
        }
        await _context.SaveChangesAsync();

        // Act
        var (results, cursor) = await _searchService.SearchEntitiesAsync(_worldId, "description", limit: 10);

        // Assert
        results.Should().HaveCount(10);
        cursor.Should().NotBeNullOrEmpty();
    }

    [TestMethod]
    public async Task SearchEntitiesAsync_WithCursor_ReturnsNextPage()
    {
        // Arrange
        for (int i = 0; i < 25; i++)
        {
            await _context.WorldEntities.AddAsync(CreateEntity($"Entity {i}", "Description"));
        }
        await _context.SaveChangesAsync();

        // Act - First page
        var (firstPage, cursor1) = await _searchService.SearchEntitiesAsync(_worldId, "description", limit: 10);

        // Act - Second page
        var (secondPage, cursor2) = await _searchService.SearchEntitiesAsync(_worldId, "description", limit: 10, cursor: cursor1);

        // Assert
        firstPage.Should().HaveCount(10);
        secondPage.Should().HaveCount(10);
        cursor1.Should().NotBeNullOrEmpty();
        cursor2.Should().NotBeNullOrEmpty();

        // Ensure no overlap
        var firstIds = firstPage.Select(e => e.Id).ToList();
        var secondIds = secondPage.Select(e => e.Id).ToList();
        firstIds.Should().NotIntersectWith(secondIds);
    }

    [TestMethod]
    public async Task SearchEntitiesAsync_MaxLimit_ClampsTo200()
    {
        // Arrange
        for (int i = 0; i < 250; i++)
        {
            await _context.WorldEntities.AddAsync(CreateEntity($"Entity {i}", "Description"));
        }
        await _context.SaveChangesAsync();

        // Act
        var (results, _) = await _searchService.SearchEntitiesAsync(_worldId, "description", limit: 500);

        // Assert
        results.Should().HaveCount(200);
    }

    [TestMethod]
    public async Task SearchEntitiesAsync_EmptyQuery_ThrowsArgumentException()
    {
        // Act & Assert
        await FluentActions.Awaiting(() => _searchService.SearchEntitiesAsync(_worldId, ""))
            .Should().ThrowAsync<ArgumentException>()
            .WithMessage("Search query cannot be empty.*");
    }

    [TestMethod]
    public async Task SearchEntitiesAsync_WorldNotFound_ThrowsWorldNotFoundException()
    {
        // Arrange
        _worldRepository.GetByIdAsync(_worldId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<World?>(null));

        // Act & Assert
        await FluentActions.Awaiting(() => _searchService.SearchEntitiesAsync(_worldId, "test"))
            .Should().ThrowAsync<WorldNotFoundException>();
    }

    [TestMethod]
    public async Task SearchEntitiesAsync_UnauthorizedUser_ThrowsUnauthorizedWorldAccessException()
    {
        // Arrange
        var differentUserId = Guid.NewGuid();
        var mockWorld = World.Create(differentUserId, "Test World", "Test Description");
        typeof(World).GetProperty("Id")!.SetValue(mockWorld, _worldId);

        _worldRepository.GetByIdAsync(_worldId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<World?>(mockWorld));

        // Act & Assert
        await FluentActions.Awaiting(() => _searchService.SearchEntitiesAsync(_worldId, "test"))
            .Should().ThrowAsync<UnauthorizedWorldAccessException>();
    }

    [TestMethod]
    public async Task SearchEntitiesAsync_ExcludesDeletedEntities_ReturnsOnlyActive()
    {
        // Arrange
        var entity1 = CreateEntity("Active", "Description");
        var entity2 = CreateEntity("Deleted", "Description");
        entity2.SoftDelete();

        await _context.WorldEntities.AddRangeAsync(entity1, entity2);
        await _context.SaveChangesAsync();

        // Act
        var (results, _) = await _searchService.SearchEntitiesAsync(_worldId, "description");

        // Assert
        results.Should().ContainSingle();
        results.First().Id.Should().Be(entity1.Id);
    }

    private WorldEntity CreateEntity(
        string name,
        string? description = null,
        List<string>? tags = null,
        DateTime? createdDate = null)
    {
        var entity = WorldEntity.Create(
            _worldId,
            EntityType.Other,
            name,
            TestOwnerId,
            description,
            null,
            tags ?? new List<string>(),
            new Dictionary<string, object>());

        if (createdDate.HasValue)
        {
            typeof(WorldEntity).GetProperty("CreatedDate")!.SetValue(entity, createdDate.Value);
            typeof(WorldEntity).GetProperty("ModifiedDate")!.SetValue(entity, createdDate.Value);
        }

        return entity;
    }
}
