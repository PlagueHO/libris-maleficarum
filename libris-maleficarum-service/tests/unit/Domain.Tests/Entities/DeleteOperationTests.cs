namespace LibrisMaleficarum.Domain.Tests.Entities;

using FluentAssertions;
using LibrisMaleficarum.Domain.Entities;

/// <summary>
/// Unit tests for DeleteOperation entity methods.
/// Tests create, start, update progress, complete, and fail operations.
/// </summary>
[TestClass]
[TestCategory("Unit")]
public class DeleteOperationTests
{
    private readonly Guid _worldId = Guid.NewGuid();
    private readonly Guid _rootEntityId = Guid.NewGuid();
    private const string RootEntityName = "Test Entity";
    private const string TestUserId = "test-user-id";

    #region Create Tests

    [TestMethod]
    public void Create_WithValidParameters_ShouldCreateOperation()
    {
        // Act
        var operation = DeleteOperation.Create(_worldId, _rootEntityId, RootEntityName, TestUserId, cascade: true);

        // Assert
        operation.Should().NotBeNull();
        operation.Id.Should().NotBeEmpty();
        operation.WorldId.Should().Be(_worldId);
        operation.RootEntityId.Should().Be(_rootEntityId);
        operation.RootEntityName.Should().Be(RootEntityName);
        operation.Status.Should().Be(DeleteOperationStatus.Pending);
        operation.TotalEntities.Should().Be(0);
        operation.DeletedCount.Should().Be(0);
        operation.FailedCount.Should().Be(0);
        operation.FailedEntityIds.Should().BeEmpty();
        operation.ErrorDetails.Should().BeNull();
        operation.CreatedBy.Should().Be(TestUserId);
        operation.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        operation.StartedAt.Should().BeNull();
        operation.CompletedAt.Should().BeNull();
        operation.Cascade.Should().BeTrue();
        operation.Ttl.Should().Be(86400); // 24 hours
    }

    [TestMethod]
    public void Create_WithCascadeFalse_ShouldSetCascadeToFalse()
    {
        // Act
        var operation = DeleteOperation.Create(_worldId, _rootEntityId, RootEntityName, TestUserId, cascade: false);

        // Assert
        operation.Cascade.Should().BeFalse();
    }

    [TestMethod]
    public void Create_WithNullRootEntityName_ShouldThrowArgumentException()
    {
        // Act
        var action = () => DeleteOperation.Create(_worldId, _rootEntityId, null!, TestUserId);

        // Assert
        action.Should().Throw<ArgumentException>()
            .WithMessage("*Root entity name is required*")
            .And.ParamName.Should().Be("rootEntityName");
    }

    [TestMethod]
    public void Create_WithEmptyRootEntityName_ShouldThrowArgumentException()
    {
        // Act
        var action = () => DeleteOperation.Create(_worldId, _rootEntityId, string.Empty, TestUserId);

        // Assert
        action.Should().Throw<ArgumentException>()
            .WithMessage("*Root entity name is required*")
            .And.ParamName.Should().Be("rootEntityName");
    }

    [TestMethod]
    public void Create_WithNullCreatedBy_ShouldThrowArgumentException()
    {
        // Act
        var action = () => DeleteOperation.Create(_worldId, _rootEntityId, RootEntityName, null!);

        // Assert
        action.Should().Throw<ArgumentException>()
            .WithMessage("*CreatedBy is required*")
            .And.ParamName.Should().Be("createdBy");
    }

    [TestMethod]
    public void Create_WithEmptyCreatedBy_ShouldThrowArgumentException()
    {
        // Act
        var action = () => DeleteOperation.Create(_worldId, _rootEntityId, RootEntityName, string.Empty);

        // Assert
        action.Should().Throw<ArgumentException>()
            .WithMessage("*CreatedBy is required*")
            .And.ParamName.Should().Be("createdBy");
    }

    #endregion

    #region Start Tests

