namespace LibrisMaleficarum.Api.Tests.Controllers;

using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using LibrisMaleficarum.Api.Controllers;
using LibrisMaleficarum.Api.Models.Requests;
using LibrisMaleficarum.Api.Models.Responses;
using LibrisMaleficarum.Domain.Entities;
using LibrisMaleficarum.Domain.Interfaces.Repositories;
using LibrisMaleficarum.Domain.Interfaces.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;

/// <summary>
/// Unit tests for WorldsController with mocked dependencies.
/// Tests CRUD operations, validation, and error handling.
/// </summary>
[TestClass]
[TestCategory("Unit")]
public class WorldsControllerTests
{
    private IWorldRepository _worldRepository = null!;
    private ISearchService _searchService = null!;
    private IUserContextService _userContextService = null!;
    private IValidator<CreateWorldRequest> _createValidator = null!;
    private IValidator<UpdateWorldRequest> _updateValidator = null!;
    private WorldsController _controller = null!;

    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _worldId = Guid.NewGuid();
    private const string TestOwnerId = "test-owner-id";

    [TestInitialize]
    public void Setup()
    {
        _worldRepository = Substitute.For<IWorldRepository>();
        _searchService = Substitute.For<ISearchService>();
        _userContextService = Substitute.For<IUserContextService>();
        _createValidator = Substitute.For<IValidator<CreateWorldRequest>>();
        _updateValidator = Substitute.For<IValidator<UpdateWorldRequest>>();

        _userContextService.GetCurrentUserIdAsync().Returns(_userId);

        _controller = new WorldsController(
            _worldRepository,
            _searchService,
            _userContextService,
            _createValidator,
            _updateValidator);

        // Setup controller context for header access
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
    }

    #region CreateWorld Tests

    [TestMethod]
    public async Task CreateWorld_WithValidRequest_ReturnsCreatedResult()
    {
        // Arrange
        var request = new CreateWorldRequest { Name = "Test World", Description = "Test Description" };
        var world = World.Create(_userId, request.Name, request.Description);

        _createValidator.ValidateAsync(request, Arg.Any<CancellationToken>())
            .Returns(new ValidationResult());

        _worldRepository.CreateAsync(Arg.Any<World>(), Arg.Any<CancellationToken>())
            .Returns(world);

        // Act
        var result = await _controller.CreateWorld(request, CancellationToken.None);

        // Assert
        var createdResult = result.Should().BeOfType<CreatedAtActionResult>().Subject;
        createdResult.ActionName.Should().Be(nameof(WorldsController.GetWorld));
        var response = createdResult.Value.Should().BeOfType<ApiResponse<WorldResponse>>().Subject;
        response.Data.Name.Should().Be("Test World");
        response.Data.OwnerId.Should().Be(_userId);
        response.Meta.Should().ContainKey("etag");
    }

    [TestMethod]
    public async Task CreateWorld_WithInvalidRequest_ReturnsBadRequest()
    {
        // Arrange
        var request = new CreateWorldRequest { Name = "", Description = "Test" };
        var validationFailures = new List<ValidationFailure>
        {
            new("Name", "Name is required")
        };

        _createValidator.ValidateAsync(request, Arg.Any<CancellationToken>())
            .Returns(new ValidationResult(validationFailures));

        // Act
        var result = await _controller.CreateWorld(request, CancellationToken.None);

        // Assert
        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        var errorResponse = badRequestResult.Value.Should().BeOfType<ErrorResponse>().Subject;
        errorResponse.Error.Code.Should().Be("VALIDATION_ERROR");
        errorResponse.Error.ValidationErrors.Should().HaveCount(1);
        errorResponse.Error.ValidationErrors!.First().Field.Should().Be("Name");
    }

    #endregion

    #region GetWorlds Tests

    [TestMethod]
    public async Task GetWorlds_WithDefaultParameters_ReturnsOkWithWorlds()
    {
        // Arrange
        var worlds = new List<World>
        {
            World.Create(_userId, "World 1", null),
            World.Create(_userId, "World 2", "Description 2")
        };

        _worldRepository.GetAllByOwnerAsync(_userId, 50, null, Arg.Any<CancellationToken>())
            .Returns((worlds, (string?)null));

        // Act
        var result = await _controller.GetWorlds();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<PaginatedApiResponse<WorldResponse>>().Subject;
        response.Data.Should().HaveCount(2);
        response.Data.First().Name.Should().Be("World 1");
        response.Meta.Count.Should().Be(2);
        response.Meta.NextCursor.Should().BeNull();
    }

