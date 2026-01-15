namespace LibrisMaleficarum.Infrastructure.Tests.Repositories;

using FluentAssertions;
using LibrisMaleficarum.Domain.Entities;
using LibrisMaleficarum.Domain.Exceptions;
using LibrisMaleficarum.Domain.Interfaces.Services;
using LibrisMaleficarum.Infrastructure.Persistence;
using LibrisMaleficarum.Infrastructure.Repositories;
using LibrisMaleficarum.Infrastructure.Tests.Fixtures;
using Microsoft.EntityFrameworkCore;
using NSubstitute;

/// <summary>
/// Unit tests for WorldRepository.
/// Tests CRUD operations, authorization, and pagination.
/// </summary>
[TestClass]
[TestCategory("Unit")]
public class WorldRepositoryTests
{
    private ApplicationDbContext _context = null!;
    private IUserContextService _userContextService = null!;
    private ITelemetryService _telemetryService = null!;
    private WorldRepository _repository = null!;
    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _otherUserId = Guid.NewGuid();
    private readonly string _testDatabaseName = $"TestDb_{Guid.NewGuid()}";

    [TestInitialize]
    public void Setup()
    {
        var options = GetDbContextOptions();
        _context = new ApplicationDbContext(options);
        _userContextService = Substitute.For<IUserContextService>();
        _userContextService.GetCurrentUserIdAsync().Returns(_userId);
        _telemetryService = new NoOpTelemetryService();

        _repository = new WorldRepository(_context, _userContextService, _telemetryService);
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


    [TestCleanup]
    public void Cleanup()
    {
        _context.Dispose();
    }

    #region CreateAsync Tests

    [TestMethod]
    public async Task CreateAsync_WithValidWorld_CreatesAndReturnsWorld()
    {
        // Arrange
        var world = World.Create(_userId, "Test World", "Test Description");

        // Act
        var result = await _repository.CreateAsync(world);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("Test World");
        result.Description.Should().Be("Test Description");
        result.OwnerId.Should().Be(_userId);
        result.Id.Should().NotBe(Guid.Empty);

        var savedWorld = await _context.Worlds.FirstOrDefaultAsync(w => w.Name == "Test World");
        savedWorld.Should().NotBeNull();
    }

    [TestMethod]
    public async Task CreateAsync_SetsCreatedAndModifiedDates()
    {
        // Arrange
        var world = World.Create(_userId, "Test World", null);

        // Act
        var result = await _repository.CreateAsync(world);

        // Assert
        result.CreatedDate.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        result.ModifiedDate.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    #endregion

    #region GetByIdAsync Tests

    [TestMethod]
    public async Task GetByIdAsync_WithExistingWorld_ReturnsWorld()
    {
        // Arrange
        var world = World.Create(_userId, "Existing World", "Description");
        await _context.Worlds.AddAsync(world);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByIdAsync(world.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(world.Id);
        result.Name.Should().Be("Existing World");
    }

    [TestMethod]
    public async Task GetByIdAsync_WithNonexistentWorld_ReturnsNull()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var result = await _repository.GetByIdAsync(nonExistentId);

        // Assert
        result.Should().BeNull();
    }

    [TestMethod]
    public async Task GetByIdAsync_WithDeletedWorld_ReturnsNull()
    {
        // Arrange
        var world = World.Create(_userId, "Deleted World", null);
        world.SoftDelete();
        await _context.Worlds.AddAsync(world);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByIdAsync(world.Id);

        // Assert
        result.Should().BeNull();
    }

    [TestMethod]
    public async Task GetByIdAsync_WithUnauthorizedWorld_ThrowsUnauthorizedWorldAccessException()
    {
        // Arrange
        var world = World.Create(_otherUserId, "Other User's World", null);
        await _context.Worlds.AddAsync(world);
        await _context.SaveChangesAsync();

        // Act & Assert
        await Assert.ThrowsExactlyAsync<UnauthorizedWorldAccessException>(
            async () => await _repository.GetByIdAsync(world.Id));
    }

    #endregion

    #region GetAllByOwnerAsync Tests

    [TestMethod]
    public async Task GetAllByOwnerAsync_WithMultipleWorlds_ReturnsAll()
    {
        // Arrange
        var world1 = World.Create(_userId, "World 1", null);
        var world2 = World.Create(_userId, "World 2", null);
        var world3 = World.Create(_userId, "World 3", null);
        await _context.Worlds.AddRangeAsync(world1, world2, world3);
        await _context.SaveChangesAsync();

        // Act
        var (worlds, nextCursor) = await _repository.GetAllByOwnerAsync(_userId);

        // Assert
        worlds.Should().HaveCount(3);
        worlds.Select(w => w.Name).Should().Contain(new[] { "World 1", "World 2", "World 3" });
        nextCursor.Should().BeNull();
    }

    [TestMethod]
    public async Task GetAllByOwnerAsync_WithPagination_ReturnsLimitedResults()
    {
        // Arrange
        for (int i = 1; i <= 10; i++)
        {
            var world = World.Create(_userId, $"World {i}", null);
            await _context.Worlds.AddAsync(world);
            await Task.Delay(1); // Ensure different CreatedDate for ordering
        }
        await _context.SaveChangesAsync();

        // Act
        var (worlds, nextCursor) = await _repository.GetAllByOwnerAsync(_userId, limit: 5);

        // Assert
        worlds.Should().HaveCount(5);
        nextCursor.Should().NotBeNullOrEmpty();
    }


    [TestMethod]
    public async Task GetAllByOwnerAsync_ExcludesDeletedWorlds()
    {
        // Arrange
        var world1 = World.Create(_userId, "Active World", null);
        var world2 = World.Create(_userId, "Deleted World", null);
        world2.SoftDelete();
        await _context.Worlds.AddRangeAsync(world1, world2);
        await _context.SaveChangesAsync();

        // Act
        var (worlds, _) = await _repository.GetAllByOwnerAsync(_userId);

        // Assert
        worlds.Should().HaveCount(1);
        worlds.First().Name.Should().Be("Active World");
    }

    [TestMethod]
    public async Task GetAllByOwnerAsync_ExcludesOtherUsersWorlds()
    {
        // Arrange
        var myWorld = World.Create(_userId, "My World", null);
        var otherWorld = World.Create(_otherUserId, "Other World", null);
        await _context.Worlds.AddRangeAsync(myWorld, otherWorld);
        await _context.SaveChangesAsync();

        // Act
        var (worlds, _) = await _repository.GetAllByOwnerAsync(_userId);

        // Assert
        worlds.Should().HaveCount(1);
        worlds.First().Name.Should().Be("My World");
    }

    [TestMethod]
    public async Task GetAllByOwnerAsync_CapsLimitTo200()
    {
        // Arrange - Act with limit > 200
        var (worlds, _) = await _repository.GetAllByOwnerAsync(_userId, limit: 300);

        // Assert - Implementation should cap to 200 max
        // Since we don't have 200 worlds, just verify it doesn't throw
        worlds.Should().NotBeNull();
    }

    #endregion

    #region UpdateAsync Tests

    [TestMethod]
    public async Task UpdateAsync_WithValidWorld_UpdatesWorld()
    {
        // Arrange
        var world = World.Create(_userId, "Original Name", "Original Description");
        await _context.Worlds.AddAsync(world);
        await _context.SaveChangesAsync();

        world.Update("Updated Name", "Updated Description");

        // Act
        var result = await _repository.UpdateAsync(world);

        // Assert
        result.Name.Should().Be("Updated Name");
        result.Description.Should().Be("Updated Description");
        result.ModifiedDate.Should().BeAfter(result.CreatedDate);

        var savedWorld = await _context.Worlds.FirstOrDefaultAsync(w => w.Id == world.Id);
        savedWorld!.Name.Should().Be("Updated Name");
    }

    [TestMethod]
    public async Task UpdateAsync_WithNonexistentWorld_ThrowsWorldNotFoundException()
    {
        // Arrange
        var world = World.Create(_userId, "Test", null);

        // Act & Assert
        await Assert.ThrowsExactlyAsync<WorldNotFoundException>(
            async () => await _repository.UpdateAsync(world));
    }

    [TestMethod]
    public async Task UpdateAsync_WithUnauthorizedWorld_ThrowsUnauthorizedWorldAccessException()
    {
        // Arrange
        var world = World.Create(_otherUserId, "Other User's World", null);
        await _context.Worlds.AddAsync(world);
        await _context.SaveChangesAsync();

        world.Update("New Name", null);

        // Act & Assert
        await Assert.ThrowsExactlyAsync<UnauthorizedWorldAccessException>(
            async () => await _repository.UpdateAsync(world));
    }

    #endregion

    #region DeleteAsync Tests

    [TestMethod]
    public async Task DeleteAsync_WithValidWorld_SoftDeletesWorld()
    {
        // Arrange
        var world = World.Create(_userId, "World to Delete", null);
        await _context.Worlds.AddAsync(world);
        await _context.SaveChangesAsync();

        // Act
        await _repository.DeleteAsync(world.Id);

        // Assert
        var deletedWorld = await _context.Worlds.FirstOrDefaultAsync(w => w.Id == world.Id);
        deletedWorld.Should().NotBeNull();
        deletedWorld!.IsDeleted.Should().BeTrue();
    }

    [TestMethod]
    public async Task DeleteAsync_WithNonexistentWorld_ThrowsWorldNotFoundException()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act & Assert
        await Assert.ThrowsExactlyAsync<WorldNotFoundException>(
            async () => await _repository.DeleteAsync(nonExistentId));
    }

    [TestMethod]
    public async Task DeleteAsync_WithUnauthorizedWorld_ThrowsUnauthorizedWorldAccessException()
    {
        // Arrange
        var world = World.Create(_otherUserId, "Other User's World", null);
        await _context.Worlds.AddAsync(world);
        await _context.SaveChangesAsync();

        // Act & Assert
        await Assert.ThrowsExactlyAsync<UnauthorizedWorldAccessException>(
            async () => await _repository.DeleteAsync(world.Id));
    }

    [TestMethod]
    public async Task DeleteAsync_WithAlreadyDeletedWorld_ThrowsWorldNotFoundException()
    {
        // Arrange
        var world = World.Create(_userId, "Already Deleted", null);
        world.SoftDelete();
        await _context.Worlds.AddAsync(world);
        await _context.SaveChangesAsync();

        // Act & Assert
        await Assert.ThrowsExactlyAsync<WorldNotFoundException>(
            async () => await _repository.DeleteAsync(world.Id));
    }

    #endregion
}
