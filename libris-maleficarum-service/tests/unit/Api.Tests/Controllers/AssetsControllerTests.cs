namespace LibrisMaleficarum.Api.Tests.Controllers;

using FluentAssertions;
using LibrisMaleficarum.Api.Controllers;
using LibrisMaleficarum.Api.Models.Responses;
using LibrisMaleficarum.Domain.Entities;
using LibrisMaleficarum.Domain.Exceptions;
using LibrisMaleficarum.Domain.Interfaces.Repositories;
using LibrisMaleficarum.Domain.Interfaces.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NSubstitute;

/// <summary>
/// Unit tests for AssetsController with mocked dependencies.
/// Tests request validation, response formatting, and error handling.
/// </summary>
[TestClass]
[TestCategory("Unit")]
public class AssetsControllerTests
{
    private IAssetRepository _assetRepository = null!;
    private IBlobStorageService _blobStorageService = null!;
    private IWorldEntityRepository _entityRepository = null!;
    private IUserContextService _userContextService = null!;
    private ILogger<AssetsController> _logger = null!;
    private AssetsController _controller = null!;

    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _worldId = Guid.NewGuid();
    private readonly Guid _entityId = Guid.NewGuid();
    private readonly Guid _assetId = Guid.NewGuid();
    private const string ValidBlobUrl = "https://storage.azure.com/container/blob";

    [TestInitialize]
    public void Setup()
    {
        _assetRepository = Substitute.For<IAssetRepository>();
        _blobStorageService = Substitute.For<IBlobStorageService>();
        _entityRepository = Substitute.For<IWorldEntityRepository>();
        _userContextService = Substitute.For<IUserContextService>();
        _logger = Substitute.For<ILogger<AssetsController>>();

        _userContextService.GetCurrentUserIdAsync().Returns(_userId);

        _controller = new AssetsController(
            _assetRepository,
            _blobStorageService,
            _entityRepository,
            _userContextService,
            _logger);
    }

    #region GetAssets Tests

    [TestMethod]
    public async Task GetAssets_WithValidRequest_ReturnsOkWithAssets()
    {
        // Arrange
        var assets = new List<Asset>
        {
            Asset.Create(_worldId, _entityId, "file1.jpg", "image/jpeg", 1024, ValidBlobUrl + "1", AssetType.Image),
            Asset.Create(_worldId, _entityId, "file2.png", "image/png", 2048, ValidBlobUrl + "2", AssetType.Image)
        };

        _assetRepository.GetAllByEntityAsync(_entityId, _worldId, _userId, 50, null, Arg.Any<CancellationToken>())
            .Returns((assets, (string?)null));

        // Act
        var result = await _controller.GetAssets(_worldId, _entityId);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<PaginatedApiResponse<AssetResponse>>().Subject;
        response.Data.Should().HaveCount(2);
        response.Data.First().FileName.Should().Be("file1.jpg");
        response.Meta.Count.Should().Be(2);
        response.Meta.NextCursor.Should().BeNull();
    }

    [TestMethod]
    public async Task GetAssets_WithCustomLimit_PassesLimitToRepository()
    {
        // Arrange
        _assetRepository.GetAllByEntityAsync(_entityId, _worldId, _userId, 10, null, Arg.Any<CancellationToken>())
            .Returns((new List<Asset>(), (string?)null));

        // Act
        await _controller.GetAssets(_worldId, _entityId, limit: 10);

        // Assert
        await _assetRepository.Received(1).GetAllByEntityAsync(_entityId, _worldId, _userId, 10, null, Arg.Any<CancellationToken>());
    }

    [TestMethod]
    public async Task GetAssets_WithCursor_PassesCursorToRepository()
    {
        // Arrange
        var cursor = "cursor-token";
        _assetRepository.GetAllByEntityAsync(_entityId, _worldId, _userId, 50, cursor, Arg.Any<CancellationToken>())
            .Returns((new List<Asset>(), (string?)null));

        // Act
        await _controller.GetAssets(_worldId, _entityId, cursor: cursor);

        // Assert
        await _assetRepository.Received(1).GetAllByEntityAsync(_entityId, _worldId, _userId, 50, cursor, Arg.Any<CancellationToken>());
    }

    [TestMethod]
    public async Task GetAssets_WithNextCursor_ReturnsNextCursorInMeta()
    {
        // Arrange
        var nextCursor = "next-cursor-token";
        _assetRepository.GetAllByEntityAsync(_entityId, _worldId, _userId, 50, null, Arg.Any<CancellationToken>())
            .Returns((new List<Asset>(), nextCursor));

        // Act
        var result = await _controller.GetAssets(_worldId, _entityId);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<PaginatedApiResponse<AssetResponse>>().Subject;
        response.Meta.NextCursor.Should().Be(nextCursor);
    }