    [TestMethod]
    public void Start_WithValidTotalEntities_ShouldUpdateStatus()
    {
        // Arrange
        var operation = DeleteOperation.Create(_worldId, _rootEntityId, RootEntityName, TestUserId);

        // Act
        operation.Start(10);

        // Assert
        operation.Status.Should().Be(DeleteOperationStatus.InProgress);
        operation.TotalEntities.Should().Be(10);
        operation.StartedAt.Should().NotBeNull();
        operation.StartedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [TestMethod]
    public void Start_WithZeroEntities_ShouldSucceed()
    {
        // Arrange
        var operation = DeleteOperation.Create(_worldId, _rootEntityId, RootEntityName, TestUserId);

        // Act
        operation.Start(0);

        // Assert
        operation.Status.Should().Be(DeleteOperationStatus.InProgress);
        operation.TotalEntities.Should().Be(0);
    }

    [TestMethod]
    public void Start_WithNegativeTotalEntities_ShouldThrowArgumentException()
    {
        // Arrange
        var operation = DeleteOperation.Create(_worldId, _rootEntityId, RootEntityName, TestUserId);

        // Act
        var action = () => operation.Start(-1);

        // Assert
        action.Should().Throw<ArgumentException>()
            .WithMessage("*Total entities must be non-negative*")
            .And.ParamName.Should().Be("totalEntities");
    }

    #endregion

    #region UpdateProgress Tests

    [TestMethod]
    public void UpdateProgress_WithValidCounts_ShouldUpdateCounters()
    {
        // Arrange
        var operation = DeleteOperation.Create(_worldId, _rootEntityId, RootEntityName, TestUserId);
        operation.Start(10);

        // Act
        operation.UpdateProgress(deletedCount: 7, failedCount: 2);

        // Assert
        operation.DeletedCount.Should().Be(7);
        operation.FailedCount.Should().Be(2);
    }

    [TestMethod]
    public void UpdateProgress_WithFailedIds_ShouldStoreFailedEntityIds()
    {
        // Arrange
        var operation = DeleteOperation.Create(_worldId, _rootEntityId, RootEntityName, TestUserId);
        operation.Start(10);
        var failedIds = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() };

        // Act
        operation.UpdateProgress(deletedCount: 8, failedCount: 2, failedIds: failedIds);

        // Assert
        operation.FailedEntityIds.Should().BeEquivalentTo(failedIds);
    }

    [TestMethod]
    public void UpdateProgress_WithNegativeDeletedCount_ShouldThrowArgumentException()
    {
        // Arrange
        var operation = DeleteOperation.Create(_worldId, _rootEntityId, RootEntityName, TestUserId);
        operation.Start(10);

        // Act
        var action = () => operation.UpdateProgress(deletedCount: -1, failedCount: 0);

        // Assert
        action.Should().Throw<ArgumentException>()
            .WithMessage("*Deleted count must be non-negative*")
            .And.ParamName.Should().Be("deletedCount");
    }

    [TestMethod]
    public void UpdateProgress_WithNegativeFailedCount_ShouldThrowArgumentException()
    {
        // Arrange
        var operation = DeleteOperation.Create(_worldId, _rootEntityId, RootEntityName, TestUserId);
        operation.Start(10);

        // Act
        var action = () => operation.UpdateProgress(deletedCount: 5, failedCount: -1);

        // Assert
        action.Should().Throw<ArgumentException>()
            .WithMessage("*Failed count must be non-negative*")
            .And.ParamName.Should().Be("failedCount");
    }

    #endregion

    #region AddFailedEntity Tests

    [TestMethod]
    public void AddFailedEntity_WithNewEntityId_ShouldAddToList()
    {
        // Arrange
        var operation = DeleteOperation.Create(_worldId, _rootEntityId, RootEntityName, TestUserId);
        var failedId = Guid.NewGuid();

        // Act
        operation.AddFailedEntity(failedId);

        // Assert
        operation.FailedEntityIds.Should().Contain(failedId);
        operation.FailedCount.Should().Be(1);
    }

    [TestMethod]
    public void AddFailedEntity_WithDuplicateEntityId_ShouldNotAddDuplicate()
    {
        // Arrange
        var operation = DeleteOperation.Create(_worldId, _rootEntityId, RootEntityName, TestUserId);
        var failedId = Guid.NewGuid();

        // Act
        operation.AddFailedEntity(failedId);
        operation.AddFailedEntity(failedId); // Add same ID again

        // Assert
        operation.FailedEntityIds.Should().HaveCount(1);
        operation.FailedCount.Should().Be(1);
    }