    [TestMethod]
    public async Task GetWorlds_WithPagination_ReturnsNextCursor()
    {
        // Arrange
        var worlds = new List<World> { World.Create(_userId, "World 1", null) };
        var nextCursor = "cursor-123";

        _worldRepository.GetAllByOwnerAsync(_userId, 10, null, Arg.Any<CancellationToken>())
            .Returns((worlds, nextCursor));

        // Act
        var result = await _controller.GetWorlds(limit: 10);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<PaginatedApiResponse<WorldResponse>>().Subject;
        response.Meta.NextCursor.Should().Be(nextCursor);
    }

    [TestMethod]
    public async Task GetWorlds_WithCustomLimit_PassesLimitToRepository()
    {
        // Arrange
        _worldRepository.GetAllByOwnerAsync(_userId, 25, null, Arg.Any<CancellationToken>())
            .Returns((new List<World>(), (string?)null));

        // Act
        await _controller.GetWorlds(limit: 25);

        // Assert
        await _worldRepository.Received(1).GetAllByOwnerAsync(_userId, 25, null, Arg.Any<CancellationToken>());
    }

    #endregion

    #region GetWorld Tests

    [TestMethod]
    public async Task GetWorld_WithValidId_ReturnsOkWithWorld()
    {
        // Arrange
        var world = World.Create(_userId, "Test World", "Test Description");

        _worldRepository.GetByIdAsync(_worldId, Arg.Any<CancellationToken>())
            .Returns(world);

        // Act
        var result = await _controller.GetWorld(_worldId, CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<ApiResponse<WorldResponse>>().Subject;
        response.Data.Name.Should().Be("Test World");
        response.Data.Description.Should().Be("Test Description");
        response.Meta.Should().ContainKey("etag");

        _controller.Response.Headers.Should().ContainKey("ETag");
    }

    [TestMethod]
    public async Task GetWorld_WithNonexistentId_ReturnsNotFound()
    {
        // Arrange
        _worldRepository.GetByIdAsync(_worldId, Arg.Any<CancellationToken>())
            .Returns((World?)null);

        // Act
        var result = await _controller.GetWorld(_worldId, CancellationToken.None);

        // Assert
        var notFoundResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
        var errorResponse = notFoundResult.Value.Should().BeOfType<ErrorResponse>().Subject;
        errorResponse.Error.Code.Should().Be("WORLD_NOT_FOUND");
        errorResponse.Error.Message.Should().Contain(_worldId.ToString());
    }

    #endregion

    #region UpdateWorld Tests

    [TestMethod]
    public async Task UpdateWorld_WithValidRequest_ReturnsOkWithUpdatedWorld()
    {
        // Arrange
        var request = new UpdateWorldRequest { Name = "Updated Name", Description = "Updated Description" };
        var existingWorld = World.Create(_userId, "Original Name", "Original Description");
        var etag = "test-etag";

        _updateValidator.ValidateAsync(request, Arg.Any<CancellationToken>())
            .Returns(new ValidationResult());

        _worldRepository.GetByIdAsync(_worldId, Arg.Any<CancellationToken>())
            .Returns(existingWorld);

        _worldRepository.UpdateAsync(Arg.Any<World>(), etag, Arg.Any<CancellationToken>())
            .Returns(existingWorld);

        _controller.Request.Headers.IfMatch = $"\"{etag}\"";

        // Act
        var result = await _controller.UpdateWorld(_worldId, request, CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<ApiResponse<WorldResponse>>().Subject;
        response.Data.Should().NotBeNull();
        response.Meta.Should().ContainKey("etag");

        await _worldRepository.Received(1).UpdateAsync(Arg.Any<World>(), etag, Arg.Any<CancellationToken>());
    }

    [TestMethod]
    public async Task UpdateWorld_WithInvalidRequest_ReturnsBadRequest()
    {
        // Arrange
        var request = new UpdateWorldRequest { Name = "", Description = "Test" };
        var validationFailures = new List<ValidationFailure>
        {
            new("Name", "Name is required")
        };

        _updateValidator.ValidateAsync(request, Arg.Any<CancellationToken>())
            .Returns(new ValidationResult(validationFailures));

        // Act
        var result = await _controller.UpdateWorld(_worldId, request, CancellationToken.None);

        // Assert
        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        var errorResponse = badRequestResult.Value.Should().BeOfType<ErrorResponse>().Subject;
        errorResponse.Error.Code.Should().Be("VALIDATION_ERROR");
        errorResponse.Error.ValidationErrors.Should().HaveCount(1);
    }

    [TestMethod]
    public async Task UpdateWorld_WithNonexistentWorld_ReturnsNotFound()
    {
        // Arrange
        var request = new UpdateWorldRequest { Name = "Updated Name", Description = "Updated" };

        _updateValidator.ValidateAsync(request, Arg.Any<CancellationToken>())
            .Returns(new ValidationResult());

        _worldRepository.GetByIdAsync(_worldId, Arg.Any<CancellationToken>())
            .Returns((World?)null);

        // Act
        var result = await _controller.UpdateWorld(_worldId, request, CancellationToken.None);

        // Assert
        var notFoundResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
        var errorResponse = notFoundResult.Value.Should().BeOfType<ErrorResponse>().Subject;
        errorResponse.Error.Code.Should().Be("WORLD_NOT_FOUND");
    }

    #endregion

    #region DeleteWorld Tests

    [TestMethod]
    public async Task DeleteWorld_WithValidId_ReturnsNoContent()
    {
        // Arrange
        _worldRepository.DeleteAsync(_worldId, Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.DeleteWorld(_worldId, CancellationToken.None);

        // Assert
        result.Should().BeOfType<NoContentResult>();
        await _worldRepository.Received(1).DeleteAsync(_worldId, Arg.Any<CancellationToken>());
    }

    #endregion

    #region SearchEntities Tests

    [TestMethod]
    public async Task SearchEntities_WithValidQuery_ReturnsOkWithResults()
    {
        // Arrange
        var query = "test";
        var entities = new List<WorldEntity>
        {
            WorldEntity.Create(_worldId, Domain.ValueObjects.EntityType.Character, "Test Character", TestOwnerId, "Description")
        };

        _searchService.SearchEntitiesAsync(_worldId, query, null, null, 50, null, Arg.Any<CancellationToken>())
            .Returns((entities, (string?)null));

        // Act
        var result = await _controller.SearchEntities(_worldId, query);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<PaginatedApiResponse<EntityResponse>>().Subject;
        response.Data.Should().HaveCount(1);
        response.Data.First().Name.Should().Be("Test Character");
        response.Meta.Count.Should().Be(1);
    }

    [TestMethod]
    public async Task SearchEntities_WithEmptyQuery_ReturnsBadRequest()
    {
        // Act
        var result = await _controller.SearchEntities(_worldId, "");

        // Assert
        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        var errorResponse = badRequestResult.Value.Should().BeOfType<ErrorResponse>().Subject;
        errorResponse.Error.Code.Should().Be("INVALID_SEARCH_QUERY");
        errorResponse.Error.Message.Should().Contain("required");
    }

    [TestMethod]
    public async Task SearchEntities_WithNullQuery_ReturnsBadRequest()
    {
        // Act
        var result = await _controller.SearchEntities(_worldId, null!);

        // Assert
        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        var errorResponse = badRequestResult.Value.Should().BeOfType<ErrorResponse>().Subject;
        errorResponse.Error.Code.Should().Be("INVALID_SEARCH_QUERY");
    }

    [TestMethod]
    public async Task SearchEntities_WithSortingParameters_PassesToSearchService()
    {
        // Arrange
        var query = "test";
        _searchService.SearchEntitiesAsync(_worldId, query, "name", "asc", 100, "cursor", Arg.Any<CancellationToken>())
            .Returns((new List<WorldEntity>(), (string?)null));

        // Act
        await _controller.SearchEntities(_worldId, query, "name", "asc", 100, "cursor");

        // Assert
        await _searchService.Received(1).SearchEntitiesAsync(
            _worldId, query, "name", "asc", 100, "cursor", Arg.Any<CancellationToken>());
    }

    #endregion
}
