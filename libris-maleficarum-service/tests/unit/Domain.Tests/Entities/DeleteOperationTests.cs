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
