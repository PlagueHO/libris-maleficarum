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
using LibrisMaleficarum.Domain.ValueObjects;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;

/// <summary>
/// Unit tests for EntitiesController with mocked dependencies.
/// Tests CRUD operations, hierarchy management, validation, and error handling.
/// </summary>
[TestClass]
public class EntitiesControllerTests
{
    private IWorldEntityRepository _entityRepository = null!;
    private ISearchService _searchService = null!;
    private IWorldRepository _worldRepository = null!;
    private IUserContextService _userContextService = null!;
    private IValidator<CreateEntityRequest> _createValidator = null!;
    private IValidator<UpdateEntityRequest> _updateValidator = null!;
    private EntitiesController _controller = null!;

    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _worldId = Guid.NewGuid();
    private readonly Guid _entityId = Guid.NewGuid();
    private readonly Guid _parentId = Guid.NewGuid();

    [TestInitialize]
    public void Setup()
    {
        _entityRepository = Substitute.For<IWorldEntityRepository>();
        _searchService = Substitute.For<ISearchService>();
        _worldRepository = Substitute.For<IWorldRepository>();
        _userContextService = Substitute.For<IUserContextService>();
        _createValidator = Substitute.For<IValidator<CreateEntityRequest>>();
        _updateValidator = Substitute.For<IValidator<UpdateEntityRequest>>();

        _userContextService.GetCurrentUserIdAsync().Returns(_userId);

        _controller = new EntitiesController(
            _entityRepository,
            _searchService,
            _worldRepository,
            _userContextService,
            _createValidator,
            _updateValidator);

        // Setup controller context for header access
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
    }

    #region CreateEntity Tests

    [TestMethod]
    public async Task CreateEntity_WithValidRequest_ReturnsCreatedResult()
    {
        // Arrange
        var request = new CreateEntityRequest
        {
            EntityType = EntityType.Character,
            Name = "Test Character",
            Description = "Test Description"
        };
        var entity = WorldEntity.Create(_worldId, request.EntityType, request.Name, request.Description);

        _createValidator.ValidateAsync(request, Arg.Any<CancellationToken>())
            .Returns(new ValidationResult());

        _entityRepository.CreateAsync(Arg.Any<WorldEntity>(), Arg.Any<CancellationToken>())
            .Returns(entity);

        // Act
        var result = await _controller.CreateEntity(_worldId, request, CancellationToken.None);

        // Assert
        var createdResult = result.Should().BeOfType<CreatedAtActionResult>().Subject;
        createdResult.ActionName.Should().Be(nameof(EntitiesController.GetEntity));
        var response = createdResult.Value.Should().BeOfType<ApiResponse<EntityResponse>>().Subject;
        response.Data.Name.Should().Be("Test Character");
        response.Data.EntityType.Should().Be(EntityType.Character);
        response.Meta.Should().ContainKey("etag");
    }

    [TestMethod]
    public async Task CreateEntity_WithParentId_CreatesChildEntity()
    {
        // Arrange
        var request = new CreateEntityRequest
        {
            EntityType = EntityType.Location,
            Name = "Child Location",
            ParentId = _parentId
        };

        _createValidator.ValidateAsync(request, Arg.Any<CancellationToken>())
            .Returns(new ValidationResult());

        _entityRepository.CreateAsync(
            Arg.Is<WorldEntity>(e => e.ParentId == _parentId),
            Arg.Any<CancellationToken>())
            .Returns(args => args.Arg<WorldEntity>());

        // Act
        var result = await _controller.CreateEntity(_worldId, request, CancellationToken.None);

        // Assert
        var createdResult = result.Should().BeOfType<CreatedAtActionResult>().Subject;
        var response = createdResult.Value.Should().BeOfType<ApiResponse<EntityResponse>>().Subject;
        response.Data.ParentId.Should().Be(_parentId);
    }

    [TestMethod]
    public async Task CreateEntity_WithInvalidRequest_ReturnsBadRequest()
    {
        // Arrange
        var request = new CreateEntityRequest { EntityType = EntityType.Character, Name = "" };
        var validationFailures = new List<ValidationFailure>
        {
            new("Name", "Name is required")
        };

        _createValidator.ValidateAsync(request, Arg.Any<CancellationToken>())
            .Returns(new ValidationResult(validationFailures));

        // Act
        var result = await _controller.CreateEntity(_worldId, request, CancellationToken.None);

        // Assert
        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        var errorResponse = badRequestResult.Value.Should().BeOfType<ErrorResponse>().Subject;
        errorResponse.Error.Code.Should().Be("VALIDATION_ERROR");
        errorResponse.Error.ValidationErrors.Should().HaveCount(1);
    }

    #endregion

    #region GetEntities Tests

