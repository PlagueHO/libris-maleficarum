namespace LibrisMaleficarum.Api.IntegrationTests;

using LibrisMaleficarum.Domain.ValueObjects;
using LibrisMaleficarum.Infrastructure.Persistence;
using LibrisMaleficarum.IntegrationTests.Shared;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Basic API integration tests for Entity Management endpoints.
/// These tests verify the API can manage entities within worlds via HTTP requests.
/// Uses shared AppHostFixture from IntegrationTests.Shared project to avoid Docker container conflicts.
/// </summary>
[TestClass]
[TestCategory("Integration")]
[TestCategory("RequiresDocker")]
[DoNotParallelize] // AppHost tests must run sequentially to avoid port conflicts
public class EntitiesApiIntegrationTests
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
    public async Task GetEntities_ReturnsSuccessStatusCode()
    {
        // Arrange
        var cancellationToken = TestContext!.CancellationTokenSource.Token;
        using var httpClient = AppHostFixture.App!.CreateHttpClient("api");

        // Create a world first
        var worldRequest = new { Name = "Test World for Entities", Description = "World to test entities" };
        using var worldResponse = await httpClient.PostAsJsonAsync("/api/v1/worlds", worldRequest, cancellationToken);
        worldResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var worldId = worldResponse.Headers.Location!.Segments.Last();

        // Act - List all entities in world
        using var response = await httpClient.GetAsync($"/api/v1/worlds/{worldId}/entities", cancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK, "API should return 200 OK for GET /api/v1/worlds/{worldId}/entities");

        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        content.Should().NotBeNullOrWhiteSpace("Response should have content");
    }

    [TestMethod]
    public async Task CreateEntity_WithValidData_ReturnsCreated()
    {
        // Arrange
        var cancellationToken = TestContext!.CancellationTokenSource.Token;
        using var httpClient = AppHostFixture.App!.CreateHttpClient("api");

        // Create a world first
        var worldRequest = new { Name = "Test World for Entity Creation", Description = "World to test entity creation" };
        using var worldResponse = await httpClient.PostAsJsonAsync("/api/v1/worlds", worldRequest, cancellationToken);
        worldResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var worldId = worldResponse.Headers.Location!.Segments.Last();

        var createRequest = new
        {
            Name = "Test Character",
            Description = "A character created by API integration test",
            EntityType = EntityType.Character
        };

        // Act - Create new entity
        using var response = await httpClient.PostAsJsonAsync(
            $"/api/v1/worlds/{worldId}/entities",
            createRequest,
            cancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created, "API should return 201 Created for POST /api/v1/worlds/{worldId}/entities");

        response.Headers.Location.Should().NotBeNull("Location header should be present");

        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        content.Should().NotBeNullOrWhiteSpace("Response should have content");
        content.Should().Contain("Test Character", "Response should contain the entity name");
    }

    [TestMethod]
    public async Task GetEntityById_WithValidId_ReturnsEntity()
    {
        // Arrange
        var cancellationToken = TestContext!.CancellationTokenSource.Token;
        using var httpClient = AppHostFixture.App!.CreateHttpClient("api");

        // Create a world first
        var worldRequest = new { Name = "Test World for GetEntityById", Description = "World to test get entity by ID" };
        using var worldResponse = await httpClient.PostAsJsonAsync("/api/v1/worlds", worldRequest, cancellationToken);
        worldResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var worldId = worldResponse.Headers.Location!.Segments.Last();

        // Create an entity
        var createRequest = new { Name = "Test Entity for GetById", Description = "Entity to test GET by ID", EntityType = EntityType.Location };
        using var createResponse = await httpClient.PostAsJsonAsync($"/api/v1/worlds/{worldId}/entities", createRequest, cancellationToken);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        createResponse.Headers.Location.Should().NotBeNull("Location header should be present");
        var entityId = createResponse.Headers.Location!.Segments.Last();

        // Act - Get the entity by ID
        using var response = await httpClient.GetAsync($"/api/v1/worlds/{worldId}/entities/{entityId}", cancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK, "API should return 200 OK for GET /api/v1/worlds/{worldId}/entities/{entityId}");

        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        content.Should().NotBeNullOrWhiteSpace("Response should have content");
        content.Should().Contain("Test Entity for GetById", "Response should contain the entity name");
    }

    [TestMethod]
    public async Task UpdateEntity_WithValidData_ReturnsSuccess()
    {
        // Arrange
        var cancellationToken = TestContext!.CancellationTokenSource.Token;
        using var httpClient = AppHostFixture.App!.CreateHttpClient("api");

        // Create a world first
        var worldRequest = new { Name = "Test World for UpdateEntity", Description = "World to test entity update" };
        using var worldResponse = await httpClient.PostAsJsonAsync("/api/v1/worlds", worldRequest, cancellationToken);
        worldResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var worldId = worldResponse.Headers.Location!.Segments.Last();

        // Create an entity
        var createRequest = new { Name = "Original Entity Name", Description = "Original description", EntityType = EntityType.Character };
        using var createResponse = await httpClient.PostAsJsonAsync($"/api/v1/worlds/{worldId}/entities", createRequest, cancellationToken);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        createResponse.Headers.Location.Should().NotBeNull("Location header should be present");
        var entityId = createResponse.Headers.Location!.Segments.Last();

        // Act - Update the entity
        var updateRequest = new { Name = "Updated Entity Name", Description = "Updated description", EntityType = EntityType.Character };
        using var response = await httpClient.PutAsJsonAsync($"/api/v1/worlds/{worldId}/entities/{entityId}", updateRequest, cancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK, "API should return 200 OK for PUT /api/v1/worlds/{worldId}/entities/{entityId}");

        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        content.Should().NotBeNullOrWhiteSpace("Response should have content");
        content.Should().Contain("Updated Entity Name", "Response should contain the updated entity name");
    }

    [TestMethod]
    public async Task DeleteEntity_WithValidId_ReturnsNoContent()
    {
        // Arrange
        var cancellationToken = TestContext!.CancellationTokenSource.Token;
        using var httpClient = AppHostFixture.App!.CreateHttpClient("api");

        // Create a world first
        var worldRequest = new { Name = "Test World for DeleteEntity", Description = "World to test entity deletion" };
        using var worldResponse = await httpClient.PostAsJsonAsync("/api/v1/worlds", worldRequest, cancellationToken);
        worldResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var worldId = worldResponse.Headers.Location!.Segments.Last();

        // Create an entity
        var createRequest = new { Name = "Entity to Delete", Description = "This entity will be deleted", EntityType = EntityType.Item };
        using var createResponse = await httpClient.PostAsJsonAsync($"/api/v1/worlds/{worldId}/entities", createRequest, cancellationToken);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        createResponse.Headers.Location.Should().NotBeNull("Location header should be present");
        var entityId = createResponse.Headers.Location!.Segments.Last();

        // Act - Delete the entity (soft delete)
        using var response = await httpClient.DeleteAsync($"/api/v1/worlds/{worldId}/entities/{entityId}", cancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent, "API should return 204 No Content for DELETE /api/v1/worlds/{worldId}/entities/{entityId}");
    }

    [TestMethod]
    public async Task GetEntityChildren_ReturnsSuccessStatusCode()
    {
        // Arrange
        var cancellationToken = TestContext!.CancellationTokenSource.Token;
        using var httpClient = AppHostFixture.App!.CreateHttpClient("api");

        // Create a world first
        var worldRequest = new { Name = "Test World for Entity Hierarchy", Description = "World to test entity hierarchy" };
        using var worldResponse = await httpClient.PostAsJsonAsync("/api/v1/worlds", worldRequest, cancellationToken);
        worldResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var worldId = worldResponse.Headers.Location!.Segments.Last();

        // Create a parent entity
        var parentRequest = new { Name = "Parent Entity", Description = "Parent entity", EntityType = EntityType.Location };
        using var parentResponse = await httpClient.PostAsJsonAsync($"/api/v1/worlds/{worldId}/entities", parentRequest, cancellationToken);
        parentResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var parentId = parentResponse.Headers.Location!.Segments.Last();

        // Act - Get children of entity
        using var response = await httpClient.GetAsync($"/api/v1/worlds/{worldId}/entities/{parentId}/children", cancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK, "API should return 200 OK for GET /api/v1/worlds/{worldId}/entities/{parentId}/children");

        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        content.Should().NotBeNullOrWhiteSpace("Response should have content");
    }

    [TestMethod]
    public async Task GetEntityById_WithNonExistentId_ReturnsNotFound()
    {
        // Arrange
        var cancellationToken = TestContext!.CancellationTokenSource.Token;
        using var httpClient = AppHostFixture.App!.CreateHttpClient("api");

        // Create a world first
        var worldRequest = new { Name = "Test World for NotFound", Description = "World to test 404" };
        using var worldResponse = await httpClient.PostAsJsonAsync("/api/v1/worlds", worldRequest, cancellationToken);
        worldResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var worldId = worldResponse.Headers.Location!.Segments.Last();

        var nonExistentId = Guid.NewGuid().ToString();

        // Act - Try to get a non-existent entity
        using var response = await httpClient.GetAsync($"/api/v1/worlds/{worldId}/entities/{nonExistentId}", cancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound, "API should return 404 Not Found for non-existent entity");
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
