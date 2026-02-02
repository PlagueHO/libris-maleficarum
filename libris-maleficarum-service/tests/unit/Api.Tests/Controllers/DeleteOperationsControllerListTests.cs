namespace LibrisMaleficarum.Api.Tests.Controllers;

using FluentAssertions;
using LibrisMaleficarum.Api.Controllers;
using LibrisMaleficarum.Api.Models.Responses;
using LibrisMaleficarum.Domain.Entities;
using LibrisMaleficarum.Domain.Interfaces.Repositories;
using LibrisMaleficarum.Domain.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;

/// <summary>
/// Unit tests for DeleteOperationsController list operations endpoint.
/// </summary>
[TestClass]
[TestCategory("Unit")]
public class DeleteOperationsControllerListTests
{
    private IDeleteService _deleteService = null!;
    private IWorldRepository _worldRepository = null!;
    private DeleteOperationsController _controller = null!;

    [TestInitialize]
    public void Setup()
    {
        _deleteService = Substitute.For<IDeleteService>();
        _worldRepository = Substitute.For<IWorldRepository>();
        _controller = new DeleteOperationsController(_deleteService, _worldRepository);

        // Setup default world ownership (authorized) for all tests
        var userId = Guid.NewGuid();
        var world = World.Create(userId, "Test World", null);
        _worldRepository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(world);
    }

    [TestMethod]
    public async Task ListDeleteOperations_WithValidWorld_ReturnsOperations()
    {
        // Arrange
        var worldId = Guid.NewGuid();
        var operations = new List<DeleteOperation>
        {
            CreateDeleteOperation(worldId, createdAt: DateTime.UtcNow.AddMinutes(-5)),
            CreateDeleteOperation(worldId, createdAt: DateTime.UtcNow.AddMinutes(-10)),
            CreateDeleteOperation(worldId, createdAt: DateTime.UtcNow.AddMinutes(-15))
        };

        _deleteService
            .ListRecentOperationsAsync(worldId, Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(operations);

        // Act
        var result = await _controller.ListDeleteOperations(worldId, limit: 20, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = (OkObjectResult)result;
        var response = okResult.Value.Should().BeOfType<ApiResponse<IEnumerable<DeleteOperationResponse>>>().Subject;

        response.Data.Should().HaveCount(3);
        response.Meta.Should().ContainKey("count");
        response.Meta!["count"].Should().Be(3);
    }

    [TestMethod]
    public async Task ListDeleteOperations_WithDefaultLimit_Uses20()
    {
        // Arrange
        var worldId = Guid.NewGuid();
        _deleteService
            .ListRecentOperationsAsync(worldId, 20, Arg.Any<CancellationToken>())
            .Returns(new List<DeleteOperation>());

        // Act
        await _controller.ListDeleteOperations(worldId, cancellationToken: CancellationToken.None);

        // Assert
        await _deleteService.Received(1).ListRecentOperationsAsync(worldId, 20, Arg.Any<CancellationToken>());
    }

    [TestMethod]
    public async Task ListDeleteOperations_WithCustomLimit_UsesProvidedLimit()
    {
        // Arrange
        var worldId = Guid.NewGuid();
        var customLimit = 50;
        _deleteService
            .ListRecentOperationsAsync(worldId, customLimit, Arg.Any<CancellationToken>())
            .Returns(new List<DeleteOperation>());

        // Act
        await _controller.ListDeleteOperations(worldId, limit: customLimit, cancellationToken: CancellationToken.None);

        // Assert
        await _deleteService.Received(1).ListRecentOperationsAsync(worldId, customLimit, Arg.Any<CancellationToken>());
    }

    [TestMethod]
    public async Task ListDeleteOperations_WithNoOperations_ReturnsEmptyList()
    {
        // Arrange
        var worldId = Guid.NewGuid();
        _deleteService
            .ListRecentOperationsAsync(worldId, Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new List<DeleteOperation>());

        // Act
        var result = await _controller.ListDeleteOperations(worldId, cancellationToken: CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = (OkObjectResult)result;
        var response = okResult.Value.Should().BeOfType<ApiResponse<IEnumerable<DeleteOperationResponse>>>().Subject;

        response.Data.Should().BeEmpty();
        response.Meta.Should().ContainKey("count");
        response.Meta!["count"].Should().Be(0);
    }

    [TestMethod]
    public async Task ListDeleteOperations_MapsAllProperties_Correctly()
    {
        // Arrange
        var worldId = Guid.NewGuid();
        var operationId = Guid.NewGuid();
        var rootEntityId = Guid.NewGuid();
        var failedId1 = Guid.NewGuid();
        var failedId2 = Guid.NewGuid();

        var operation = CreateDeleteOperation(
            worldId,
            operationId: operationId,
            rootEntityId: rootEntityId,
            status: DeleteOperationStatus.Partial,
            totalEntities: 10,
            deletedCount: 8,
            failedCount: 2,
            failedEntityIds: [failedId1, failedId2]);

        _deleteService
            .ListRecentOperationsAsync(worldId, Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new List<DeleteOperation> { operation });

        // Act
        var result = await _controller.ListDeleteOperations(worldId, cancellationToken: CancellationToken.None);

        // Assert
        var okResult = (OkObjectResult)result;
        var response = okResult.Value.Should().BeOfType<ApiResponse<IEnumerable<DeleteOperationResponse>>>().Subject;
        var dto = response.Data.First();

        dto.Id.Should().Be(operationId);
        dto.WorldId.Should().Be(worldId);
        dto.RootEntityId.Should().Be(rootEntityId);
        dto.Status.Should().Be("partial");
        dto.TotalEntities.Should().Be(10);
        dto.DeletedCount.Should().Be(8);
        dto.FailedCount.Should().Be(2);
        dto.FailedEntityIds.Should().HaveCount(2);
        dto.FailedEntityIds.Should().Contain(failedId1);
        dto.FailedEntityIds.Should().Contain(failedId2);
    }

    private static DeleteOperation CreateDeleteOperation(
        Guid worldId,
        Guid? operationId = null,
        Guid? rootEntityId = null,
        DeleteOperationStatus status = DeleteOperationStatus.Completed,
        DateTime? createdAt = null,
        int totalEntities = 5,
        int deletedCount = 5,
        int failedCount = 0,
        List<Guid>? failedEntityIds = null)
    {
        var operation = DeleteOperation.Create(
            worldId,
            rootEntityId ?? Guid.NewGuid(),
            "Test Entity",
            "user123",
            cascade: true);

        // Use reflection to set private Id property if specified
        if (operationId.HasValue)
        {
            var idProperty = typeof(DeleteOperation).GetProperty(nameof(DeleteOperation.Id))!;
            idProperty.SetValue(operation, operationId.Value);
        }

        // Set CreatedAt if specified
        if (createdAt.HasValue)
        {
            var createdAtProperty = typeof(DeleteOperation).GetProperty(nameof(DeleteOperation.CreatedAt))!;
            createdAtProperty.SetValue(operation, createdAt.Value);
        }

        operation.Start(totalEntities);
        operation.UpdateProgress(deletedCount, failedCount, failedIds: failedEntityIds);

        // Set status
        var statusProperty = typeof(DeleteOperation).GetProperty(nameof(DeleteOperation.Status))!;
        statusProperty.SetValue(operation, status);

        if (status == DeleteOperationStatus.Completed || status == DeleteOperationStatus.Partial || status == DeleteOperationStatus.Failed)
        {
            operation.Complete();
        }

        return operation;
    }
}
