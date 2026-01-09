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
/// Unit tests for AssetRepository with mocked blob storage service.
/// Tests CRUD operations, authorization, and blob storage coordination.
/// </summary>
[TestClass]
public class AssetRepositoryTests
{
    private ApplicationDbContext _context = null!;
    private IWorldRepository _worldRepository = null!;
    private IBlobStorageService _blobStorageService = null!;
    private AssetRepository _repository = null!;
    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _worldId = Guid.NewGuid();
    private readonly Guid _entityId = Guid.NewGuid();
    private const string ValidBlobUrl = "https://storage.azure.com/container/blob";

    [TestInitialize]
    public void Setup()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);

        _worldRepository = Substitute.For<IWorldRepository>();
        _blobStorageService = Substitute.For<IBlobStorageService>();

        _repository = new AssetRepository(
            _context,
            _blobStorageService,
            _worldRepository);
    }

    [TestCleanup]
    public void Cleanup()
    {
        _context.Dispose();
    }

    #region CreateAsync Tests

    [TestMethod]
    public async Task CreateAsync_WithValidAsset_UploadsAndSavesToDatabase()
    {
        // Arrange
        var fileStream = new MemoryStream();
        ConfigureAuthorizedWorld();
        ConfigureExistingEntity();

        _blobStorageService.UploadAsync(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<Stream>(),
            Arg.Any<string>(),
            Arg.Any<IDictionary<string, string>>(),
            Arg.Any<CancellationToken>())
            .Returns(ValidBlobUrl);

        // Act
        var result = await _repository.CreateAsync(_worldId, _entityId, "test.jpg", "image/jpeg", 1024, fileStream, AssetType.Image, null, null, null, _userId);

        // Assert
        result.Should().NotBeNull();
        result.FileName.Should().Be("test.jpg");
        result.BlobUrl.Should().Be(ValidBlobUrl);

        var savedAsset = await _context.Assets.FirstOrDefaultAsync(a => a.FileName == "test.jpg");
        savedAsset.Should().NotBeNull();
        savedAsset!.SizeBytes.Should().Be(1024);
    }

    [TestMethod]
    public async Task CreateAsync_WithUnauthorizedWorld_ThrowsUnauthorizedWorldAccessException()
    {
        // Arrange
        var fileStream = new MemoryStream();
        ConfigureUnauthorizedWorld();

        // Act & Assert
        await Assert.ThrowsExactlyAsync<UnauthorizedWorldAccessException>(
            async () => await _repository.CreateAsync(_worldId, _entityId, "test.jpg", "image/jpeg", 1024, fileStream, AssetType.Image, null, null, null, _userId));
    }

    [TestMethod]
    public async Task CreateAsync_WithNonexistentEntity_ThrowsEntityNotFoundException()
    {
        // Arrange
        var fileStream = new MemoryStream();
        ConfigureAuthorizedWorld();
        // Don't configure entity - it won't exist

        // Act & Assert
        await Assert.ThrowsExactlyAsync<EntityNotFoundException>(
            async () => await _repository.CreateAsync(_worldId, _entityId, "test.jpg", "image/jpeg", 1024, fileStream, AssetType.Image, null, null, null, _userId));
    }

    #endregion

    #region GetByIdAsync Tests

    [TestMethod]
    public async Task GetByIdAsync_WithExistingAsset_ReturnsAsset()
    {
        // Arrange
        var asset = await CreateAndSaveAsset("test.jpg");
        ConfigureAuthorizedWorld();

        // Act
        var result = await _repository.GetByIdAsync(asset.Id, _userId);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(asset.Id);
        result.FileName.Should().Be("test.jpg");
    }

    [TestMethod]
    public async Task GetByIdAsync_WithNonexistentAsset_ThrowsAssetNotFoundException()
    {
        // Arrange
        var nonexistentId = Guid.NewGuid();
        ConfigureAuthorizedWorld();

        // Act & Assert
        await Assert.ThrowsExactlyAsync<AssetNotFoundException>(
            async () => await _repository.GetByIdAsync(nonexistentId, _userId));
    }

    [TestMethod]
    public async Task GetByIdAsync_WithDeletedAsset_ThrowsAssetNotFoundException()
    {
        // Arrange
        var asset = await CreateAndSaveAsset("test.jpg");
        asset.SoftDelete(_userId.ToString());
        await _context.SaveChangesAsync();
        ConfigureAuthorizedWorld();

        // Act & Assert
        await Assert.ThrowsExactlyAsync<AssetNotFoundException>(
            async () => await _repository.GetByIdAsync(asset.Id, _userId));
    }

    [TestMethod]
    public async Task GetByIdAsync_WithUnauthorizedWorld_ThrowsUnauthorizedWorldAccessException()
    {
        // Arrange
        var asset = await CreateAndSaveAsset("test.jpg");
        ConfigureUnauthorizedWorld();

        // Act & Assert
        await Assert.ThrowsExactlyAsync<UnauthorizedWorldAccessException>(
            async () => await _repository.GetByIdAsync(asset.Id, _userId));
    }

    #endregion

    #region GetAllByEntityAsync Tests

    [TestMethod]
    public async Task GetAllByEntityAsync_WithMultipleAssets_ReturnsAllAssets()
    {
        // Arrange
        var asset1 = await CreateAndSaveAsset("file1.jpg");
        var asset2 = await CreateAndSaveAsset("file2.png");
        var asset3 = await CreateAndSaveAsset("other.jpg", Guid.NewGuid()); // Different entity

        ConfigureAuthorizedWorld();

        // Act
        var (results, _) = await _repository.GetAllByEntityAsync(_entityId, _worldId, _userId);

        // Assert
        results.Should().HaveCount(2);
        results.Should().Contain(a => a.FileName == "file1.jpg");
        results.Should().Contain(a => a.FileName == "file2.png");
        results.Should().NotContain(a => a.FileName == "other.jpg");
    }

    [TestMethod]
    public async Task GetAllByEntityAsync_ExcludesDeletedAssets()
    {
        // Arrange
        var asset1 = await CreateAndSaveAsset("active.jpg");
        var asset2 = await CreateAndSaveAsset("deleted.jpg");

        asset2.SoftDelete(_userId.ToString());
        await _context.SaveChangesAsync();

        ConfigureAuthorizedWorld();

        // Act
        var (results, _) = await _repository.GetAllByEntityAsync(_entityId, _worldId, _userId);

        // Assert
        results.Should().HaveCount(1);
        results.Single().FileName.Should().Be("active.jpg");
    }

    [TestMethod]
    public async Task GetAllByEntityAsync_WithUnauthorizedWorld_ThrowsUnauthorizedWorldAccessException()
    {
        // Arrange
        await CreateAndSaveAsset("test.jpg");
        ConfigureUnauthorizedWorld();

        // Act & Assert
        await Assert.ThrowsExactlyAsync<UnauthorizedWorldAccessException>(
            async () => await _repository.GetAllByEntityAsync(_entityId, _worldId, _userId));
    }

    [TestMethod]
    public async Task GetAllByEntityAsync_WithPagination_ReturnsLimitedResults()
    {
        // Arrange
        for (int i = 1; i <= 10; i++)
        {
            await CreateAndSaveAsset($"file{i}.jpg");
        }

        ConfigureAuthorizedWorld();

        // Act
        var (results, nextCursor) = await _repository.GetAllByEntityAsync(_entityId, _worldId, _userId, limit: 5);

        // Assert
        results.Should().HaveCount(5);
        nextCursor.Should().NotBeNullOrEmpty();
    }

    #endregion

    #region DeleteAsync Tests

    [TestMethod]
    public async Task DeleteAsync_WithExistingAsset_SoftDeletesAssetAndDeletesBlob()
    {
        // Arrange
        var asset = await CreateAndSaveAsset("test.jpg");
        ConfigureAuthorizedWorld();

        // Act
        await _repository.DeleteAsync(asset.Id, _userId);

        // Assert
        await _blobStorageService.Received(1).DeleteAsync(asset.BlobUrl, Arg.Any<CancellationToken>());

        var deletedAsset = await _context.Assets.IgnoreQueryFilters()
            .FirstOrDefaultAsync(a => a.Id == asset.Id);
        deletedAsset.Should().NotBeNull();
        deletedAsset!.IsDeleted.Should().BeTrue();
    }

    [TestMethod]
    public async Task DeleteAsync_WithNonexistentAsset_ThrowsAssetNotFoundException()
    {
        // Arrange
        var nonexistentId = Guid.NewGuid();
        ConfigureAuthorizedWorld();

        // Act & Assert
        await Assert.ThrowsExactlyAsync<AssetNotFoundException>(
            async () => await _repository.DeleteAsync(nonexistentId, _userId));

        await _blobStorageService.DidNotReceive().DeleteAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [TestMethod]
    public async Task DeleteAsync_WithAlreadyDeletedAsset_ThrowsAssetNotFoundException()
    {
        // Arrange
        var asset = await CreateAndSaveAsset("test.jpg");
        asset.SoftDelete(_userId.ToString());
        await _context.SaveChangesAsync();
        ConfigureAuthorizedWorld();

        // Act & Assert
        await Assert.ThrowsExactlyAsync<AssetNotFoundException>(
            async () => await _repository.DeleteAsync(asset.Id, _userId));

        await _blobStorageService.DidNotReceive().DeleteAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [TestMethod]
    public async Task DeleteAsync_WithUnauthorizedWorld_ThrowsUnauthorizedWorldAccessException()
    {
        // Arrange
        var asset = await CreateAndSaveAsset("test.jpg");
        ConfigureUnauthorizedWorld();

        // Act & Assert
        await Assert.ThrowsExactlyAsync<UnauthorizedWorldAccessException>(
            async () => await _repository.DeleteAsync(asset.Id, _userId));

        await _blobStorageService.DidNotReceive().DeleteAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    #endregion

    #region Helper Methods

    private void ConfigureAuthorizedWorld()
    {
        _worldRepository.GetByIdAsync(_worldId, Arg.Any<CancellationToken>())
            .Returns(CreateWorld(_worldId, _userId));
    }

    private void ConfigureUnauthorizedWorld()
    {
        var differentUserId = Guid.NewGuid();
        _worldRepository.GetByIdAsync(_worldId, Arg.Any<CancellationToken>())
            .Returns(CreateWorld(_worldId, differentUserId));
    }

    private void ConfigureExistingEntity()
    {
        var entity = WorldEntity.Create(_worldId, EntityType.Location, "Test Entity");
        // Use reflection to set the Id
        var idProperty = typeof(WorldEntity).GetProperty("Id");
        idProperty?.SetValue(entity, _entityId);
        _context.WorldEntities.Add(entity);
        _context.SaveChanges();
    }

    private async Task<Asset> CreateAndSaveAsset(string fileName, Guid? entityId = null)
    {
        var asset = Asset.Create(_worldId, entityId ?? _entityId, fileName, "image/jpeg", 1024, ValidBlobUrl, AssetType.Image);
        _context.Assets.Add(asset);
        await _context.SaveChangesAsync();
        return asset;
    }

    private World CreateWorld(Guid worldId, Guid ownerId)
    {
        var world = World.Create(ownerId, "Test World");
        // Use reflection to set the Id since World.Create doesn't accept a worldId parameter
        var idProperty = typeof(World).GetProperty("Id");
        idProperty?.SetValue(world, worldId);
        return world;
    }

    #endregion
}
