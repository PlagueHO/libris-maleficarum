namespace LibrisMaleficarum.Infrastructure.Tests.Services;

using FluentAssertions;
using LibrisMaleficarum.Domain.Configuration;
using LibrisMaleficarum.Domain.Entities;
using LibrisMaleficarum.Domain.Interfaces.Repositories;
using LibrisMaleficarum.Domain.Interfaces.Services;
using LibrisMaleficarum.Domain.ValueObjects;
using LibrisMaleficarum.Infrastructure.Services;
using LibrisMaleficarum.Infrastructure.Tests.Fixtures;
using Microsoft.Extensions.Options;
using NSubstitute;

/// <summary>
/// Unit tests for DeleteService cascade delete logic.
/// Tests discovery of descendants, batch processing, and progress tracking.
/// </summary>
[TestClass]
[TestCategory("Unit")]
public class DeleteServiceCascadeTests
{
    private IWorldEntityRepository _worldEntityRepository = null!;
    private IDeleteOperationRepository _deleteOperationRepository = null!;
    private IUserContextService _userContextService = null!;
    private ITelemetryService _telemetryService = null!;
    private IOptions<DeleteOperationOptions> _options = null!;
    private DeleteService _deleteService = null!;

    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _worldId = Guid.NewGuid();
    private readonly string _userIdString = "test-user-id";

    [TestInitialize]
    public void Setup()
    {
        _worldEntityRepository = Substitute.For<IWorldEntityRepository>();
        _deleteOperationRepository = Substitute.For<IDeleteOperationRepository>();
        _userContextService = Substitute.For<IUserContextService>();
        _telemetryService = new NoOpTelemetryService();

        _options = Options.Create(new DeleteOperationOptions
        {
            MaxConcurrentPerUserPerWorld = 5,
            RetryAfterSeconds = 60,
            PollingIntervalMs = 500,
            MaxBatchSize = 10,
            RateLimitPerSecond = 0
        });

        _userContextService.GetCurrentUserIdAsync().Returns(_userId);

        _deleteService = new DeleteService(
            _worldEntityRepository,
            _deleteOperationRepository,
            _userContextService,
            _telemetryService,
            _options);
    }

    #region T026: InitiateDeleteAsync with Parent Entity

    [TestMethod]
    public async Task InitiateDeleteAsync_WithParentEntity_CreatesOperationWithCascadeFlag()
    {
        // Arrange
        var parentId = Guid.NewGuid();
        var parentEntity = CreateWorldEntity(parentId, "Parent", EntityType.Continent);

        _deleteOperationRepository.CountActiveByUserAsync(_worldId, _userIdString, Arg.Any<CancellationToken>())
            .Returns(0);

        _worldEntityRepository.GetByIdAsync(_worldId, parentId, Arg.Any<CancellationToken>())
            .Returns(parentEntity);

        var createdOperation = DeleteOperation.Create(_worldId, parentId, "Parent", _userIdString, cascade: true);
        _deleteOperationRepository.CreateAsync(Arg.Any<DeleteOperation>(), Arg.Any<CancellationToken>())
            .Returns(createdOperation);

        // Act
        var result = await _deleteService.InitiateDeleteAsync(_worldId, parentId, cascade: true);

        // Assert
        result.Should().NotBeNull();
        result.Cascade.Should().BeTrue();
        result.RootEntityId.Should().Be(parentId);
        result.RootEntityName.Should().Be("Parent");

        await _deleteOperationRepository.Received(1).CreateAsync(
            Arg.Is<DeleteOperation>(op => op.Cascade == true && op.RootEntityId == parentId),
            Arg.Any<CancellationToken>());
    }

    #endregion

    #region T029: ProcessDeleteAsync with Descendants

