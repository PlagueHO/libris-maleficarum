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
/// Integration tests for AssetRepository using Aspire-managed Cosmos DB Emulator.
/// These tests verify real Cosmos DB operations including CRUD, pagination, and authorization.
/// Uses shared AppHostFixture from IntegrationTests.Shared project.
/// Mocks IBlobStorageService since we're testing repository logic, not blob storage.
/// </summary>
[TestClass]
[TestCategory("Integration")]
[TestCategory("RequiresDocker")]
[DoNotParallelize] // AppHost tests must run sequentially to avoid port conflicts
public class AssetRepositoryIntegrationTests
{
    private const string TestOwnerId = "test-owner-id";

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

    /// <summary>
    /// Ensures database and containers are created with retries for reliability.
    /// Cosmos DB container creation can be slow, so we retry if needed.
    /// </summary>
    private static async Task EnsureDatabaseCreatedAsync(ApplicationDbContext context)
    {
        const int maxRetries = 3;
        for (int i = 0; i < maxRetries; i++)
        {
            try
            {
                await context.Database.EnsureCreatedAsync();
                // Verify container exists by making a simple query
                _ = await context.Worlds.FirstOrDefaultAsync();
                return;
            }
            catch (Exception) when (i < maxRetries - 1)
            {
                // Wait before retrying
                await Task.Delay(500);
            }
        }

        // Final attempt without catching
        await context.Database.EnsureCreatedAsync();
    }

    [TestMethod]
    public async Task CreateAsync_CreatesAssetWithBlobStorage()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var testDatabaseName = $"IntegrationTestDb_{Guid.NewGuid()}";

        await using var context = CreateDbContext(testDatabaseName);
        await EnsureDatabaseCreatedAsync(context);

        // Create world and entity
        var world = World.Create(userId, "Test World", null);
        await context.Worlds.AddAsync(world);

        var entity = WorldEntity.Create(world.Id, EntityType.Character, "Test Character", TestOwnerId, null, null, null);
        await context.WorldEntities.AddAsync(entity);
        await context.SaveChangesAsync();

        var worldRepository = Substitute.For<IWorldRepository>();
        worldRepository.GetByIdAsync(world.Id, Arg.Any<CancellationToken>()).Returns(world);

        var blobStorageService = Substitute.For<IBlobStorageService>();
        var expectedBlobUrl = $"https://storage.blob.core.windows.net/assets/{world.Id}/{entity.Id}/test.jpg";
        blobStorageService.UploadAsync(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<Stream>(),
            Arg.Any<string>(),
            Arg.Any<Dictionary<string, string>>(),
            Arg.Any<CancellationToken>())
            .Returns(expectedBlobUrl);

        var repository = new AssetRepository(context, blobStorageService, worldRepository);

        var fileName = "test.jpg";
        var contentType = "image/jpeg";
        var sizeBytes = 1024L;
        using var fileStream = new MemoryStream(new byte[1024]);

        // Act
        var createdAsset = await repository.CreateAsync(
            world.Id,
            entity.Id,
            fileName,
            contentType,
            sizeBytes,
            fileStream,
            AssetType.Image,
            null,
            null,
            null,
            userId);

        // Assert
        createdAsset.Should().NotBeNull();
        createdAsset.FileName.Should().Be(fileName);
        createdAsset.ContentType.Should().Be(contentType);
        createdAsset.SizeBytes.Should().Be(sizeBytes);
        createdAsset.WorldId.Should().Be(world.Id);
        createdAsset.EntityId.Should().Be(entity.Id);
        createdAsset.BlobUrl.Should().Be(expectedBlobUrl);
        createdAsset.IsDeleted.Should().BeFalse();

        // Verify blob storage was called
        await blobStorageService.Received(1).UploadAsync(
            "assets",
            Arg.Is<string>(name => name.Contains(entity.Id.ToString())),
            Arg.Any<Stream>(),
            contentType,
            Arg.Any<Dictionary<string, string>>(),
            Arg.Any<CancellationToken>());