    [TestMethod]
    public void AddFailedEntity_WithMultipleIds_ShouldAddAll()
    {
        // Arrange
        var operation = DeleteOperation.Create(_worldId, _rootEntityId, RootEntityName, TestUserId);
        var failedId1 = Guid.NewGuid();
        var failedId2 = Guid.NewGuid();
        var failedId3 = Guid.NewGuid();

        // Act
        operation.AddFailedEntity(failedId1);
        operation.AddFailedEntity(failedId2);
        operation.AddFailedEntity(failedId3);

        // Assert
        operation.FailedEntityIds.Should().HaveCount(3);
        operation.FailedCount.Should().Be(3);
        operation.FailedEntityIds.Should().Contain([failedId1, failedId2, failedId3]);
    }

    #endregion

    #region HasFailures Tests

    [TestMethod]
    public void HasFailures_WithNoFailures_ShouldReturnFalse()
    {
        // Arrange
        var operation = DeleteOperation.Create(_worldId, _rootEntityId, RootEntityName, TestUserId);

        // Act & Assert
        operation.HasFailures.Should().BeFalse();
    }

    [TestMethod]
    public void HasFailures_WithFailures_ShouldReturnTrue()
    {
        // Arrange
        var operation = DeleteOperation.Create(_worldId, _rootEntityId, RootEntityName, TestUserId);
        operation.AddFailedEntity(Guid.NewGuid());

        // Act & Assert
        operation.HasFailures.Should().BeTrue();
    }

    [TestMethod]
    public void HasFailures_WithFailuresViaUpdateProgress_ShouldReturnTrue()
    {
        // Arrange
        var operation = DeleteOperation.Create(_worldId, _rootEntityId, RootEntityName, TestUserId);
        operation.Start(10);
        var failedIds = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() };
        operation.UpdateProgress(deletedCount: 8, failedCount: 2, failedIds: failedIds);

