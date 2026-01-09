namespace LibrisMaleficarum.Api.IntegrationTests;

using LibrisMaleficarum.Domain.ValueObjects;
using LibrisMaleficarum.Infrastructure.Persistence;
using LibrisMaleficarum.IntegrationTests.Shared;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Basic API integration tests for Asset Management endpoints.
/// These tests verify the API can manage assets (files) within worlds via HTTP requests.
/// Uses shared AppHostFixture from IntegrationTests.Shared project to avoid Docker container conflicts.
/// </summary>
[TestClass]
[TestCategory("Integration")]
[TestCategory("RequiresDocker")]
[DoNotParallelize] // AppHost tests must run sequentially to avoid port conflicts
public class AssetsApiIntegrationTests
{
    public TestContext? TestContext { get; set; }

    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(30);

    [ClassInitialize]
    public static async Task ClassInitialize(TestContext context)
    {
        // Initialize shared AppHost fixture (runs once for all tests in this class)
        await AppHostFixture.InitializeAsync(context);

        // Ensure database is created before running any tests
        await EnsureDatabaseCreatedAsync(context.CancellationTokenSource.Token);
    }

    [TestMethod]
    public async Task GetEntityAssets_ReturnsSuccessStatusCode()
    {
        // Arrange
        var cancellationToken = TestContext!.CancellationTokenSource.Token;
        using var httpClient = AppHostFixture.App!.CreateHttpClient("api");

        // Create a world and entity first
        var worldRequest = new { Name = "Test World for Assets", Description = "World to test assets" };
        using var worldResponse = await httpClient.PostAsJsonAsync("/api/v1/worlds", worldRequest, cancellationToken);
        worldResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var worldId = worldResponse.Headers.Location!.Segments.Last();

        var entityRequest = new { Name = "Test Entity for Assets", Description = "Entity to test assets", EntityType = EntityType.Character };
        using var entityResponse = await httpClient.PostAsJsonAsync($"/api/v1/worlds/{worldId}/entities", entityRequest, cancellationToken);
        entityResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var entityId = entityResponse.Headers.Location!.Segments.Last();

        // Act - List all assets for entity
        using var response = await httpClient.GetAsync($"/api/v1/worlds/{worldId}/entities/{entityId}/assets", cancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK, "API should return 200 OK for GET /api/v1/worlds/{worldId}/entities/{entityId}/assets");

        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        content.Should().NotBeNullOrWhiteSpace("Response should have content");
    }

    [TestMethod]
    public async Task UploadAsset_WithValidData_ReturnsCreated()
    {
        // Arrange
        var cancellationToken = TestContext!.CancellationTokenSource.Token;
        using var httpClient = AppHostFixture.App!.CreateHttpClient("api");

        // Create a world and entity first
        var worldRequest = new { Name = "Test World for Asset Upload", Description = "World to test asset upload" };
        using var worldResponse = await httpClient.PostAsJsonAsync("/api/v1/worlds", worldRequest, cancellationToken);
        worldResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var worldId = worldResponse.Headers.Location!.Segments.Last();

        var entityRequest = new { Name = "Test Entity for Asset Upload", Description = "Entity to test asset upload", EntityType = EntityType.Character };
        using var entityResponse = await httpClient.PostAsJsonAsync($"/api/v1/worlds/{worldId}/entities", entityRequest, cancellationToken);
        entityResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var entityId = entityResponse.Headers.Location!.Segments.Last();

        // Create multipart form data for file upload
        using var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(new byte[] { 0x89, 0x50, 0x4E, 0x47 }); // PNG header
        fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/png");
        content.Add(fileContent, "file", "test-image.png");

        // Act - Upload asset
        using var response = await httpClient.PostAsync(
            $"/api/v1/worlds/{worldId}/entities/{entityId}/assets",
            content,
            cancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created, "API should return 201 Created for POST /api/v1/worlds/{worldId}/entities/{entityId}/assets");

        response.Headers.Location.Should().NotBeNull("Location header should be present");
    }

