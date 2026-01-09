namespace LibrisMaleficarum.Domain.Tests.Entities;

using FluentAssertions;
using LibrisMaleficarum.Domain.Entities;

/// <summary>
/// Unit tests for the Asset entity.
/// Tests validation logic for file uploads and asset management.
/// </summary>
[TestClass]
public class AssetTests
{
    private readonly Guid _worldId = Guid.NewGuid();
    private readonly Guid _entityId = Guid.NewGuid();
    private const string ValidBlobUrl = "https://storage.azure.com/container/blob";

    [TestMethod]
    public void Create_WithValidParameters_ReturnsAsset()
    {
        // Arrange & Act
        var asset = Asset.Create(
            _worldId,
            _entityId,
            "test-file.jpg",
            "image/jpeg",
            1024,
            ValidBlobUrl,
            AssetType.Image);

        // Assert
        asset.Should().NotBeNull();
        asset.Id.Should().NotBeEmpty();
        asset.WorldId.Should().Be(_worldId);
        asset.EntityId.Should().Be(_entityId);
        asset.FileName.Should().Be("test-file.jpg");
        asset.ContentType.Should().Be("image/jpeg");
        asset.SizeBytes.Should().Be(1024);
        asset.BlobUrl.Should().Be(ValidBlobUrl);
        asset.CreatedDate.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        asset.IsDeleted.Should().BeFalse();
    }

    [TestMethod]
    public void Validate_WithValidFileName_Succeeds()
    {
        // Arrange
        var asset = Asset.Create(_worldId, _entityId, "valid-filename.png", "image/png", 1024, ValidBlobUrl, AssetType.Image);

        // Act & Assert
        var act = () => asset.Validate();
        act.Should().NotThrow();
    }

    [TestMethod]
    public void Validate_WithFileNameExceeding255Chars_ThrowsArgumentException()
    {
        // Arrange
        var longFileName = new string('a', 256) + ".jpg";

        // Act & Assert
        var act = () => Asset.Create(_worldId, _entityId, longFileName, "image/jpeg", 1024, ValidBlobUrl, AssetType.Image);
        act.Should().Throw<ArgumentException>()
            .WithMessage("FileName must not exceed 255 characters.*");
    }

    [TestMethod]
    public void Validate_WithEmptyFileName_ThrowsArgumentException()
    {
        // Act & Assert
        var act = () => Asset.Create(_worldId, _entityId, "", "image/jpeg", 1024, ValidBlobUrl, AssetType.Image);
        act.Should().Throw<ArgumentException>()
            .WithMessage("FileName cannot be null or whitespace.*");
    }

    [TestMethod]
    public void Validate_WithNullFileName_ThrowsArgumentException()
    {
        // Act & Assert
        var act = () => Asset.Create(_worldId, _entityId, null!, "image/jpeg", 1024, ValidBlobUrl, AssetType.Image);
        act.Should().Throw<ArgumentException>()
            .WithMessage("FileName cannot be null or whitespace.*");
    }

    [TestMethod]
    public void Validate_WithFileSizeExceedingDefault25MB_ThrowsArgumentException()
    {
        // Arrange
        var oversizedFile = 26214401; // 25MB + 1 byte

        // Act & Assert
        var act = () => Asset.Create(_worldId, _entityId, "large-file.jpg", "image/jpeg", oversizedFile, ValidBlobUrl, AssetType.Image);
        act.Should().Throw<ArgumentException>()
            .WithMessage("*exceeds maximum allowed size*");
    }

    [TestMethod]
    public void Validate_WithFileSizeAtExact25MB_Succeeds()
    {
        // Arrange
        var maxSize = 26214400; // Exactly 25MB

        // Act
        var asset = Asset.Create(_worldId, _entityId, "max-file.jpg", "image/jpeg", maxSize, ValidBlobUrl, AssetType.Image);

        // Assert
        asset.SizeBytes.Should().Be(maxSize);
    }

    [TestMethod]
    public void Validate_WithCustomMaxSize_EnforcesCustomLimit()
    {
        // Arrange
        var customMax = 1024; // 1KB limit

        // Act & Assert
        var act = () => Asset.Create(_worldId, _entityId, "file.jpg", "image/jpeg", 2048, ValidBlobUrl, AssetType.Image, maxSizeBytes: customMax);
        act.Should().Throw<ArgumentException>()
            .WithMessage("*exceeds maximum allowed size (1024 bytes)*");
    }

    [TestMethod]
    public void Validate_WithZeroFileSize_ThrowsArgumentException()
    {
        // Act & Assert
        var act = () => Asset.Create(_worldId, _entityId, "empty.jpg", "image/jpeg", 0, ValidBlobUrl, AssetType.Image);
        act.Should().Throw<ArgumentException>()
            .WithMessage("SizeBytes must be greater than zero.*");
    }

    [TestMethod]
    public void Validate_WithNegativeFileSize_ThrowsArgumentException()
    {
        // Act & Assert
        var act = () => Asset.Create(_worldId, _entityId, "negative.jpg", "image/jpeg", -100, ValidBlobUrl, AssetType.Image);
        act.Should().Throw<ArgumentException>()
            .WithMessage("SizeBytes must be greater than zero.*");
    }

