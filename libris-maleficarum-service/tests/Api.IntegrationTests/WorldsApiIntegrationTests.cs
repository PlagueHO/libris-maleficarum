namespace LibrisMaleficarum.Api.IntegrationTests;

using LibrisMaleficarum.IntegrationTests.Shared;

/// <summary>
/// Basic API integration tests for World Management endpoints.
/// These tests verify the API can start, connect to Cosmos DB Emulator, and respond to HTTP requests.
/// Uses shared AppHostFixture from IntegrationTests.Shared project to avoid Docker container conflicts.
/// </summary>
[TestClass]
[TestCategory("Integration")]
[TestCategory("RequiresDocker")]
[DoNotParallelize] // AppHost tests must run sequentially to avoid port conflicts
public class WorldsApiIntegrationTests
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
    public async Task GetWorlds_ReturnsSuccessStatusCode()
    {
        // Arrange
        var cancellationToken = TestContext!.CancellationTokenSource.Token;
        using var httpClient = AppHostFixture.App!.CreateHttpClient("api");

        // Act - List all worlds for current user
        using var response = await httpClient.GetAsync("/api/v1/worlds", cancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK, "API should return 200 OK for GET /api/v1/worlds");

        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        content.Should().NotBeNullOrWhiteSpace("Response should have content");
    }

    [TestMethod]
    public async Task CreateWorld_WithValidData_ReturnsCreated()
    {
        // Arrange
        var cancellationToken = TestContext!.CancellationTokenSource.Token;
        using var httpClient = AppHostFixture.App!.CreateHttpClient("api");

        var createRequest = new
        {
            Name = "Integration Test World",
            Description = "A world created by API integration test"
        };

        // Act - Create new world
        using var response = await httpClient.PostAsJsonAsync(
            "/api/v1/worlds",
            createRequest,
            cancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created, "API should return 201 Created for POST /api/v1/worlds");

        response.Headers.Location.Should().NotBeNull("Location header should be present");
        // Note: ETag header will be added when optimistic concurrency is fully implemented

        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        content.Should().NotBeNullOrWhiteSpace("Response should have content");
        content.Should().Contain("Integration Test World", "Response should contain the world name");
    }

    [TestMethod]
    public async Task GetWorldById_WithValidId_ReturnsWorld()
    {
        // Arrange
        var cancellationToken = TestContext!.CancellationTokenSource.Token;
        using var httpClient = AppHostFixture.App!.CreateHttpClient("api");

        // Create a world first
        var createRequest = new
        {
            Name = "Test World for GetById",
            Description = "World to test GET by ID"
        };

        using var createResponse = await httpClient.PostAsJsonAsync(
            "/api/v1/worlds",
            createRequest,
            cancellationToken);

        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        createResponse.Headers.Location.Should().NotBeNull("Location header should be present");

        // Extract ID from Location header (e.g., /api/v1/worlds/{id})
        var worldId = createResponse.Headers.Location!.Segments.Last();

        // Act - Get the world by ID
        using var response = await httpClient.GetAsync($"/api/v1/worlds/{worldId}", cancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK, "API should return 200 OK for GET /api/v1/worlds/{id}");

        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        content.Should().NotBeNullOrWhiteSpace("Response should have content");
        content.Should().Contain("Test World for GetById", "Response should contain the world name");
    }

    [TestMethod]
    public async Task UpdateWorld_WithValidData_ReturnsSuccess()
    {
        // Arrange
        var cancellationToken = TestContext!.CancellationTokenSource.Token;
        using var httpClient = AppHostFixture.App!.CreateHttpClient("api");

        // Create a world first
        var createRequest = new
        {
            Name = "Original World Name",
            Description = "Original description"
        };

        using var createResponse = await httpClient.PostAsJsonAsync(
            "/api/v1/worlds",
            createRequest,
            cancellationToken);

        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        createResponse.Headers.Location.Should().NotBeNull("Location header should be present");

        // Extract ID from Location header (e.g., /api/v1/worlds/{id})
        var worldId = createResponse.Headers.Location!.Segments.Last();

        // Act - Update the world
        var updateRequest = new
        {
            Name = "Updated World Name",
            Description = "Updated description"
        };

        using var response = await httpClient.PutAsJsonAsync(
            $"/api/v1/worlds/{worldId}",
            updateRequest,
            cancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK, "API should return 200 OK for PUT /api/v1/worlds/{id}");

        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        content.Should().NotBeNullOrWhiteSpace("Response should have content");
        content.Should().Contain("Updated World Name", "Response should contain the updated world name");
    }

    [TestMethod]
    public async Task DeleteWorld_WithValidId_ReturnsNoContent()
    {
        // Arrange
        var cancellationToken = TestContext!.CancellationTokenSource.Token;
        using var httpClient = AppHostFixture.App!.CreateHttpClient("api");

        // Create a world first
        var createRequest = new
        {
            Name = "World to Delete",
            Description = "This world will be deleted"
        };

        using var createResponse = await httpClient.PostAsJsonAsync(
            "/api/v1/worlds",
            createRequest,
            cancellationToken);

        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        createResponse.Headers.Location.Should().NotBeNull("Location header should be present");

        // Extract ID from Location header (e.g., /api/v1/worlds/{id})
        var worldId = createResponse.Headers.Location!.Segments.Last();

        // Act - Delete the world (soft delete)
        using var response = await httpClient.DeleteAsync($"/api/v1/worlds/{worldId}", cancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent, "API should return 204 No Content for DELETE /api/v1/worlds/{id}");
    }

    [TestMethod]
    public async Task GetWorldById_WithNonExistentId_ReturnsNotFound()
    {
        // Arrange
        var cancellationToken = TestContext!.CancellationTokenSource.Token;
        using var httpClient = AppHostFixture.App!.CreateHttpClient("api");
        var nonExistentId = Guid.NewGuid().ToString();

        // Act - Try to get a non-existent world
        using var response = await httpClient.GetAsync($"/api/v1/worlds/{nonExistentId}", cancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound, "API should return 404 Not Found for non-existent world");
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
