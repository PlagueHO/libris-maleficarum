namespace LibrisMaleficarum.Infrastructure.IntegrationTests.Services;

using Azure.Storage.Blobs;
using LibrisMaleficarum.Infrastructure.Services;
using LibrisMaleficarum.IntegrationTests.Shared;

/// <summary>
/// Integration tests for BlobStorageService using Azure Storage Emulator (Azurite).
/// These tests verify the service can upload, retrieve SAS URLs, and delete blobs from Azure Blob Storage.
/// Uses shared AppHostFixture from IntegrationTests.Shared project to avoid Docker container conflicts.
/// </summary>
[TestClass]
[TestCategory("Integration")]
[TestCategory("RequiresDocker")]
[DoNotParallelize] // AppHost tests must run sequentially to avoid port conflicts
public class BlobStorageServiceIntegrationTests
{
    public TestContext? TestContext { get; set; }

    private BlobStorageService? _blobStorageService;
    private BlobServiceClient? _blobServiceClient;

    [ClassInitialize]
    public static async Task ClassInitialize(TestContext context)
    {
        // Initialize shared AppHost fixture (runs once for all tests in this class)
        await AppHostFixture.InitializeAsync(context);
    }

    [TestInitialize]
    public void TestInitialize()
    {
        // Create BlobServiceClient using connection string from AppHostFixture
        var connectionString = AppHostFixture.StorageConnectionString;
        _blobServiceClient = new BlobServiceClient(connectionString);

        // Create service instance
        _blobStorageService = new BlobStorageService(_blobServiceClient);
    }

    [TestMethod]
    public async Task UploadAsync_ValidFile_ReturnsAbsoluteUri()
    {
        // Arrange
        var containerName = "test-container";
        var blobName = $"test-upload-{Guid.NewGuid()}.txt";
        var contentType = "text/plain";
        var fileContent = "Hello, Blob Storage!"u8.ToArray();
        using var stream = new MemoryStream(fileContent);
        var metadata = new Dictionary<string, string>
        {
            ["author"] = "IntegrationTest",
            ["purpose"] = "TestUpload"
        };

        // Act
        var blobUrl = await _blobStorageService!.UploadAsync(
            containerName,
            blobName,
            stream,
            contentType,
            metadata);

        // Assert
        blobUrl.Should().NotBeNullOrWhiteSpace("Upload should return a blob URL");
        blobUrl.Should().Contain(containerName, "Blob URL should contain container name");
        blobUrl.Should().Contain(blobName, "Blob URL should contain blob name");

        // Verify blob exists
        var containerClient = _blobServiceClient!.GetBlobContainerClient(containerName);
        var blobClient = containerClient.GetBlobClient(blobName);
        var exists = await blobClient.ExistsAsync();
        exists.Value.Should().BeTrue("Blob should exist after upload");

        // Verify blob properties
        var properties = await blobClient.GetPropertiesAsync();
        properties.Value.ContentType.Should().Be(contentType, "Content type should match");
        properties.Value.Metadata.Should().ContainKey("author");
        properties.Value.Metadata["author"].Should().Be("IntegrationTest");

        // Cleanup
        await blobClient.DeleteIfExistsAsync();
    }

    [TestMethod]
    public async Task UploadAsync_CreatesContainerIfNotExists()
    {
        // Arrange
        var containerName = $"new-container-{Guid.NewGuid()}";
        var blobName = "test-blob.txt";
        var contentType = "text/plain";
        var fileContent = "Test content"u8.ToArray();
        using var stream = new MemoryStream(fileContent);

        // Verify container doesn't exist
        var containerClient = _blobServiceClient!.GetBlobContainerClient(containerName);
        var containerExists = await containerClient.ExistsAsync();
        containerExists.Value.Should().BeFalse("Container should not exist before upload");

        // Act
        var blobUrl = await _blobStorageService!.UploadAsync(
            containerName,
            blobName,
            stream,
            contentType);

        // Assert
        blobUrl.Should().NotBeNullOrWhiteSpace("Upload should return a blob URL");

        // Verify container was created
        containerExists = await containerClient.ExistsAsync();
        containerExists.Value.Should().BeTrue("Container should be created during upload");

        // Cleanup
        await containerClient.DeleteAsync();
    }

    [TestMethod]
    public async Task GetSasUriAsync_ValidBlobUrl_ReturnsSasUri()
    {
        // Arrange
        var containerName = "test-sas-container";
        var blobName = $"test-sas-{Guid.NewGuid()}.txt";
        var contentType = "text/plain";
        var fileContent = "SAS test content"u8.ToArray();
        using var uploadStream = new MemoryStream(fileContent);

        // Upload blob first
        var blobUrl = await _blobStorageService!.UploadAsync(
            containerName,
            blobName,
            uploadStream,
            contentType);

        // Act
        var sasUri = await _blobStorageService.GetSasUriAsync(blobUrl, expirationMinutes: 15);

        // Assert
        sasUri.Should().NotBeNullOrWhiteSpace("SAS URI should not be null");
        sasUri.Should().Contain("sig=", "SAS URI should contain signature");
        sasUri.Should().Contain("se=", "SAS URI should contain expiry time");
        sasUri.Should().Contain("sp=r", "SAS URI should have read permission");

        // Verify SAS URI can be used to download blob
        using var httpClient = new HttpClient();
        var downloadResponse = await httpClient.GetAsync(sasUri);
        downloadResponse.IsSuccessStatusCode.Should().BeTrue("SAS URI should allow blob download");

        var downloadedContent = await downloadResponse.Content.ReadAsByteArrayAsync();
        downloadedContent.Should().BeEquivalentTo(fileContent, "Downloaded content should match uploaded content");

        // Cleanup
        var containerClient = _blobServiceClient!.GetBlobContainerClient(containerName);
        var blobClient = containerClient.GetBlobClient(blobName);
        await blobClient.DeleteIfExistsAsync();
    }