    [TestMethod]
    [DataRow("image/jpeg")]
    [DataRow("image/jpg")]
    [DataRow("image/png")]
    [DataRow("image/gif")]
    [DataRow("image/webp")]
    [DataRow("audio/mpeg")]
    [DataRow("audio/mp3")]
    [DataRow("audio/wav")]
    [DataRow("audio/ogg")]
    [DataRow("video/mp4")]
    [DataRow("video/webm")]
    [DataRow("application/pdf")]
    [DataRow("text/plain")]
    [DataRow("text/markdown")]
    public void Validate_WithAllowedContentType_Succeeds(string contentType)
    {
        // Act
        var asset = Asset.Create(_worldId, _entityId, "file.ext", contentType, 1024, ValidBlobUrl, AssetType.Document);

        // Assert
        asset.ContentType.Should().Be(contentType);
    }

    [TestMethod]
    [DataRow("application/x-executable")]
    [DataRow("text/html")]
    [DataRow("application/javascript")]
    [DataRow("image/svg+xml")]
    public void Validate_WithDisallowedContentType_ThrowsArgumentException(string contentType)
    {
        // Act & Assert
        var act = () => Asset.Create(_worldId, _entityId, "file.ext", contentType, 1024, ValidBlobUrl, AssetType.Document);
        act.Should().Throw<ArgumentException>()
            .WithMessage($"ContentType '{contentType}' is not in the allowed list.*");
    }

    [TestMethod]
    public void Validate_WithEmptyContentType_ThrowsArgumentException()
    {
        // Act & Assert
        var act = () => Asset.Create(_worldId, _entityId, "file.jpg", "", 1024, ValidBlobUrl, AssetType.Image);
        act.Should().Throw<ArgumentException>()
            .WithMessage("ContentType cannot be null or whitespace.*");
    }

    [TestMethod]
    public void Validate_WithCustomAllowedContentTypes_EnforcesCustomList()
    {
        // Arrange
        var customTypes = new HashSet<string> { "image/png" };

        // Act & Assert - allowed type should succeed
        var asset = Asset.Create(_worldId, _entityId, "file.png", "image/png", 1024, ValidBlobUrl, AssetType.Image, allowedContentTypes: customTypes);
        asset.ContentType.Should().Be("image/png");

        // Assert - disallowed type should fail
        var act = () => Asset.Create(_worldId, _entityId, "file.jpg", "image/jpeg", 1024, ValidBlobUrl, AssetType.Image, allowedContentTypes: customTypes);
        act.Should().Throw<ArgumentException>()
            .WithMessage("*is not in the allowed list*");
    }

    [TestMethod]
    public void Validate_WithInvalidBlobUrl_ThrowsArgumentException()
    {
        // Act & Assert
        var act = () => Asset.Create(_worldId, _entityId, "file.jpg", "image/jpeg", 1024, "not-a-valid-url", AssetType.Image);
        act.Should().Throw<ArgumentException>()
            .WithMessage("BlobUrl must be a valid absolute URI.*");
    }

    [TestMethod]
    public void Validate_WithRelativeBlobUrl_ThrowsArgumentException()
    {
        // Act & Assert
        var act = () => Asset.Create(_worldId, _entityId, "file.jpg", "image/jpeg", 1024, "/relative/path", AssetType.Image);
        act.Should().Throw<ArgumentException>()
            .WithMessage("BlobUrl must be a valid absolute URI.*");
    }

    [TestMethod]
    public void Validate_WithEmptyBlobUrl_ThrowsArgumentException()
    {
        // Act & Assert
        var act = () => Asset.Create(_worldId, _entityId, "file.jpg", "image/jpeg", 1024, "", AssetType.Image);
        act.Should().Throw<ArgumentException>()
            .WithMessage("BlobUrl cannot be null or whitespace.*");
    }

    [TestMethod]
    public void Validate_WithEmptyWorldId_ThrowsArgumentException()
    {
        // Act & Assert
        var act = () => Asset.Create(Guid.Empty, _entityId, "file.jpg", "image/jpeg", 1024, ValidBlobUrl, AssetType.Image);
        act.Should().Throw<ArgumentException>()
            .WithMessage("WorldId cannot be empty.*");
    }

    [TestMethod]
    public void Validate_WithEmptyEntityId_ThrowsArgumentException()
    {
        // Act & Assert
        var act = () => Asset.Create(_worldId, Guid.Empty, "file.jpg", "image/jpeg", 1024, ValidBlobUrl, AssetType.Image);
        act.Should().Throw<ArgumentException>()
            .WithMessage("EntityId cannot be empty.*");
    }

    [TestMethod]
    public void SoftDelete_SetsIsDeletedToTrue()
    {
        // Arrange
        var asset = Asset.Create(_worldId, _entityId, "file.jpg", "image/jpeg", 1024, ValidBlobUrl, AssetType.Image);
        asset.IsDeleted.Should().BeFalse();

        // Act
        asset.SoftDelete("test-user-id");

        // Assert
        asset.IsDeleted.Should().BeTrue();
        asset.DeletedBy.Should().Be("test-user-id");
        asset.DeletedDate.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [TestMethod]
    public void SoftDelete_CanBeCalledMultipleTimes()
    {
        // Arrange
        var asset = Asset.Create(_worldId, _entityId, "file.jpg", "image/jpeg", 1024, ValidBlobUrl, AssetType.Image);

        // Act
        asset.SoftDelete("test-user-id");
        asset.SoftDelete("test-user-id"); // Second call

        // Assert
        asset.IsDeleted.Should().BeTrue();
    }
}
