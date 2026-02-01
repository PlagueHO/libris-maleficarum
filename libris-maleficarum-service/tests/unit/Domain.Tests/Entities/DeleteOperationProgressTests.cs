namespace LibrisMaleficarum.Domain.Tests.Entities;

using LibrisMaleficarum.Domain.Entities;

/// <summary>
/// Unit tests for DeleteOperation progress monitoring methods (User Story 3).
/// Tests UpdateProgress, AddFailedEntity, Complete, and CompletedDate.
/// </summary>
[TestClass]
public class DeleteOperationProgressTests
{
    [TestMethod]
    public void UpdateProgress_WithIncrementalCounts_UpdatesDeletedAndFailedCounts()
    {
        // Arrange
        var operation = DeleteOperation.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Test Entity",
            "user123",
            cascade: true);

        operation.Start(100);

        // Act
        operation.UpdateProgress(deletedCount: 10, failedCount: 0);

        // Assert
        operation.DeletedCount.Should().Be(10);
        operation.FailedCount.Should().Be(0);
    }

    [TestMethod]
    public void UpdateProgress_WithFailedIds_UpdatesFailedEntityIdsList()
    {
        // Arrange
        var operation = DeleteOperation.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Test Entity",
            "user123",
            cascade: true);

        operation.Start(100);

        var failedId1 = Guid.NewGuid();
        var failedId2 = Guid.NewGuid();
        var failedIds = new List<Guid> { failedId1, failedId2 };

        // Act
        operation.UpdateProgress(deletedCount: 98, failedCount: 2, failedIds: failedIds);

        // Assert
        operation.FailedEntityIds.Should().HaveCount(2);
        operation.FailedEntityIds.Should().Contain(failedId1);
        operation.FailedEntityIds.Should().Contain(failedId2);
    }

    [TestMethod]
    public void AddFailedEntity_WithNewId_AddsToFailedList()
    {
        // Arrange
        var operation = DeleteOperation.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Test Entity",
            "user123",
            cascade: true);

        operation.Start(10);

        var failedId = Guid.NewGuid();

        // Act
        operation.AddFailedEntity(failedId);

        // Assert
        operation.FailedEntityIds.Should().Contain(failedId);
        operation.FailedCount.Should().Be(1);
        operation.HasFailures.Should().BeTrue();
    }

    [TestMethod]
    public void AddFailedEntity_WithDuplicateId_DoesNotDuplicate()
    {
        // Arrange
        var operation = DeleteOperation.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Test Entity",
            "user123",
            cascade: true);

        operation.Start(10);

        var failedId = Guid.NewGuid();

        // Act
        operation.AddFailedEntity(failedId);
        operation.AddFailedEntity(failedId); // Add same ID again

        // Assert
        operation.FailedEntityIds.Should().HaveCount(1, "Duplicate IDs should not be added");
        operation.FailedCount.Should().Be(1);
    }

    [TestMethod]
    public void Complete_WithNoFailures_SetsStatusToCompleted()
    {
        // Arrange
        var operation = DeleteOperation.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Test Entity",
            "user123",
            cascade: true);

        operation.Start(10);
        operation.UpdateProgress(deletedCount: 10, failedCount: 0);

        // Act
        operation.Complete();

        // Assert
        operation.Status.Should().Be(DeleteOperationStatus.Completed);
        operation.CompletedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [TestMethod]
    public void Complete_WithPartialFailures_SetsStatusToPartial()
    {
        // Arrange
        var operation = DeleteOperation.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Test Entity",
            "user123",
            cascade: true);

        operation.Start(10);
        operation.UpdateProgress(deletedCount: 8, failedCount: 0);
        operation.AddFailedEntity(Guid.NewGuid());
        operation.AddFailedEntity(Guid.NewGuid());

        // Act
        operation.Complete();

        // Assert
        operation.Status.Should().Be(DeleteOperationStatus.Partial);
        operation.CompletedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [TestMethod]
    public void Complete_WithAllFailures_SetsStatusToFailed()
    {
        // Arrange
        var operation = DeleteOperation.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Test Entity",
            "user123",
            cascade: true);

        operation.Start(5);

        // Add all entities as failed
        for (var i = 0; i < 5; i++)
        {
            operation.AddFailedEntity(Guid.NewGuid());
        }

        operation.UpdateProgress(deletedCount: 0, failedCount: 5);

        // Act
        operation.Complete();

        // Assert
        operation.Status.Should().Be(DeleteOperationStatus.Failed);
        operation.CompletedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [TestMethod]
    public void Complete_IdempotentOperation_SetsStatusToCompleted()
    {
        // Arrange - Operation with zero total entities (already deleted entity)
        var operation = DeleteOperation.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Test Entity",
            "user123",
            cascade: true);

        operation.Start(0);

        // Act
        operation.Complete();

        // Assert
        operation.Status.Should().Be(DeleteOperationStatus.Completed);
        operation.CompletedAt.Should().NotBeNull();
    }

    [TestMethod]
    public void HasFailures_WhenNoFailures_ReturnsFalse()
    {
        // Arrange
        var operation = DeleteOperation.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Test Entity",
            "user123",
            cascade: true);

        // Act & Assert
        operation.HasFailures.Should().BeFalse();
    }

    [TestMethod]
    public void HasFailures_WhenFailuresExist_ReturnsTrue()
    {
        // Arrange
        var operation = DeleteOperation.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Test Entity",
            "user123",
            cascade: true);

        operation.AddFailedEntity(Guid.NewGuid());

        // Act & Assert
        operation.HasFailures.Should().BeTrue();
    }
}