        // Act & Assert
        operation.HasFailures.Should().BeTrue();
    }

    #endregion

    #region Complete Tests

    [TestMethod]
    public void Complete_WithNoFailures_ShouldSetStatusToCompleted()
    {
        // Arrange
        var operation = DeleteOperation.Create(_worldId, _rootEntityId, RootEntityName, TestUserId);
        operation.Start(10);
        operation.UpdateProgress(deletedCount: 10, failedCount: 0);

        // Act
        operation.Complete();

        // Assert
        operation.Status.Should().Be(DeleteOperationStatus.Completed);
        operation.CompletedAt.Should().NotBeNull();
        operation.CompletedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [TestMethod]
    public void Complete_WithFailures_ShouldSetStatusToPartial()
    {
        // Arrange
        var operation = DeleteOperation.Create(_worldId, _rootEntityId, RootEntityName, TestUserId);
        operation.Start(10);
        operation.UpdateProgress(deletedCount: 8, failedCount: 2);

        // Act
        operation.Complete();

        // Assert
        operation.Status.Should().Be(DeleteOperationStatus.Partial);
        operation.CompletedAt.Should().NotBeNull();
    }

    [TestMethod]
    public void Complete_WithAllFailed_ShouldSetStatusToFailed()
    {
        // Arrange
        var operation = DeleteOperation.Create(_worldId, _rootEntityId, RootEntityName, TestUserId);
        operation.Start(10);
        operation.UpdateProgress(deletedCount: 0, failedCount: 10);

        // Act
        operation.Complete();

        // Assert
        operation.Status.Should().Be(DeleteOperationStatus.Failed);
        operation.CompletedAt.Should().NotBeNull();
    }

    [TestMethod]
    public void Complete_WithZeroEntities_ShouldSetStatusToCompleted()
    {
        // Arrange
        var operation = DeleteOperation.Create(_worldId, _rootEntityId, RootEntityName, TestUserId);
        operation.Start(0);

        // Act
        operation.Complete();

        // Assert
        operation.Status.Should().Be(DeleteOperationStatus.Completed);
        operation.CompletedAt.Should().NotBeNull();
    }

    #endregion

    #region Fail Tests

    [TestMethod]
    public void Fail_WithValidErrorDetails_ShouldSetStatusToFailed()
    {
        // Arrange
        var operation = DeleteOperation.Create(_worldId, _rootEntityId, RootEntityName, TestUserId);
        operation.Start(10);
        var errorDetails = "Database connection lost";

        // Act
        operation.Fail(errorDetails);

        // Assert
        operation.Status.Should().Be(DeleteOperationStatus.Failed);
        operation.ErrorDetails.Should().Be(errorDetails);
        operation.CompletedAt.Should().NotBeNull();
        operation.CompletedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [TestMethod]
    public void Fail_WithNullErrorDetails_ShouldThrowArgumentException()
    {
        // Arrange
        var operation = DeleteOperation.Create(_worldId, _rootEntityId, RootEntityName, TestUserId);

        // Act
        var action = () => operation.Fail(null!);

        // Assert
        action.Should().Throw<ArgumentException>()
            .WithMessage("*Error details are required*")
            .And.ParamName.Should().Be("errorDetails");
    }

    [TestMethod]
    public void Fail_WithEmptyErrorDetails_ShouldThrowArgumentException()
    {
        // Arrange
        var operation = DeleteOperation.Create(_worldId, _rootEntityId, RootEntityName, TestUserId);

        // Act
        var action = () => operation.Fail(string.Empty);

        // Assert
        action.Should().Throw<ArgumentException>()
            .WithMessage("*Error details are required*")
            .And.ParamName.Should().Be("errorDetails");
    }

    #endregion

    #region Retry Tests

    [TestMethod]
    public void Retry_WithFailedOperation_ShouldResetToRetryableState()
    {
        // Arrange
        var operation = DeleteOperation.Create(_worldId, _rootEntityId, RootEntityName, TestUserId);
        operation.Start(10);
        operation.UpdateProgress(deletedCount: 5, failedCount: 5);
        operation.AddFailedEntity(Guid.NewGuid());
        operation.Fail("Database connection lost");
        var originalId = operation.Id;
        var originalCreatedAt = operation.CreatedAt;

        // Act
        operation.Retry();

        // Assert
        operation.Id.Should().Be(originalId, "operation ID should be preserved");
        operation.Status.Should().Be(DeleteOperationStatus.Pending);
        operation.DeletedCount.Should().Be(0);
        operation.FailedCount.Should().Be(0);
        operation.FailedEntityIds.Should().BeEmpty();
        operation.ErrorDetails.Should().BeNull();
        operation.StartedAt.Should().BeNull();
        operation.CompletedAt.Should().BeNull();
        operation.CreatedAt.Should().Be(originalCreatedAt, "creation timestamp should be preserved");
        operation.WorldId.Should().Be(_worldId, "world ID should be preserved");
        operation.RootEntityId.Should().Be(_rootEntityId, "root entity ID should be preserved");
        operation.RootEntityName.Should().Be(RootEntityName, "root entity name should be preserved");
    }

    [TestMethod]
    public void Retry_WithPartialOperation_ShouldResetToRetryableState()
    {
        // Arrange
        var operation = DeleteOperation.Create(_worldId, _rootEntityId, RootEntityName, TestUserId);
        operation.Start(10);
        operation.UpdateProgress(deletedCount: 7, failedCount: 3);
        operation.AddFailedEntity(Guid.NewGuid());
        operation.AddFailedEntity(Guid.NewGuid());
        operation.AddFailedEntity(Guid.NewGuid());
        operation.Complete(); // Should result in Partial status
        var originalId = operation.Id;

        // Act
        operation.Retry();

        // Assert
        operation.Id.Should().Be(originalId, "operation ID should be preserved");
        operation.Status.Should().Be(DeleteOperationStatus.Pending);
        operation.DeletedCount.Should().Be(0);
        operation.FailedCount.Should().Be(0);
        operation.FailedEntityIds.Should().BeEmpty();
        operation.ErrorDetails.Should().BeNull();
        operation.StartedAt.Should().BeNull();
        operation.CompletedAt.Should().BeNull();
    }

    [TestMethod]
    public void Retry_WithCompletedOperation_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var operation = DeleteOperation.Create(_worldId, _rootEntityId, RootEntityName, TestUserId);
        operation.Start(10);
        operation.UpdateProgress(deletedCount: 10, failedCount: 0);
        operation.Complete();

        // Act
        var action = () => operation.Retry();

        // Assert
        action.Should().Throw<InvalidOperationException>()
            .WithMessage("*Only failed or partial operations can be retried*");
    }

    [TestMethod]
    public void Retry_WithPendingOperation_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var operation = DeleteOperation.Create(_worldId, _rootEntityId, RootEntityName, TestUserId);

        // Act
        var action = () => operation.Retry();

        // Assert
        action.Should().Throw<InvalidOperationException>()
            .WithMessage("*Only failed or partial operations can be retried*");
    }

    [TestMethod]
    public void Retry_WithInProgressOperation_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var operation = DeleteOperation.Create(_worldId, _rootEntityId, RootEntityName, TestUserId);
        operation.Start(10);

        // Act
        var action = () => operation.Retry();

        // Assert
        action.Should().Throw<InvalidOperationException>()
            .WithMessage("*Only failed or partial operations can be retried*");
    }

    [TestMethod]
    public void Retry_PreservesImmutableProperties_AfterRetry()
    {
        // Arrange
        var operation = DeleteOperation.Create(_worldId, _rootEntityId, RootEntityName, TestUserId, cascade: true, ttlSeconds: 3600);
        operation.Start(10);
        operation.Fail("Error occurred");
        
        var originalId = operation.Id;
        var originalWorldId = operation.WorldId;
        var originalRootEntityId = operation.RootEntityId;
        var originalRootEntityName = operation.RootEntityName;
        var originalCreatedBy = operation.CreatedBy;
        var originalCreatedAt = operation.CreatedAt;
        var originalCascade = operation.Cascade;
        var originalTtl = operation.Ttl;

        // Act
        operation.Retry();

        // Assert - All immutable properties should be preserved
        operation.Id.Should().Be(originalId);
        operation.WorldId.Should().Be(originalWorldId);
        operation.RootEntityId.Should().Be(originalRootEntityId);
        operation.RootEntityName.Should().Be(originalRootEntityName);
        operation.CreatedBy.Should().Be(originalCreatedBy);
        operation.CreatedAt.Should().Be(originalCreatedAt);
        operation.Cascade.Should().Be(originalCascade);
        operation.Ttl.Should().Be(originalTtl);
    }

    #endregion

    #region Complete Workflow Tests

    [TestMethod]
    public void CompleteWorkflow_CreateToComplete_ShouldFollowExpectedStates()
    {
        // Arrange & Act - Create
        var operation = DeleteOperation.Create(_worldId, _rootEntityId, RootEntityName, TestUserId);
        operation.Status.Should().Be(DeleteOperationStatus.Pending);

        // Act - Start
        operation.Start(10);
        operation.Status.Should().Be(DeleteOperationStatus.InProgress);

        // Act - Update Progress
        operation.UpdateProgress(deletedCount: 5, failedCount: 0);
        operation.DeletedCount.Should().Be(5);

        // Act - Update Progress Again
        operation.UpdateProgress(deletedCount: 10, failedCount: 0);
        operation.DeletedCount.Should().Be(10);

        // Act - Complete
        operation.Complete();
        operation.Status.Should().Be(DeleteOperationStatus.Completed);
    }

    [TestMethod]
    public void CompleteWorkflow_CreateToFail_ShouldFollowExpectedStates()
    {
        // Arrange & Act - Create
        var operation = DeleteOperation.Create(_worldId, _rootEntityId, RootEntityName, TestUserId);
        operation.Status.Should().Be(DeleteOperationStatus.Pending);

        // Act - Start
        operation.Start(10);
        operation.Status.Should().Be(DeleteOperationStatus.InProgress);

        // Act - Fail
        operation.Fail("Unexpected error occurred");
        operation.Status.Should().Be(DeleteOperationStatus.Failed);
        operation.ErrorDetails.Should().Be("Unexpected error occurred");
    }

    #endregion
}