    [TestMethod]
    public async Task GetEntities_WithDefaultParameters_ReturnsOkWithEntities()
    {
        // Arrange
        var entities = new List<WorldEntity>
        {
            WorldEntity.Create(_worldId, EntityType.Character, "Entity 1", "Description 1"),
            WorldEntity.Create(_worldId, EntityType.Location, "Entity 2", null)
        };

        _entityRepository.GetAllByWorldAsync(_worldId, null, null, 50, null, Arg.Any<CancellationToken>())
            .Returns((entities, (string?)null));

        // Act
        var result = await _controller.GetEntities(_worldId);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<PaginatedApiResponse<EntityResponse>>().Subject;
        response.Data.Should().HaveCount(2);
        response.Data.First().Name.Should().Be("Entity 1");
        response.Meta.Count.Should().Be(2);
        response.Meta.NextCursor.Should().BeNull();
    }

    [TestMethod]
    public async Task GetEntities_WithTypeFilter_PassesTypeToRepository()
    {
        // Arrange
        _entityRepository.GetAllByWorldAsync(_worldId, EntityType.Character, null, 50, null, Arg.Any<CancellationToken>())
            .Returns((new List<WorldEntity>(), (string?)null));

        // Act
        await _controller.GetEntities(_worldId, type: EntityType.Character);

        // Assert
        await _entityRepository.Received(1).GetAllByWorldAsync(
            _worldId, EntityType.Character, null, 50, null, Arg.Any<CancellationToken>());
    }

    [TestMethod]
    public async Task GetEntities_WithTagsFilter_PassesParsedTagsToRepository()
    {
        // Arrange
        _entityRepository.GetAllByWorldAsync(
            _worldId,
            null,
            Arg.Is<List<string>>(tags => tags.Count == 2 && tags.Contains("hero") && tags.Contains("warrior")),
            50,
            null,
            Arg.Any<CancellationToken>())
            .Returns((new List<WorldEntity>(), (string?)null));

        // Act
        await _controller.GetEntities(_worldId, tags: "hero,warrior");

        // Assert
        await _entityRepository.Received(1).GetAllByWorldAsync(
            _worldId,
            null,
            Arg.Is<List<string>>(tags => tags.Count == 2),
            50,
            null,
            Arg.Any<CancellationToken>());
    }

    #endregion

    #region GetEntity Tests