        // Cleanup
        await context.Database.EnsureDeletedAsync();
    }

    [TestMethod]
    public async Task CreateAsync_WithUnauthorizedUser_ThrowsUnauthorizedWorldAccessException()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var unauthorizedUserId = Guid.NewGuid();
        var testDatabaseName = $"IntegrationTestDb_{Guid.NewGuid()}";

        await using var context = CreateDbContext(testDatabaseName);
        await EnsureDatabaseCreatedAsync(context);

        // Create world owned by different user
        var world = World.Create(ownerId, "Owner's World", null);
        await context.Worlds.AddAsync(world);

        var entity = WorldEntity.Create(world.Id, EntityType.Location, "Test Location", TestOwnerId, null, null, null);
        await context.WorldEntities.AddAsync(entity);
        await context.SaveChangesAsync();

        var worldRepository = Substitute.For<IWorldRepository>();
        worldRepository.GetByIdAsync(world.Id, Arg.Any<CancellationToken>()).Returns(world);

        var blobStorageService = Substitute.For<IBlobStorageService>();
        var repository = new AssetRepository(context, blobStorageService, worldRepository);

        using var fileStream = new MemoryStream(new byte[1024]);

        // Act & Assert
        var act = async () => await repository.CreateAsync(
            world.Id,
            entity.Id,
            "test.jpg",
            "image/jpeg",
            1024L,
            fileStream,
            AssetType.Image,
            null,
            null,
            null,
            unauthorizedUserId);

        await act.Should().ThrowAsync<UnauthorizedWorldAccessException>()
            .WithMessage($"*{unauthorizedUserId}*{world.Id}*");

        // Cleanup
        await context.Database.EnsureDeletedAsync();
    }

    [TestMethod]
    public async Task CreateAsync_WithNonExistentEntity_ThrowsEntityNotFoundException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var testDatabaseName = $"IntegrationTestDb_{Guid.NewGuid()}";

        await using var context = CreateDbContext(testDatabaseName);
        await EnsureDatabaseCreatedAsync(context);

        // Create world
        var world = World.Create(userId, "Test World", null);
        await context.Worlds.AddAsync(world);
        await context.SaveChangesAsync();

        var worldRepository = Substitute.For<IWorldRepository>();
        worldRepository.GetByIdAsync(world.Id, Arg.Any<CancellationToken>()).Returns(world);

        var blobStorageService = Substitute.For<IBlobStorageService>();
        var repository = new AssetRepository(context, blobStorageService, worldRepository);

        var nonExistentEntityId = Guid.NewGuid();
        using var fileStream = new MemoryStream(new byte[1024]);

        // Act & Assert
        var act = async () => await repository.CreateAsync(
            world.Id,
            nonExistentEntityId,
            "test.jpg",
            "image/jpeg",
            1024L,
            fileStream, AssetType.Image,
            null,
            null,
            null, userId);

        await act.Should().ThrowAsync<EntityNotFoundException>()
            .WithMessage($"*{nonExistentEntityId}*{world.Id}*");

        // Cleanup
        await context.Database.EnsureDeletedAsync();
    }

    [TestMethod]
    public async Task GetByIdAsync_WithValidId_ReturnsAsset()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var testDatabaseName = $"IntegrationTestDb_{Guid.NewGuid()}";

        await using var context = CreateDbContext(testDatabaseName);
        await EnsureDatabaseCreatedAsync(context);

        // Create world and entity
        var world = World.Create(userId, "Test World", null);
        await context.Worlds.AddAsync(world);

        var entity = WorldEntity.Create(world.Id, EntityType.Character, "Test Character", TestOwnerId, null, null, null);
        await context.WorldEntities.AddAsync(entity);

        var asset = Asset.Create(
            world.Id,
            entity.Id,
            "test.jpg",
            "image/jpeg",
            1024L,
            "https://storage.blob.core.windows.net/assets/test.jpg",
            AssetType.Image);
        await context.Assets.AddAsync(asset);
        await context.SaveChangesAsync();

        var worldRepository = Substitute.For<IWorldRepository>();
        worldRepository.GetByIdAsync(world.Id, Arg.Any<CancellationToken>()).Returns(world);

        var blobStorageService = Substitute.For<IBlobStorageService>();
        var repository = new AssetRepository(context, blobStorageService, worldRepository);

        // Act
        var retrievedAsset = await repository.GetByIdAsync(asset.Id, userId);

        // Assert
        retrievedAsset.Should().NotBeNull();
        retrievedAsset.Id.Should().Be(asset.Id);
        retrievedAsset.FileName.Should().Be("test.jpg");
        retrievedAsset.ContentType.Should().Be("image/jpeg");
        retrievedAsset.WorldId.Should().Be(world.Id);
        retrievedAsset.EntityId.Should().Be(entity.Id);

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
        await EnsureDatabaseCreatedAsync(context);

        // Create world owned by different user
        var world = World.Create(ownerId, "Owner's World", null);
        await context.Worlds.AddAsync(world);

        var entity = WorldEntity.Create(world.Id, EntityType.Character, "Test Character", TestOwnerId, null, null, null);
        await context.WorldEntities.AddAsync(entity);

        var asset = Asset.Create(
            world.Id,
            entity.Id,
            "test.jpg",
            "image/jpeg",
            1024L,
            "https://storage.blob.core.windows.net/assets/test.jpg",
            AssetType.Image);
        await context.Assets.AddAsync(asset);
        await context.SaveChangesAsync();

        var worldRepository = Substitute.For<IWorldRepository>();
        worldRepository.GetByIdAsync(world.Id, Arg.Any<CancellationToken>()).Returns(world);

        var blobStorageService = Substitute.For<IBlobStorageService>();
        var repository = new AssetRepository(context, blobStorageService, worldRepository);

        // Act & Assert
        var act = async () => await repository.GetByIdAsync(asset.Id, unauthorizedUserId);

        await act.Should().ThrowAsync<UnauthorizedWorldAccessException>()
            .WithMessage($"*{unauthorizedUserId}*{world.Id}*");

        // Cleanup
        await context.Database.EnsureDeletedAsync();
    }

    [TestMethod]
    public async Task GetByIdAsync_WithNonExistentAsset_ThrowsAssetNotFoundException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var testDatabaseName = $"IntegrationTestDb_{Guid.NewGuid()}";

        await using var context = CreateDbContext(testDatabaseName);
        await EnsureDatabaseCreatedAsync(context);

        var worldRepository = Substitute.For<IWorldRepository>();
        var blobStorageService = Substitute.For<IBlobStorageService>();
        var repository = new AssetRepository(context, blobStorageService, worldRepository);

        var nonExistentAssetId = Guid.NewGuid();

        // Act & Assert
        var act = async () => await repository.GetByIdAsync(nonExistentAssetId, userId);

        await act.Should().ThrowAsync<AssetNotFoundException>()
            .WithMessage($"*{nonExistentAssetId}*");

        // Cleanup
        await context.Database.EnsureDeletedAsync();
    }

    [TestMethod]
    public async Task GetByIdAsync_WithDeletedAsset_ThrowsAssetNotFoundException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var testDatabaseName = $"IntegrationTestDb_{Guid.NewGuid()}";

        await using var context = CreateDbContext(testDatabaseName);
        await EnsureDatabaseCreatedAsync(context);

        // Create world and entity
        var world = World.Create(userId, "Test World", null);
        await context.Worlds.AddAsync(world);

        var entity = WorldEntity.Create(world.Id, EntityType.Character, "Test Character", TestOwnerId, null, null, null);
        await context.WorldEntities.AddAsync(entity);

        var asset = Asset.Create(
            world.Id,
            entity.Id,
            "test.jpg",
            "image/jpeg",
            1024L,
            "https://storage.blob.core.windows.net/assets/test.jpg",
            AssetType.Image);
        await context.Assets.AddAsync(asset);
        await context.SaveChangesAsync();

        // Soft delete the asset
        asset.SoftDelete(userId.ToString());
        await context.SaveChangesAsync();

        var worldRepository = Substitute.For<IWorldRepository>();
        var blobStorageService = Substitute.For<IBlobStorageService>();
        var repository = new AssetRepository(context, blobStorageService, worldRepository);

        // Act & Assert
        var act = async () => await repository.GetByIdAsync(asset.Id, userId);

        await act.Should().ThrowAsync<AssetNotFoundException>()
            .WithMessage($"*{asset.Id}*");

        // Cleanup
        await context.Database.EnsureDeletedAsync();
    }

    [TestMethod]
    public async Task GetAllByEntityAsync_ReturnsAllAssets()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var testDatabaseName = $"IntegrationTestDb_{Guid.NewGuid()}";

        await using var context = CreateDbContext(testDatabaseName);
        await EnsureDatabaseCreatedAsync(context);

        // Create world and entity
        var world = World.Create(userId, "Test World", null);
        await context.Worlds.AddAsync(world);

        var entity = WorldEntity.Create(world.Id, EntityType.Character, "Test Character", TestOwnerId, null, null, null);
        await context.WorldEntities.AddAsync(entity);

        // Create multiple assets
        var asset1 = Asset.Create(world.Id, entity.Id, "image1.jpg", "image/jpeg", 1024L, "https://blob.url/1.jpg", AssetType.Image);
        var asset2 = Asset.Create(world.Id, entity.Id, "image2.png", "image/png", 2048L, "https://blob.url/2.png", AssetType.Image);
        var asset3 = Asset.Create(world.Id, entity.Id, "document.pdf", "application/pdf", 4096L, "https://blob.url/doc.pdf", AssetType.Document);

        await context.Assets.AddRangeAsync(asset1, asset2, asset3);
        await context.SaveChangesAsync();

        var worldRepository = Substitute.For<IWorldRepository>();
        worldRepository.GetByIdAsync(world.Id, Arg.Any<CancellationToken>()).Returns(world);

        var blobStorageService = Substitute.For<IBlobStorageService>();
        var repository = new AssetRepository(context, blobStorageService, worldRepository);

        // Act
        var (assets, nextCursor) = await repository.GetAllByEntityAsync(entity.Id, world.Id, userId);

        // Assert
        assets.Should().HaveCount(3, "All assets should be returned");
        assets.Should().Contain(a => a.FileName == "image1.jpg");
        assets.Should().Contain(a => a.FileName == "image2.png");
        assets.Should().Contain(a => a.FileName == "document.pdf");
        nextCursor.Should().BeNull("No more pages available");

        // Cleanup
        await context.Database.EnsureDeletedAsync();
    }

    [TestMethod]
    public async Task GetAllByEntityAsync_WithLimit_ReturnsLimitedResults()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var testDatabaseName = $"IntegrationTestDb_{Guid.NewGuid()}";

        await using var context = CreateDbContext(testDatabaseName);
        await EnsureDatabaseCreatedAsync(context);

        // Create world and entity
        var world = World.Create(userId, "Test World", null);
        await context.Worlds.AddAsync(world);

        var entity = WorldEntity.Create(world.Id, EntityType.Character, "Test Character", TestOwnerId, null, null, null);
        await context.WorldEntities.AddAsync(entity);

        // Create multiple assets
        for (int i = 1; i <= 5; i++)
        {
            var asset = Asset.Create(
                world.Id,
                entity.Id,
                $"image{i}.jpg",
                "image/jpeg",
                1024L * i,
                $"https://blob.url/{i}.jpg",
                AssetType.Image);
            await context.Assets.AddAsync(asset);
        }
        await context.SaveChangesAsync();

        var worldRepository = Substitute.For<IWorldRepository>();
        worldRepository.GetByIdAsync(world.Id, Arg.Any<CancellationToken>()).Returns(world);

        var blobStorageService = Substitute.For<IBlobStorageService>();
        var repository = new AssetRepository(context, blobStorageService, worldRepository);

        // Act
        var (assets, nextCursor) = await repository.GetAllByEntityAsync(
            entity.Id,
            world.Id,
            userId,
            limit: 2);

        // Assert
        assets.Should().HaveCount(2, "Limit of 2 was specified");
        nextCursor.Should().NotBeNullOrWhiteSpace("More pages available");

        // Cleanup
        await context.Database.EnsureDeletedAsync();
    }

    [TestMethod]
    public async Task GetAllByEntityAsync_WithCursor_ReturnsNextPage()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var testDatabaseName = $"IntegrationTestDb_{Guid.NewGuid()}";

        await using var context = CreateDbContext(testDatabaseName);
        await EnsureDatabaseCreatedAsync(context);

        // Create world and entity
        var world = World.Create(userId, "Test World", null);
        await context.Worlds.AddAsync(world);

        var entity = WorldEntity.Create(world.Id, EntityType.Character, "Test Character", TestOwnerId, null, null, null);
        await context.WorldEntities.AddAsync(entity);

        // Create multiple assets with delays to ensure different timestamps
        var assets = new List<Asset>();
        for (int i = 1; i <= 3; i++)
        {
            var asset = Asset.Create(
                world.Id,
                entity.Id,
                $"image{i}.jpg",
                "image/jpeg",
                1024L * i,
                $"https://blob.url/{i}.jpg",
                AssetType.Image);
            await context.Assets.AddAsync(asset);
            await context.SaveChangesAsync();
            assets.Add(asset);
            await Task.Delay(1000); // Ensure different timestamps
        }

        var worldRepository = Substitute.For<IWorldRepository>();
        worldRepository.GetByIdAsync(world.Id, Arg.Any<CancellationToken>()).Returns(world);

        var blobStorageService = Substitute.For<IBlobStorageService>();
        var repository = new AssetRepository(context, blobStorageService, worldRepository);

        // Act - Get first page
        var (firstPage, cursor1) = await repository.GetAllByEntityAsync(
            entity.Id,
            world.Id,
            userId,
            limit: 1);

        // Act - Get second page using cursor
        var (secondPage, cursor2) = await repository.GetAllByEntityAsync(
            entity.Id,
            world.Id,
            userId,
            limit: 1,
            cursor: cursor1);

        // Assert
        firstPage.Should().HaveCount(1);
        cursor1.Should().NotBeNullOrWhiteSpace();
        secondPage.Should().HaveCount(1);
        firstPage[0].Id.Should().NotBe(secondPage[0].Id, "Different assets should be returned");

        // Cleanup
        await context.Database.EnsureDeletedAsync();
    }

    [TestMethod]
    public async Task GetAllByEntityAsync_ExcludesDeletedAssets()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var testDatabaseName = $"IntegrationTestDb_{Guid.NewGuid()}";

        await using var context = CreateDbContext(testDatabaseName);
        await EnsureDatabaseCreatedAsync(context);

        // Create world and entity
        var world = World.Create(userId, "Test World", null);
        await context.Worlds.AddAsync(world);

        var entity = WorldEntity.Create(world.Id, EntityType.Character, "Test Character", TestOwnerId, null, null, null);
        await context.WorldEntities.AddAsync(entity);

        // Create assets
        var asset1 = Asset.Create(world.Id, entity.Id, "active.jpg", "image/jpeg", 1024L, "https://blob.url/active.jpg", AssetType.Image);
        var asset2 = Asset.Create(world.Id, entity.Id, "deleted.jpg", "image/jpeg", 1024L, "https://blob.url/deleted.jpg", AssetType.Image);

        await context.Assets.AddRangeAsync(asset1, asset2);
        await context.SaveChangesAsync();

        // Soft delete asset2
        asset2.SoftDelete(userId.ToString());
        await context.SaveChangesAsync();

        var worldRepository = Substitute.For<IWorldRepository>();
        worldRepository.GetByIdAsync(world.Id, Arg.Any<CancellationToken>()).Returns(world);

        var blobStorageService = Substitute.For<IBlobStorageService>();
        var repository = new AssetRepository(context, blobStorageService, worldRepository);

        // Act
        var (assets, _) = await repository.GetAllByEntityAsync(entity.Id, world.Id, userId);

        // Assert
        assets.Should().HaveCount(1, "Deleted assets should be excluded");
        assets.Should().Contain(a => a.FileName == "active.jpg");
        assets.Should().NotContain(a => a.FileName == "deleted.jpg");

        // Cleanup
        await context.Database.EnsureDeletedAsync();
    }

    [TestMethod]
    public async Task GetAllByEntityAsync_WithUnauthorizedUser_ThrowsUnauthorizedWorldAccessException()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var unauthorizedUserId = Guid.NewGuid();
        var testDatabaseName = $"IntegrationTestDb_{Guid.NewGuid()}";

        await using var context = CreateDbContext(testDatabaseName);
        await EnsureDatabaseCreatedAsync(context);

        // Create world owned by different user
        var world = World.Create(ownerId, "Owner's World", null);
        await context.Worlds.AddAsync(world);

        var entity = WorldEntity.Create(world.Id, EntityType.Character, "Test Character", TestOwnerId, null, null, null);
        await context.WorldEntities.AddAsync(entity);
        await context.SaveChangesAsync();

        var worldRepository = Substitute.For<IWorldRepository>();
        worldRepository.GetByIdAsync(world.Id, Arg.Any<CancellationToken>()).Returns(world);

        var blobStorageService = Substitute.For<IBlobStorageService>();
        var repository = new AssetRepository(context, blobStorageService, worldRepository);

        // Act & Assert
        var act = async () => await repository.GetAllByEntityAsync(
            entity.Id,
            world.Id,
            unauthorizedUserId);

        await act.Should().ThrowAsync<UnauthorizedWorldAccessException>()
            .WithMessage($"*{unauthorizedUserId}*{world.Id}*");

        // Cleanup
        await context.Database.EnsureDeletedAsync();
    }

    [TestMethod]
    public async Task DeleteAsync_SoftDeletesAssetAndDeletesBlob()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var testDatabaseName = $"IntegrationTestDb_{Guid.NewGuid()}";

        await using var context = CreateDbContext(testDatabaseName);
        await EnsureDatabaseCreatedAsync(context);

        // Create world and entity
        var world = World.Create(userId, "Test World", null);
        await context.Worlds.AddAsync(world);

        var entity = WorldEntity.Create(world.Id, EntityType.Character, "Test Character", TestOwnerId, null, null, null);
        await context.WorldEntities.AddAsync(entity);

        var blobUrl = "https://storage.blob.core.windows.net/assets/test.jpg";
        var asset = Asset.Create(
            world.Id,
            entity.Id,
            "test.jpg",
            "image/jpeg",
            1024L,
            blobUrl,
            AssetType.Image);
        await context.Assets.AddAsync(asset);
        await context.SaveChangesAsync();

        var worldRepository = Substitute.For<IWorldRepository>();
        worldRepository.GetByIdAsync(world.Id, Arg.Any<CancellationToken>()).Returns(world);

        var blobStorageService = Substitute.For<IBlobStorageService>();
        var repository = new AssetRepository(context, blobStorageService, worldRepository);

        // Act
        await repository.DeleteAsync(asset.Id, userId);

        // Assert - Verify soft delete in database
        var deletedAsset = await context.Assets.FirstOrDefaultAsync(a => a.Id == asset.Id);
        deletedAsset.Should().NotBeNull();
        deletedAsset!.IsDeleted.Should().BeTrue("Asset should be soft deleted");

        // Assert - Verify blob deletion was called
        await blobStorageService.Received(1).DeleteAsync(blobUrl, Arg.Any<CancellationToken>());

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
        await EnsureDatabaseCreatedAsync(context);

        // Create world owned by different user
        var world = World.Create(ownerId, "Owner's World", null);
        await context.Worlds.AddAsync(world);

        var entity = WorldEntity.Create(world.Id, EntityType.Character, "Test Character", TestOwnerId, null, null, null);
        await context.WorldEntities.AddAsync(entity);

        var asset = Asset.Create(
            world.Id,
            entity.Id,
            "test.jpg",
            "image/jpeg",
            1024L,
            "https://storage.blob.core.windows.net/assets/test.jpg",
            AssetType.Image);
        await context.Assets.AddAsync(asset);
        await context.SaveChangesAsync();

        var worldRepository = Substitute.For<IWorldRepository>();
        worldRepository.GetByIdAsync(world.Id, Arg.Any<CancellationToken>()).Returns(world);

        var blobStorageService = Substitute.For<IBlobStorageService>();
        var repository = new AssetRepository(context, blobStorageService, worldRepository);

        // Act & Assert
        var act = async () => await repository.DeleteAsync(asset.Id, unauthorizedUserId);

        await act.Should().ThrowAsync<UnauthorizedWorldAccessException>()
            .WithMessage($"*{unauthorizedUserId}*{world.Id}*");

        // Cleanup
        await context.Database.EnsureDeletedAsync();
    }

    [TestMethod]
    public async Task DeleteAsync_WithNonExistentAsset_ThrowsAssetNotFoundException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var testDatabaseName = $"IntegrationTestDb_{Guid.NewGuid()}";

        await using var context = CreateDbContext(testDatabaseName);
        await EnsureDatabaseCreatedAsync(context);

        var worldRepository = Substitute.For<IWorldRepository>();
        var blobStorageService = Substitute.For<IBlobStorageService>();
        var repository = new AssetRepository(context, blobStorageService, worldRepository);

        var nonExistentAssetId = Guid.NewGuid();

        // Act & Assert
        var act = async () => await repository.DeleteAsync(nonExistentAssetId, userId);

        await act.Should().ThrowAsync<AssetNotFoundException>()
            .WithMessage($"*{nonExistentAssetId}*");

        // Cleanup
        await context.Database.EnsureDeletedAsync();
    }
}
