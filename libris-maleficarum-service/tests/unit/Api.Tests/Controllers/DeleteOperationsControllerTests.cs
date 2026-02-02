namespace LibrisMaleficarum.Api.Tests.Controllers;

using FluentAssertions;
using LibrisMaleficarum.Api.Controllers;
using LibrisMaleficarum.Api.Models.Responses;
using LibrisMaleficarum.Domain.Entities;
using LibrisMaleficarum.Domain.Exceptions;
using LibrisMaleficarum.Domain.Interfaces.Repositories;
using LibrisMaleficarum.Domain.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

/// <summary>
/// Unit tests for DeleteOperationsController.
/// Tests status polling, list operations endpoints, and authorization.
/// </summary>
[TestClass]
[TestCategory("Unit")]
public class DeleteOperationsControllerTests
{
    private IDeleteService _deleteService = null!;
    private IWorldRepository _worldRepository = null!;
    private DeleteOperationsController _controller = null!;

    private readonly Guid _worldId = Guid.NewGuid();
    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _operationId = Guid.NewGuid();
    private readonly Guid _entityId = Guid.NewGuid();
    private const string TestUserId = "test-user-id";

    [TestInitialize]
    public void Setup()
    {
        _deleteService = Substitute.For<IDeleteService>();
        _worldRepository = Substitute.For<IWorldRepository>();
        _controller = new DeleteOperationsController(_deleteService, _worldRepository);

        // Setup default world ownership (authorized)
        var world = World.Create(_userId, "Test World", null);
        _worldRepository.GetByIdAsync(_worldId, Arg.Any<CancellationToken>())
            .Returns(world);
    }

    #region Constructor Tests

    [TestMethod]
    public void Constructor_WithNullDeleteService_ShouldThrowArgumentNullException()
    {
        // Act
        var action = () => new DeleteOperationsController(null!, _worldRepository);

        // Assert
        action.Should().Throw<ArgumentNullException>()
            .And.ParamName.Should().Be("deleteService");
    }

    [TestMethod]
    public void Constructor_WithNullWorldRepository_ShouldThrowArgumentNullException()
    {
        // Act
        var action = () => new DeleteOperationsController(_deleteService, null!);

        // Assert
        action.Should().Throw<ArgumentNullException>()
            .And.ParamName.Should().Be("worldRepository");
    }

    #endregion

    #region GetDeleteOperation Tests