    [TestMethod]
    public async Task GetEntity_WithValidId_ReturnsOkWithEntity()
    {
        // Arrange
        var entity = WorldEntity.Create(_worldId, EntityType.Item, "Test Item", "Test Description");

        _entityRepository.GetByIdAsync(_worldId, _entityId, Arg.Any<CancellationToken>())
            .Returns(entity);

        // Act
        var result = await _controller.GetEntity(_worldId, _entityId, CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<ApiResponse<EntityResponse>>().Subject;
        response.Data.Name.Should().Be("Test Item");
        response.Data.EntityType.Should().Be(EntityType.Item);

        _controller.Response.Headers.Should().ContainKey("ETag");
    }

    [TestMethod]
    public async Task GetEntity_WithNonexistentId_ReturnsNotFound()
    {
        // Arrange
        _entityRepository.GetByIdAsync(_worldId, _entityId, Arg.Any<CancellationToken>())
            .Returns((WorldEntity?)null);

        // Act
        var result = await _controller.GetEntity(_worldId, _entityId, CancellationToken.None);

        // Assert
        var notFoundResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
        var errorResponse = notFoundResult.Value.Should().BeOfType<ErrorResponse>().Subject;
        errorResponse.Error.Code.Should().Be("ENTITY_NOT_FOUND");
        errorResponse.Error.Message.Should().Contain(_entityId.ToString());
    }

    #endregion

    #region UpdateEntity Tests

    [TestMethod]
    public async Task UpdateEntity_WithValidRequest_ReturnsOkWithUpdatedEntity()
    {
        // Arrange
        var request = new UpdateEntityRequest
        {
            Name = "Updated Name",
            Description = "Updated Description",
            EntityType = EntityType.Character
        };
        var existingEntity = WorldEntity.Create(_worldId, EntityType.Character, "Original Name", "Original Description");
        var etag = "test-etag";

        _updateValidator.ValidateAsync(request, Arg.Any<CancellationToken>())
            .Returns(new ValidationResult());

        _entityRepository.GetByIdAsync(_worldId, _entityId, Arg.Any<CancellationToken>())
            .Returns(existingEntity);

        _entityRepository.UpdateAsync(Arg.Any<WorldEntity>(), etag, Arg.Any<CancellationToken>())
            .Returns(existingEntity);

        _controller.Request.Headers["If-Match"] = etag;

        // Act
        var result = await _controller.UpdateEntity(_worldId, _entityId, request, CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<ApiResponse<EntityResponse>>().Subject;
        response.Data.Should().NotBeNull();
        response.Meta.Should().ContainKey("etag");

        await _entityRepository.Received(1).UpdateAsync(Arg.Any<WorldEntity>(), etag, Arg.Any<CancellationToken>());
    }

    [TestMethod]
    public async Task UpdateEntity_WithInvalidRequest_ReturnsBadRequest()
    {
        // Arrange
        var request = new UpdateEntityRequest { Name = "", EntityType = EntityType.Character };
        var validationFailures = new List<ValidationFailure>
        {
            new("Name", "Name is required")
        };

        _updateValidator.ValidateAsync(request, Arg.Any<CancellationToken>())
            .Returns(new ValidationResult(validationFailures));

        // Act
        var result = await _controller.UpdateEntity(_worldId, _entityId, request, CancellationToken.None);

        // Assert
        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        var errorResponse = badRequestResult.Value.Should().BeOfType<ErrorResponse>().Subject;
        errorResponse.Error.Code.Should().Be("VALIDATION_ERROR");
    }

    [TestMethod]
    public async Task UpdateEntity_WithNonexistentEntity_ReturnsNotFound()
    {
        // Arrange
        var request = new UpdateEntityRequest { Name = "Updated", EntityType = EntityType.Character };

        _updateValidator.ValidateAsync(request, Arg.Any<CancellationToken>())
            .Returns(new ValidationResult());

        _entityRepository.GetByIdAsync(_worldId, _entityId, Arg.Any<CancellationToken>())
            .Returns((WorldEntity?)null);

        // Act
        var result = await _controller.UpdateEntity(_worldId, _entityId, request, CancellationToken.None);

        // Assert
        var notFoundResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
        var errorResponse = notFoundResult.Value.Should().BeOfType<ErrorResponse>().Subject;
        errorResponse.Error.Code.Should().Be("ENTITY_NOT_FOUND");
    }

    #endregion

    #region PatchEntity Tests

    [TestMethod]
    public async Task PatchEntity_WithPartialUpdate_ReturnsOkWithUpdatedEntity()
    {
        // Arrange
        var request = new PatchEntityRequest { Name = "Patched Name" };
        var existingEntity = WorldEntity.Create(_worldId, EntityType.Character, "Original Name", "Description");

        _entityRepository.GetByIdAsync(_worldId, _entityId, Arg.Any<CancellationToken>())
            .Returns(existingEntity);

        _entityRepository.UpdateAsync(Arg.Any<WorldEntity>(), null, Arg.Any<CancellationToken>())
            .Returns(existingEntity);

        // Act
        var result = await _controller.PatchEntity(_worldId, _entityId, request, CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<ApiResponse<EntityResponse>>().Subject;
        response.Data.Should().NotBeNull();
        response.Meta.Should().ContainKey("etag");
    }

    [TestMethod]
    public async Task PatchEntity_WithNonexistentEntity_ReturnsNotFound()
    {
        // Arrange
        var request = new PatchEntityRequest { Name = "Patched Name" };

        _entityRepository.GetByIdAsync(_worldId, _entityId, Arg.Any<CancellationToken>())
            .Returns((WorldEntity?)null);

        // Act
        var result = await _controller.PatchEntity(_worldId, _entityId, request, CancellationToken.None);

        // Assert
        var notFoundResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
        var errorResponse = notFoundResult.Value.Should().BeOfType<ErrorResponse>().Subject;
        errorResponse.Error.Code.Should().Be("ENTITY_NOT_FOUND");
    }

    #endregion

    #region DeleteEntity Tests

    [TestMethod]
    public async Task DeleteEntity_WithValidId_ReturnsNoContent()
    {
        // Arrange
        _entityRepository.DeleteAsync(_worldId, _entityId, false, Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.DeleteEntity(_worldId, _entityId);

        // Assert
        result.Should().BeOfType<NoContentResult>();
        await _entityRepository.Received(1).DeleteAsync(_worldId, _entityId, false, Arg.Any<CancellationToken>());
    }

    [TestMethod]
    public async Task DeleteEntity_WithCascadeTrue_PassesCascadeToRepository()
    {
        // Arrange
        _entityRepository.DeleteAsync(_worldId, _entityId, true, Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        // Act
        await _controller.DeleteEntity(_worldId, _entityId, cascade: true);

        // Assert
        await _entityRepository.Received(1).DeleteAsync(_worldId, _entityId, true, Arg.Any<CancellationToken>());
    }

    #endregion

    #region GetChildren Tests

    [TestMethod]
    public async Task GetChildren_WithValidParentId_ReturnsOkWithChildren()
    {
        // Arrange
        var children = new List<WorldEntity>
        {
            WorldEntity.Create(_worldId, EntityType.Location, "Child 1", null, _parentId),
            WorldEntity.Create(_worldId, EntityType.Location, "Child 2", null, _parentId)
        };

        _entityRepository.GetChildrenAsync(_worldId, _parentId, Arg.Any<CancellationToken>())
            .Returns(children);

        // Act
        var result = await _controller.GetChildren(_worldId, _parentId, CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<ApiResponse<IEnumerable<EntityResponse>>>().Subject;
        response.Data.Should().HaveCount(2);
        response.Data.First().Name.Should().Be("Child 1");
    }

    [TestMethod]
    public async Task GetChildren_WithNoChildren_ReturnsEmptyCollection()
    {
        // Arrange
        _entityRepository.GetChildrenAsync(_worldId, _parentId, Arg.Any<CancellationToken>())
            .Returns(new List<WorldEntity>());

        // Act
        var result = await _controller.GetChildren(_worldId, _parentId, CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<ApiResponse<IEnumerable<EntityResponse>>>().Subject;
        response.Data.Should().BeEmpty();
    }

    #endregion
}