    [TestMethod]
    public async Task GetAssetById_WithValidId_ReturnsAsset()
    {
        // Arrange
        var cancellationToken = TestContext!.CancellationTokenSource.Token;
        using var httpClient = AppHostFixture.App!.CreateHttpClient("api");

        // Create a world and entity first
        var worldRequest = new { Name = "Test World for GetAsset", Description = "World to test get asset" };
        using var worldResponse = await httpClient.PostAsJsonAsync("/api/v1/worlds", worldRequest, cancellationToken);
        worldResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var worldId = worldResponse.Headers.Location!.Segments.Last();

        var entityRequest = new { Name = "Test Entity for GetAsset", Description = "Entity to test get asset", EntityType = EntityType.Location };
        using var entityResponse = await httpClient.PostAsJsonAsync($"/api/v1/worlds/{worldId}/entities", entityRequest, cancellationToken);
        entityResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var entityId = entityResponse.Headers.Location!.Segments.Last();

        // Upload an asset
        using var uploadContent = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(new byte[] { 0xFF, 0xD8, 0xFF }); // JPEG header
        fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/jpeg");
        uploadContent.Add(fileContent, "file", "test-asset-for-getbyid.jpg");

        using var uploadResponse = await httpClient.PostAsync($"/api/v1/worlds/{worldId}/entities/{entityId}/assets", uploadContent, cancellationToken);
        uploadResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        uploadResponse.Headers.Location.Should().NotBeNull("Location header should be present");
        var assetId = uploadResponse.Headers.Location!.Segments.Last();

        // Act - Get the asset by ID
        using var response = await httpClient.GetAsync($"/api/v1/worlds/{worldId}/assets/{assetId}", cancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK, "API should return 200 OK for GET /api/v1/worlds/{worldId}/assets/{assetId}");

        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        content.Should().NotBeNullOrWhiteSpace("Response should have content");
        content.Should().Contain("test-asset-for-getbyid.jpg", "Response should contain the asset fileName");
    }

    [TestMethod]
    public async Task UpdateAssetMetadata_WithValidData_ReturnsSuccess()
    {
        // Arrange
        var cancellationToken = TestContext!.CancellationTokenSource.Token;
        using var httpClient = AppHostFixture.App!.CreateHttpClient("api");

        // Create a world and entity first
        var worldRequest = new { Name = "Test World for UpdateAsset", Description = "World to test update asset" };
        using var worldResponse = await httpClient.PostAsJsonAsync("/api/v1/worlds", worldRequest, cancellationToken);
        worldResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var worldId = worldResponse.Headers.Location!.Segments.Last();

        var entityRequest = new { Name = "Test Entity for UpdateAsset", Description = "Entity to test update asset", EntityType = EntityType.Character };
        using var entityResponse = await httpClient.PostAsJsonAsync($"/api/v1/worlds/{worldId}/entities", entityRequest, cancellationToken);
        entityResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var entityId = entityResponse.Headers.Location!.Segments.Last();

        // Upload an asset
        using var uploadContent = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(new byte[] { 0x25, 0x50, 0x44, 0x46 }); // PDF header
        fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/pdf");
        uploadContent.Add(fileContent, "file", "original-document.pdf");

        using var uploadResponse = await httpClient.PostAsync($"/api/v1/worlds/{worldId}/entities/{entityId}/assets", uploadContent, cancellationToken);
        uploadResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        uploadResponse.Headers.Location.Should().NotBeNull("Location header should be present");
        var assetId = uploadResponse.Headers.Location!.Segments.Last();

        // Act - Update the asset metadata (note: Asset is immutable, endpoint returns existing asset)
        var updateRequest = new { }; // Empty update request since Asset has no updateable metadata
        using var response = await httpClient.PutAsJsonAsync($"/api/v1/worlds/{worldId}/assets/{assetId}", updateRequest, cancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK, "API should return 200 OK for PUT /api/v1/worlds/{worldId}/assets/{assetId}");

        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        content.Should().NotBeNullOrWhiteSpace("Response should have content");
        content.Should().Contain("original-document.pdf", "Response should contain the original fileName since Asset is immutable");
    }