    #endregion

    #region UploadAsset Tests

    [TestMethod]
    public async Task UploadAsset_WithValidFile_ReturnsCreatedWithAsset()
    {
        // Arrange
        var file = CreateMockFile("test.jpg", "image/jpeg", 1024);
        var createdAsset = Asset.Create(_worldId, _entityId, "test.jpg", "image/jpeg", 1024, ValidBlobUrl, AssetType.Image);

        // Mock repository to return asset with BlobUrl populated after "upload"
        _assetRepository.CreateAsync(
            _worldId,
            _entityId,
            "test.jpg",
            "image/jpeg",
            1024L,
            Arg.Any<Stream>(),
            Arg.Any<AssetType>(),
            Arg.Any<List<string>?>(),
            Arg.Any<string?>(),
            Arg.Any<ImageDimensions?>(),
            _userId,
            Arg.Any<CancellationToken>())
            .Returns(createdAsset);

        // Act
        var result = await _controller.UploadAsset(_worldId, _entityId, file, null, null);

        // Assert
        var createdResult = result.Should().BeOfType<CreatedAtActionResult>().Subject;
        createdResult.ActionName.Should().Be(nameof(AssetsController.GetAsset));

        var response = createdResult.Value.Should().BeOfType<ApiResponse<AssetResponse>>().Subject;
        response.Data.FileName.Should().Be("test.jpg");
        response.Data.SizeBytes.Should().Be(1024);
    }

    [TestMethod]
    public async Task UploadAsset_WithNullFile_ReturnsBadRequest()
    {
        // Act
        var result = await _controller.UploadAsset(_worldId, _entityId, null!, null, null);

        // Assert
        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        var errorResponse = badRequestResult.Value.Should().BeOfType<ErrorResponse>().Subject;
        errorResponse.Error.Code.Should().Be("FILE_REQUIRED");
    }

    [TestMethod]
    public async Task UploadAsset_WithEmptyFile_ReturnsBadRequest()
    {
        // Arrange
        var file = CreateMockFile("empty.jpg", "image/jpeg", 0);

        // Act
        var result = await _controller.UploadAsset(_worldId, _entityId, file, null, null);

        // Assert
        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        var errorResponse = badRequestResult.Value.Should().BeOfType<ErrorResponse>().Subject;
        errorResponse.Error.Code.Should().Be("FILE_REQUIRED");
    }

    [TestMethod]
    public async Task UploadAsset_WithFileTooLarge_ReturnsBadRequest()
    {
        // Arrange
        var file = CreateMockFile("huge.jpg", "image/jpeg", 26214401); // 25MB + 1 byte

        // Act
        var result = await _controller.UploadAsset(_worldId, _entityId, file, null, null);

        // Assert
        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        var errorResponse = badRequestResult.Value.Should().BeOfType<ErrorResponse>().Subject;
        errorResponse.Error.Code.Should().Be("FILE_TOO_LARGE");
        errorResponse.Error.Message.Should().Contain("26214401");
    }

    [TestMethod]
    public async Task UploadAsset_WithUnsupportedContentType_ReturnsBadRequest()
    {
        // Arrange
        var file = CreateMockFile("script.exe", "application/x-executable", 1024);

        // Act
        var result = await _controller.UploadAsset(_worldId, _entityId, file, null, null);

        // Assert
        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        var errorResponse = badRequestResult.Value.Should().BeOfType<ErrorResponse>().Subject;
        errorResponse.Error.Code.Should().Be("UNSUPPORTED_FILE_TYPE");
        errorResponse.Error.Message.Should().Contain("application/x-executable");
    }

    [TestMethod]
    [DataRow("image/jpeg")]
    [DataRow("image/png")]
    [DataRow("audio/mp3")]
    [DataRow("video/mp4")]
    [DataRow("application/pdf")]
    [DataRow("text/plain")]
    public async Task UploadAsset_WithAllowedContentTypes_Succeeds(string contentType)
    {
        // Arrange
        var file = CreateMockFile("file.ext", contentType, 1024);
        var createdAsset = Asset.Create(_worldId, _entityId, "file.ext", contentType, 1024, ValidBlobUrl, AssetType.Document);

        // Mock repository to return asset with BlobUrl populated after "upload"
        _assetRepository.CreateAsync(
            _worldId,
            _entityId,
            "file.ext",
            contentType,
            1024L,
            Arg.Any<Stream>(),
            Arg.Any<AssetType>(),
            Arg.Any<List<string>?>(),
            Arg.Any<string?>(),
            Arg.Any<ImageDimensions?>(),
            _userId,
            Arg.Any<CancellationToken>())
            .Returns(createdAsset);

        // Act
        var result = await _controller.UploadAsset(_worldId, _entityId, file, null, null);

        // Assert
        result.Should().BeOfType<CreatedAtActionResult>();
    }

