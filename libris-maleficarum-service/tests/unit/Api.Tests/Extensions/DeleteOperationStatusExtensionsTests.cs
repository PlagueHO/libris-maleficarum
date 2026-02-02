namespace LibrisMaleficarum.Api.Tests.Extensions;

using FluentAssertions;
using LibrisMaleficarum.Domain.Entities;
using LibrisMaleficarum.Domain.Extensions;

/// <summary>
/// Tests for DeleteOperationStatusExtensions to ensure correct API wire format.
/// </summary>
[TestClass]
[TestCategory("Unit")]
public class DeleteOperationStatusExtensionsTests
{
    [TestMethod]
    public void ToApiString_Pending_ReturnsPending()
    {
        // Arrange
        var status = DeleteOperationStatus.Pending;

        // Act
        var result = status.ToApiString();

        // Assert
        result.Should().Be("pending");
    }

    [TestMethod]
    public void ToApiString_InProgress_ReturnsInProgressWithUnderscore()
    {
        // Arrange
        var status = DeleteOperationStatus.InProgress;

        // Act
        var result = status.ToApiString();

        // Assert
        result.Should().Be("in_progress", "API contract specifies 'in_progress' with underscore");
    }

    [TestMethod]
    public void ToApiString_Completed_ReturnsCompleted()
    {
        // Arrange
        var status = DeleteOperationStatus.Completed;

        // Act
        var result = status.ToApiString();

        // Assert
        result.Should().Be("completed");
    }

    [TestMethod]
    public void ToApiString_Partial_ReturnsPartial()
    {
        // Arrange
        var status = DeleteOperationStatus.Partial;

        // Act
        var result = status.ToApiString();

        // Assert
        result.Should().Be("partial");
    }

    [TestMethod]
    public void ToApiString_Failed_ReturnsFailed()
    {
        // Arrange
        var status = DeleteOperationStatus.Failed;

        // Act
        var result = status.ToApiString();

        // Assert
        result.Should().Be("failed");
    }

    [TestMethod]
    public void ToApiString_AllStatuses_MatchApiContract()
    {
        // Arrange & Act & Assert
        var expectedMappings = new Dictionary<DeleteOperationStatus, string>
        {
            { DeleteOperationStatus.Pending, "pending" },
            { DeleteOperationStatus.InProgress, "in_progress" },
            { DeleteOperationStatus.Completed, "completed" },
            { DeleteOperationStatus.Partial, "partial" },
            { DeleteOperationStatus.Failed, "failed" }
        };

        foreach (var (status, expectedString) in expectedMappings)
        {
            status.ToApiString().Should().Be(expectedString,
                $"DeleteOperationStatus.{status} should map to '{expectedString}' per API contract");
        }
    }
}