    [TestMethod]
    public async Task DeleteAsset_WithValidId_ReturnsNoContent()
    {
        // Arrange
        var cancellationToken = TestContext!.CancellationTokenSource.Token;
        using var httpClient = AppHostFixture.App!.CreateHttpClient("api");

        // Create a world and entity first
        var worldRequest = new { Name = "Test World for DeleteAsset", Description = "World to test delete asset" };
        using var worldResponse = await httpClient.PostAsJsonAsync("/api/v1/worlds", worldRequest, cancellationToken);
        worldResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var worldId = worldResponse.Headers.Location!.Segments.Last();

        var entityRequest = new { Name = "Test Entity for DeleteAsset", Description = "Entity to test delete asset", EntityType = EntityType.Item };
        using var entityResponse = await httpClient.PostAsJsonAsync($"/api/v1/worlds/{worldId}/entities", entityRequest, cancellationToken);
        entityResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var entityId = entityResponse.Headers.Location!.Segments.Last();

        // Upload an asset
        using var uploadContent = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(new byte[] { 0x89, 0x50, 0x4E });
        fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/png");
        uploadContent.Add(fileContent, "file", "asset-to-delete.png");

        using var uploadResponse = await httpClient.PostAsync($"/api/v1/worlds/{worldId}/entities/{entityId}/assets", uploadContent, cancellationToken);
        uploadResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        uploadResponse.Headers.Location.Should().NotBeNull("Location header should be present");
        var assetId = uploadResponse.Headers.Location!.Segments.Last();

        // Act - Delete the asset
        using var response = await httpClient.DeleteAsync($"/api/v1/worlds/{worldId}/assets/{assetId}", cancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent, "API should return 204 No Content for DELETE /api/v1/worlds/{worldId}/assets/{assetId}");
    }

    [TestMethod]
    public async Task GetAssetById_WithNonExistentId_ReturnsNotFound()
    {
        // Arrange
        var cancellationToken = TestContext!.CancellationTokenSource.Token;
        using var httpClient = AppHostFixture.App!.CreateHttpClient("api");

        // Create a world first
        var worldRequest = new { Name = "Test World for Asset NotFound", Description = "World to test 404" };
        using var worldResponse = await httpClient.PostAsJsonAsync("/api/v1/worlds", worldRequest, cancellationToken);
        worldResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var worldId = worldResponse.Headers.Location!.Segments.Last();

        var nonExistentId = Guid.NewGuid().ToString();

        // Act - Try to get a non-existent asset
        using var response = await httpClient.GetAsync($"/api/v1/worlds/{worldId}/assets/{nonExistentId}", cancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound, "API should return 404 Not Found for non-existent asset");
    }

    /// <summary>
    /// Ensures the Cosmos DB database is created before running API tests.
    /// This is necessary because EF Core with Cosmos DB requires the database to exist.
    /// </summary>
    private static async Task EnsureDatabaseCreatedAsync(CancellationToken cancellationToken)
    {
        // Get Cosmos DB connection string from AppHostFixture
        var connectionString = AppHostFixture.CosmosDbConnectionString;

        // Create a DbContext to initialize the database
        var cosmosClientOptions = new Microsoft.Azure.Cosmos.CosmosClientOptions
        {
            ConnectionMode = Microsoft.Azure.Cosmos.ConnectionMode.Gateway,
            HttpClientFactory = () => new HttpClient(new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
            }),
            LimitToEndpoint = true
        };

        var dbContextOptions = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseCosmos(
                connectionString,
                "LibrisMaleficarum",
                cosmosOptions =>
                {
                    cosmosOptions.ConnectionMode(Microsoft.Azure.Cosmos.ConnectionMode.Gateway);
                    cosmosOptions.HttpClientFactory(() => new HttpClient(new HttpClientHandler
                    {
                        ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
                    }));
                })
            .Options;

        await using var context = new ApplicationDbContext(dbContextOptions);
        await context.Database.EnsureCreatedAsync(cancellationToken);
    }
}