    [TestMethod]
    public async Task GetSasUriAsync_InvalidBlobUrl_ThrowsArgumentException()
    {
        // Arrange
        var invalidBlobUrl = "https://example.com/invalid";

        // Act
        var act = async () => await _blobStorageService!.GetSasUriAsync(invalidBlobUrl);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>("Invalid blob URL should throw ArgumentException")
            .WithMessage("*Invalid blob URL format*");
    }

    [TestMethod]
    public async Task DeleteAsync_ExistingBlob_RemovesBlob()
    {
        // Arrange
        var containerName = "test-delete-container";
        var blobName = $"test-delete-{Guid.NewGuid()}.txt";
        var contentType = "text/plain";
        var fileContent = "Content to delete"u8.ToArray();
        using var uploadStream = new MemoryStream(fileContent);

        // Upload blob first
        var blobUrl = await _blobStorageService!.UploadAsync(
            containerName,
            blobName,
            uploadStream,
            contentType);

        // Verify blob exists
        var containerClient = _blobServiceClient!.GetBlobContainerClient(containerName);
        var blobClient = containerClient.GetBlobClient(blobName);
        var existsBefore = await blobClient.ExistsAsync();
        existsBefore.Value.Should().BeTrue("Blob should exist before delete");

        // Act
        await _blobStorageService.DeleteAsync(blobUrl);

        // Assert
        var existsAfter = await blobClient.ExistsAsync();
        existsAfter.Value.Should().BeFalse("Blob should not exist after delete");
    }

    [TestMethod]
    public async Task DeleteAsync_NonExistentBlob_DoesNotThrow()
    {
        // Arrange
        var containerName = "test-delete-container";
        var blobName = $"nonexistent-blob-{Guid.NewGuid()}.txt";
        var blobUrl = $"https://127.0.0.1:10000/devstoreaccount1/{containerName}/{blobName}";

        // Act
        var act = async () => await _blobStorageService!.DeleteAsync(blobUrl);

        // Assert
        await act.Should().NotThrowAsync("Deleting non-existent blob should not throw exception");
    }

    [TestMethod]
    public async Task UploadAsync_NullParameters_ThrowsArgumentNullException()
    {
        // Arrange
        var containerName = "test-container";
        var blobName = "test.txt";
        var contentType = "text/plain";
        using var stream = new MemoryStream();

        // Act & Assert - Null containerName
        var act1 = async () => await _blobStorageService!.UploadAsync(null!, blobName, stream, contentType);
        await act1.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("containerName");

        // Act & Assert - Null blobName
        var act2 = async () => await _blobStorageService!.UploadAsync(containerName, null!, stream, contentType);
        await act2.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("blobName");

        // Act & Assert - Null contentStream
        var act3 = async () => await _blobStorageService!.UploadAsync(containerName, blobName, null!, contentType);
        await act3.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("contentStream");

        // Act & Assert - Null contentType
        var act4 = async () => await _blobStorageService!.UploadAsync(containerName, blobName, stream, null!);
        await act4.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("contentType");
    }

    [TestMethod]
    public async Task GetSasUriAsync_ZeroExpirationMinutes_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var blobUrl = "https://example.com/container/blob.txt";

        // Act
        var act = async () => await _blobStorageService!.GetSasUriAsync(blobUrl, expirationMinutes: 0);

        // Assert
        await act.Should().ThrowAsync<ArgumentOutOfRangeException>()
            .WithParameterName("expirationMinutes")
            .WithMessage("*must be greater than zero*");
    }

    [TestMethod]
    public async Task UploadAsync_WithMetadata_StoresMetadataOnBlob()
    {
        // Arrange
        var containerName = "test-metadata-container";
        var blobName = $"test-metadata-{Guid.NewGuid()}.txt";
        var contentType = "text/plain";
        var fileContent = "Content with metadata"u8.ToArray();
        using var stream = new MemoryStream(fileContent);
        var metadata = new Dictionary<string, string>
        {
            ["category"] = "test",
            ["version"] = "1.0",
            ["createdby"] = "integration-test"  // Azure metadata keys cannot contain hyphens
        };

        // Act
        var blobUrl = await _blobStorageService!.UploadAsync(
            containerName,
            blobName,
            stream,
            contentType,
            metadata);

        // Assert
        var containerClient = _blobServiceClient!.GetBlobContainerClient(containerName);
        var blobClient = containerClient.GetBlobClient(blobName);
        var properties = await blobClient.GetPropertiesAsync();

        properties.Value.Metadata.Should().ContainKey("category");
        properties.Value.Metadata["category"].Should().Be("test");
        properties.Value.Metadata.Should().ContainKey("version");
        properties.Value.Metadata["version"].Should().Be("1.0");
        properties.Value.Metadata.Should().ContainKey("createdby");
        properties.Value.Metadata["createdby"].Should().Be("integration-test");

        // Cleanup
        await blobClient.DeleteIfExistsAsync();
    }
}