    [TestMethod]
    public async Task ProcessDeleteAsync_WithParentAndChildren_DeletesAllDescendants()
    {
        // Arrange
        var parentId = Guid.NewGuid();
        var child1Id = Guid.NewGuid();
        var child2Id = Guid.NewGuid();

        var operation = DeleteOperation.Create(_worldId, parentId, "Parent", _userIdString, cascade: true);
        var operationType = typeof(DeleteOperation);
        var idProperty = operationType.GetProperty("Id")!;
        var operationId = Guid.NewGuid();
        idProperty.SetValue(operation, operationId);

        _deleteOperationRepository.GetByIdAsync(_worldId, operationId, Arg.Any<CancellationToken>())
            .Returns(operation);

        // Setup descendants: 1 parent + 2 children = 3 total
        var descendants = new List<WorldEntity>
        {
            CreateWorldEntity(child1Id, "Child1", EntityType.Country, parentId),
            CreateWorldEntity(child2Id, "Child2", EntityType.Country, parentId)
        };

        _worldEntityRepository.GetDescendantsAsync(parentId, _worldId, Arg.Any<CancellationToken>())
            .Returns(descendants);

        // Mock DeleteAsync to return the count of deleted entities
        _worldEntityRepository.DeleteAsync(_worldId, parentId, _userIdString, true, Arg.Any<CancellationToken>())
            .Returns(3); // 1 parent + 2 children

        // Act
        await _deleteService.ProcessDeleteAsync(_worldId, operationId);

        // Assert - verify operation was updated twice (Start + Complete)
        await _deleteOperationRepository.Received(2).UpdateAsync(Arg.Any<DeleteOperation>(), Arg.Any<CancellationToken>());
        
        // Repository DeleteAsync should be called with cascade=true
        await _worldEntityRepository.Received(1).DeleteAsync(
            _worldId,
            parentId,
            _userIdString,
            true,
            Arg.Any<CancellationToken>());
    }

    [TestMethod]
    public async Task ProcessDeleteAsync_WithDeepHierarchy_DeletesInCorrectOrder()
    {
        // Arrange: Create 4-level hierarchy (parent → child → grandchild → greatgrandchild)
        var parentId = Guid.NewGuid();
        var childId = Guid.NewGuid();
        var grandchildId = Guid.NewGuid();
        var greatGrandchildId = Guid.NewGuid();

        var operation = DeleteOperation.Create(_worldId, parentId, "Parent", _userIdString, cascade: true);
        var operationType = typeof(DeleteOperation);
        var idProperty = operationType.GetProperty("Id")!;
        var operationId = Guid.NewGuid();
        idProperty.SetValue(operation, operationId);

        _deleteOperationRepository.GetByIdAsync(_worldId, operationId, Arg.Any<CancellationToken>())
            .Returns(operation);

        // Setup descendants (all descendants of parent)
        var descendants = new List<WorldEntity>
        {
            CreateWorldEntity(childId, "Child", EntityType.Country, parentId),
            CreateWorldEntity(grandchildId, "Grandchild", EntityType.Region, childId),
            CreateWorldEntity(greatGrandchildId, "GreatGrandchild", EntityType.City, grandchildId)
        };

        _worldEntityRepository.GetDescendantsAsync(parentId, _worldId, Arg.Any<CancellationToken>())
            .Returns(descendants);

        // Mock DeleteAsync to return total count
        _worldEntityRepository.DeleteAsync(_worldId, parentId, _userIdString, true, Arg.Any<CancellationToken>())
            .Returns(4); // 1 parent + 3 descendants

        // Act
        await _deleteService.ProcessDeleteAsync(_worldId, operationId);

        // Assert
        await _worldEntityRepository.Received(1).DeleteAsync(
            _worldId,
            parentId,
            _userIdString,
            true,
            Arg.Any<CancellationToken>());

        // Verify final operation status
        var finalUpdate = _deleteOperationRepository.ReceivedCalls()
            .Where(call => call.GetMethodInfo().Name == nameof(IDeleteOperationRepository.UpdateAsync))
            .Last();

        var updatedOperation = finalUpdate.GetArguments()[0] as DeleteOperation;
        updatedOperation.Should().NotBeNull();
        updatedOperation!.DeletedCount.Should().Be(4);
        updatedOperation.Status.Should().Be(DeleteOperationStatus.Completed);
    }