    [TestMethod]
    public async Task GetDeleteOperation_WithExistingOperation_ShouldReturn200WithResponse()
    {
        // Arrange
        var operation = DeleteOperation.Create(_worldId, _entityId, "Test Entity", TestUserId);
        operation.Start(10);
        operation.UpdateProgress(deletedCount: 5, failedCount: 0);

        _deleteService.GetOperationStatusAsync(_worldId, _operationId, Arg.Any<CancellationToken>())
            .Returns(operation);

        // Act
        var result = await _controller.GetDeleteOperation(_worldId, _operationId, CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<DeleteOperationResponse>>().Subject;
        var response = apiResponse.Data;

        response.Should().NotBeNull();
        response.Id.Should().Be(operation.Id);
        response.WorldId.Should().Be(_worldId);
        response.RootEntityId.Should().Be(_entityId);
        response.RootEntityName.Should().Be("Test Entity");
        response.Status.Should().Be("in_progress");
        response.TotalEntities.Should().Be(10);
        response.DeletedCount.Should().Be(5);
        response.FailedCount.Should().Be(0);
    }

    [TestMethod]
    public async Task GetDeleteOperation_WithPendingStatus_ShouldReturnPendingInLowercase()
    {
        // Arrange
        var operation = DeleteOperation.Create(_worldId, _entityId, "Test Entity", TestUserId);

        _deleteService.GetOperationStatusAsync(_worldId, _operationId, Arg.Any<CancellationToken>())
            .Returns(operation);

        // Act
        var result = await _controller.GetDeleteOperation(_worldId, _operationId, CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<DeleteOperationResponse>>().Subject;
        apiResponse.Data.Status.Should().Be("pending");
    }

    [TestMethod]
    public async Task GetDeleteOperation_WithCompletedStatus_ShouldReturnCompletedInLowercase()
    {
        // Arrange
        var operation = DeleteOperation.Create(_worldId, _entityId, "Test Entity", TestUserId);
        operation.Start(5);
        operation.UpdateProgress(deletedCount: 5, failedCount: 0);
        operation.Complete();

        _deleteService.GetOperationStatusAsync(_worldId, _operationId, Arg.Any<CancellationToken>())
            .Returns(operation);

        // Act
        var result = await _controller.GetDeleteOperation(_worldId, _operationId, CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<DeleteOperationResponse>>().Subject;
        apiResponse.Data.Status.Should().Be("completed");
    }

    [TestMethod]
    public async Task GetDeleteOperation_WithFailedStatus_ShouldReturnFailedInLowercase()
    {
        // Arrange
        var operation = DeleteOperation.Create(_worldId, _entityId, "Test Entity", TestUserId);
        operation.Start(5);
        operation.Fail("Test error");

        _deleteService.GetOperationStatusAsync(_worldId, _operationId, Arg.Any<CancellationToken>())
            .Returns(operation);

        // Act
        var result = await _controller.GetDeleteOperation(_worldId, _operationId, CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<DeleteOperationResponse>>().Subject;
        apiResponse.Data.Status.Should().Be("failed");
        apiResponse.Data.ErrorDetails.Should().Be("Test error");
    }

    [TestMethod]
    public async Task GetDeleteOperation_WithNonExistentOperation_ShouldReturn404()
    {
        // Arrange
        _deleteService.GetOperationStatusAsync(_worldId, _operationId, Arg.Any<CancellationToken>())
            .Returns((DeleteOperation?)null);

        // Act
        var result = await _controller.GetDeleteOperation(_worldId, _operationId, CancellationToken.None);

        // Assert
        var notFoundResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
        var errorResponse = notFoundResult.Value.Should().BeOfType<ErrorResponse>().Subject;
        errorResponse.Error.Code.Should().Be("OPERATION_NOT_FOUND");
        errorResponse.Error.Message.Should().Contain(_operationId.ToString());
    }

    [TestMethod]
    public async Task GetDeleteOperation_WithFailedEntityIds_ShouldIncludeFailedIds()
    {
        // Arrange
        var failedIds = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() };
        var operation = DeleteOperation.Create(_worldId, _entityId, "Test Entity", TestUserId);
        operation.Start(10);
        operation.UpdateProgress(deletedCount: 8, failedCount: 2, failedIds: failedIds);
        operation.Complete();

        _deleteService.GetOperationStatusAsync(_worldId, _operationId, Arg.Any<CancellationToken>())
            .Returns(operation);

        // Act
        var result = await _controller.GetDeleteOperation(_worldId, _operationId, CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<DeleteOperationResponse>>().Subject;
        apiResponse.Data.FailedEntityIds.Should().BeEquivalentTo(failedIds);
        apiResponse.Data.Status.Should().Be("partial");
    }

    [TestMethod]
    public async Task GetDeleteOperation_WithCascadeTrue_ShouldReturnCascadeFlag()
    {
        // Arrange
        var operation = DeleteOperation.Create(_worldId, _entityId, "Test Entity", TestUserId, cascade: true);

        _deleteService.GetOperationStatusAsync(_worldId, _operationId, Arg.Any<CancellationToken>())
            .Returns(operation);

        // Act
        var result = await _controller.GetDeleteOperation(_worldId, _operationId, CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<DeleteOperationResponse>>().Subject;
        apiResponse.Data.Cascade.Should().BeTrue();
    }

    [TestMethod]
    public async Task GetDeleteOperation_WithUnauthorizedWorld_ShouldThrowUnauthorizedWorldAccessException()
    {
        // Arrange
        _worldRepository.GetByIdAsync(_worldId, Arg.Any<CancellationToken>())
            .Throws(new UnauthorizedWorldAccessException(_worldId, _userId));

        // Act
        var action = async () => await _controller.GetDeleteOperation(_worldId, _operationId, CancellationToken.None);

        // Assert
        await action.Should().ThrowAsync<UnauthorizedWorldAccessException>();
    }

    #endregion

    #region ListDeleteOperations Tests

    [TestMethod]
    public async Task ListDeleteOperations_WithMultipleOperations_ShouldReturn200WithList()
    {
        // Arrange
        var operation1 = DeleteOperation.Create(_worldId, Guid.NewGuid(), "Entity 1", TestUserId);
        var operation2 = DeleteOperation.Create(_worldId, Guid.NewGuid(), "Entity 2", TestUserId);
        var operation3 = DeleteOperation.Create(_worldId, Guid.NewGuid(), "Entity 3", TestUserId);

        var operations = new List<DeleteOperation> { operation1, operation2, operation3 };

        _deleteService.ListRecentOperationsAsync(_worldId, 20, Arg.Any<CancellationToken>())
            .Returns(operations);

        // Act
        var result = await _controller.ListDeleteOperations(_worldId, limit: 20, CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<IEnumerable<DeleteOperationResponse>>>().Subject;
        apiResponse.Data.Should().HaveCount(3);

        var meta = apiResponse.Meta;
        meta.Should().NotBeNull();
        meta!.Should().ContainKey("count");
        meta!["count"].Should().Be(3);
    }

    [TestMethod]
    public async Task ListDeleteOperations_WithNoOperations_ShouldReturn200WithEmptyList()
    {
        // Arrange
        var operations = new List<DeleteOperation>();

        _deleteService.ListRecentOperationsAsync(_worldId, 20, Arg.Any<CancellationToken>())
            .Returns(operations);

        // Act
        var result = await _controller.ListDeleteOperations(_worldId, limit: 20, CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<IEnumerable<DeleteOperationResponse>>>().Subject;
        apiResponse.Data.Should().BeEmpty();

        var meta = apiResponse.Meta;
        meta.Should().NotBeNull();
        meta!["count"].Should().Be(0);
    }

    [TestMethod]
    public async Task ListDeleteOperations_WithCustomLimit_ShouldPassLimitToService()
    {
        // Arrange
        var operations = new List<DeleteOperation>();

        _deleteService.ListRecentOperationsAsync(_worldId, 50, Arg.Any<CancellationToken>())
            .Returns(operations);

        // Act
        await _controller.ListDeleteOperations(_worldId, limit: 50, CancellationToken.None);

        // Assert
        await _deleteService.Received(1).ListRecentOperationsAsync(_worldId, 50, Arg.Any<CancellationToken>());
    }

    [TestMethod]
    public async Task ListDeleteOperations_WithDefaultLimit_ShouldUse20()
    {
        // Arrange
        var operations = new List<DeleteOperation>();

        _deleteService.ListRecentOperationsAsync(_worldId, 20, Arg.Any<CancellationToken>())
            .Returns(operations);

        // Act
        await _controller.ListDeleteOperations(_worldId, cancellationToken: CancellationToken.None);

        // Assert
        await _deleteService.Received(1).ListRecentOperationsAsync(_worldId, 20, Arg.Any<CancellationToken>());
    }

    [TestMethod]
    public async Task ListDeleteOperations_ShouldReturnOperationsInCorrectFormat()
    {
        // Arrange
        var operation = DeleteOperation.Create(_worldId, _entityId, "Test Entity", TestUserId, cascade: true);
        operation.Start(10);
        operation.UpdateProgress(deletedCount: 5, failedCount: 1);

        var operations = new List<DeleteOperation> { operation };

        _deleteService.ListRecentOperationsAsync(_worldId, 20, Arg.Any<CancellationToken>())
            .Returns(operations);

        // Act
        var result = await _controller.ListDeleteOperations(_worldId, limit: 20, CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<IEnumerable<DeleteOperationResponse>>>().Subject;
        var firstOp = apiResponse.Data.First();

        firstOp.Id.Should().Be(operation.Id);
        firstOp.RootEntityName.Should().Be("Test Entity");
        firstOp.Status.Should().Be("in_progress");
        firstOp.TotalEntities.Should().Be(10);
        firstOp.DeletedCount.Should().Be(5);
        firstOp.FailedCount.Should().Be(1);
        firstOp.Cascade.Should().BeTrue();
    }

    [TestMethod]
    public async Task ListDeleteOperations_WithUnauthorizedWorld_ShouldThrowUnauthorizedWorldAccessException()
    {
        // Arrange
        _worldRepository.GetByIdAsync(_worldId, Arg.Any<CancellationToken>())
            .Throws(new UnauthorizedWorldAccessException(_worldId, _userId));

        // Act
        var action = async () => await _controller.ListDeleteOperations(_worldId, limit: 20, CancellationToken.None);

        // Assert
        await action.Should().ThrowAsync<UnauthorizedWorldAccessException>();
    }

    #endregion
}