    #endregion

    #region GetAsset Tests

    [TestMethod]
    public async Task GetAsset_WithExistingAsset_ReturnsOkWithAsset()
    {
        // Arrange
        var asset = Asset.Create(_worldId, _entityId, "test.jpg", "image/jpeg", 1024, ValidBlobUrl, AssetType.Image);
        _assetRepository.GetByIdAsync(_assetId, _userId, Arg.Any<CancellationToken>())
            .Returns(asset);

        // Act
        var result = await _controller.GetAsset(_worldId, _assetId);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<ApiResponse<AssetResponse>>().Subject;
        response.Data.FileName.Should().Be("test.jpg");
    }

    [TestMethod]
    public async Task GetAsset_WithNonexistentAsset_ThrowsAssetNotFoundException()
    {
        // Arrange
        _assetRepository.GetByIdAsync(_assetId, _userId, Arg.Any<CancellationToken>())
            .Returns<Asset>(x => throw new AssetNotFoundException(_assetId));

        // Act & Assert
        // Exception should propagate from repository and be handled by exception middleware
        await Assert.ThrowsExactlyAsync<AssetNotFoundException>(
            async () => await _controller.GetAsset(_worldId, _assetId));
    }

    #endregion

    #region GetAssetDownloadUrl Tests

    [TestMethod]
    public async Task GetAssetDownloadUrl_WithValidAsset_ReturnsSasUrl()
    {
        // Arrange
        var asset = Asset.Create(_worldId, _entityId, "test.jpg", "image/jpeg", 1024, ValidBlobUrl, AssetType.Image);
        var sasUrl = "https://storage.azure.com/container/blob?sas=token";

        _assetRepository.GetByIdAsync(_assetId, _userId, Arg.Any<CancellationToken>())
            .Returns(asset);
        _blobStorageService.GetSasUriAsync(ValidBlobUrl, 15, Arg.Any<CancellationToken>())
            .Returns(sasUrl);

        // Act
        var result = await _controller.GetAssetDownloadUrl(_worldId, _assetId);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<ApiResponse<AssetDownloadResponse>>().Subject;
        response.Data.DownloadUrl.Should().Be(sasUrl);
        response.Data.FileName.Should().Be("test.jpg");
        response.Data.ContentType.Should().Be("image/jpeg");
        response.Data.SizeBytes.Should().Be(1024);
        response.Data.ExpiresAt.Should().BeCloseTo(DateTime.UtcNow.AddMinutes(15), TimeSpan.FromSeconds(5));
    }

    [TestMethod]
    public async Task GetAssetDownloadUrl_Calls_BlobStorageService_WithCorrectParameters()
    {
        // Arrange
        var asset = Asset.Create(_worldId, _entityId, "test.jpg", "image/jpeg", 1024, ValidBlobUrl, AssetType.Image);

        _assetRepository.GetByIdAsync(_assetId, _userId, Arg.Any<CancellationToken>())
            .Returns(asset);
        _blobStorageService.GetSasUriAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns("https://sas-url");

        // Act
        await _controller.GetAssetDownloadUrl(_worldId, _assetId);

        // Assert
        await _blobStorageService.Received(1).GetSasUriAsync(ValidBlobUrl, 15, Arg.Any<CancellationToken>());
    }

    #endregion

    #region DeleteAsset Tests

    [TestMethod]
    public async Task DeleteAsset_WithExistingAsset_ReturnsNoContent()
    {
        // Arrange
        _assetRepository.DeleteAsync(_assetId, _userId, Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.DeleteAsset(_worldId, _assetId);

        // Assert
        result.Should().BeOfType<NoContentResult>();
    }

    [TestMethod]
    public async Task DeleteAsset_CallsRepository_WithCorrectParameters()
    {
        // Arrange
        _assetRepository.DeleteAsync(_assetId, _userId, Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        // Act
        await _controller.DeleteAsset(_worldId, _assetId);

        // Assert
        await _assetRepository.Received(1).DeleteAsync(_assetId, _userId, Arg.Any<CancellationToken>());
    }

    #endregion

    #region Helper Methods

    private static IFormFile CreateMockFile(string fileName, string contentType, long length)
    {
        var file = Substitute.For<IFormFile>();
        file.FileName.Returns(fileName);
        file.ContentType.Returns(contentType);
        file.Length.Returns(length);
        file.OpenReadStream().Returns(new MemoryStream());
        return file;
    }

    #endregion
}