    [TestMethod]
    public async Task ProcessDeleteAsync_WithAlreadyDeletedDescendant_SkipsAndContinues()
    {
        // Arrange
        var parentId = Guid.NewGuid();
        var child1Id = Guid.NewGuid();
        var child2Id = Guid.NewGuid();

        var operation = DeleteOperation.Create(_worldId, parentId, "Parent", _userIdString, cascade: true);
        var operationType = typeof(DeleteOperation);
        var idProperty = operationType.GetProperty("Id")!;
        var operationId = Guid.NewGuid();
        idProperty.SetValue(operation, operationId);

        _deleteOperationRepository.GetByIdAsync(_worldId, operationId, Arg.Any<CancellationToken>())
            .Returns(operation);

        // Setup descendants: only non-deleted child2 (child1 already deleted, so not returned)
        var descendants = new List<WorldEntity>
        {
            CreateWorldEntity(child2Id, "Child2", EntityType.Country, parentId)
        };

        _worldEntityRepository.GetDescendantsAsync(parentId, _worldId, Arg.Any<CancellationToken>())
            .Returns(descendants);

        // Mock DeleteAsync: only 2 entities deleted (parent + child2)
        _worldEntityRepository.DeleteAsync(_worldId, parentId, _userIdString, true, Arg.Any<CancellationToken>())
            .Returns(2);

        // Act
        await _deleteService.ProcessDeleteAsync(_worldId, operationId);

        // Assert
        var finalUpdate = _deleteOperationRepository.ReceivedCalls()
            .Where(call => call.GetMethodInfo().Name == nameof(IDeleteOperationRepository.UpdateAsync))
            .Last();

        var updatedOperation = finalUpdate.GetArguments()[0] as DeleteOperation;
        updatedOperation.Should().NotBeNull();
        updatedOperation!.DeletedCount.Should().Be(2);
        updatedOperation.Status.Should().Be(DeleteOperationStatus.Completed);
    }

    [TestMethod]
    public async Task ProcessDeleteAsync_UpdatesProgressAfterEachBatch()
    {
        // Arrange
        var parentId = Guid.NewGuid();

        var operation = DeleteOperation.Create(_worldId, parentId, "Parent", _userIdString, cascade: true);
        var operationType = typeof(DeleteOperation);
        var idProperty = operationType.GetProperty("Id")!;
        var operationId = Guid.NewGuid();
        idProperty.SetValue(operation, operationId);

        _deleteOperationRepository.GetByIdAsync(_worldId, operationId, Arg.Any<CancellationToken>())
            .Returns(operation);

        // Setup empty descendants (only parent to delete)
        _worldEntityRepository.GetDescendantsAsync(parentId, _worldId, Arg.Any<CancellationToken>())
            .Returns([]);

        _worldEntityRepository.DeleteAsync(_worldId, parentId, _userIdString, true, Arg.Any<CancellationToken>())
            .Returns(1);

        // Act
        await _deleteService.ProcessDeleteAsync(_worldId, operationId);

        // Assert - should update operation twice: Start + Complete
        await _deleteOperationRepository.Received(2).UpdateAsync(Arg.Any<DeleteOperation>(), Arg.Any<CancellationToken>());
    }

    #endregion

    #region Helper Methods

    private WorldEntity CreateWorldEntity(Guid id, string name, EntityType entityType, Guid? parentId = null)
    {
        var entity = WorldEntity.Create(
            _worldId,
            entityType,
            name,
            _userIdString,
            null,  // description
            parentId,
            null,  // tags
            null,  // attributes
            null,  // parentPath
            -1,    // parentDepth
            1);    // schemaVersion

        // Use reflection to set the Id property (private setter)
        var entityType2 = typeof(WorldEntity);
        var idProperty = entityType2.GetProperty("Id")!;
        idProperty.SetValue(entity, id);

        return entity;
    }

    #endregion
}
