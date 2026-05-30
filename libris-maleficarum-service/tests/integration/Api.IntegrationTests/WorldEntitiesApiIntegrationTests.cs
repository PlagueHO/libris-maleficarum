namespace LibrisMaleficarum.Api.IntegrationTests;

using LibrisMaleficarum.Domain.ValueObjects;
using LibrisMaleficarum.Infrastructure.Persistence;
using LibrisMaleficarum.IntegrationTests.Shared;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

/// <summary>
/// Basic API integration tests for WorldEntity Management endpoints.
/// These tests verify the API can manage entities within worlds via HTTP requests.
/// Uses shared AppHostFixture from IntegrationTests.Shared project to avoid Docker container conflicts.
/// </summary>
[TestClass]
[TestCategory("Integration")]
[TestCategory("RequiresDocker")]
[DoNotParallelize] // AppHost tests must run sequentially to avoid port conflicts
public class WorldEntitiesApiIntegrationTests
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
    public async Task GetWorldEntities_ReturnsSuccessStatusCode()
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
    public async Task CreateWorldEntity_WithValidData_ReturnsCreated()
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
    public async Task GetWorldEntityById_WithValidId_ReturnsEntity()
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
    public async Task UpdateWorldEntity_WithValidData_ReturnsSuccess()
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
    public async Task DeleteWorldEntity_WithValidId_ReturnsAccepted()
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

        // Act - Delete the entity (initiates async soft delete)
        using var response = await httpClient.DeleteAsync($"/api/v1/worlds/{worldId}/entities/{entityId}", cancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Accepted, "API should return 202 Accepted for DELETE /api/v1/worlds/{worldId}/entities/{entityId} (async operation)");
        response.Headers.Location.Should().NotBeNull("Location header should be present for polling operation status");

        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        content.Should().NotBeNullOrWhiteSpace("Response should contain delete operation details");
        content.Should().Contain("id", "Response should contain operation ID");
    }

    [TestMethod]
    public async Task GetWorldEntityChildren_ReturnsSuccessStatusCode()
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
    public async Task GetWorldEntityById_WithNonExistentId_ReturnsNotFound()
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

    [TestMethod]
    public async Task CreateWorldEntity_WithPropertyBags_PreservesObjectPayloadShape()
    {
        // Arrange
        var cancellationToken = TestContext!.CancellationTokenSource.Token;
        using var httpClient = AppHostFixture.App!.CreateHttpClient("api");

        var worldRequest = new { Name = "Test World for Payload Shape", Description = "World to test payload shape" };
        using var worldResponse = await httpClient.PostAsJsonAsync("/api/v1/worlds", worldRequest, cancellationToken);
        worldResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var worldId = worldResponse.Headers.Location!.Segments.Last();

        var createRequest = new
        {
            Name = "Shape Test Region",
            Description = "Validate payload shape",
            EntityType = EntityType.GeographicRegion,
            Properties = new
            {
                Climate = "Temperate",
                Population = 100000,
            },
            SystemProperties = new
            {
                RuleSet = "DND5E",
                ThreatLevel = "Medium",
            },
        };

        // Act
        using var createResponse = await httpClient.PostAsJsonAsync($"/api/v1/worlds/{worldId}/entities", createRequest, cancellationToken);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var responseJson = await createResponse.Content.ReadAsStringAsync(cancellationToken);
        using var document = JsonDocument.Parse(responseJson);
        var data = document.RootElement.GetProperty("data");

        // Assert
        data.GetProperty("properties").ValueKind.Should().Be(JsonValueKind.Object);
        var properties = data.GetProperty("properties");
        GetPropertyIgnoreCase(properties, "Climate").GetString().Should().Be("Temperate");

        data.GetProperty("systemProperties").ValueKind.Should().Be(JsonValueKind.Object);
        var systemProperties = data.GetProperty("systemProperties");
        GetPropertyIgnoreCase(systemProperties, "RuleSet").GetString().Should().Be("DND5E");

        data.GetProperty("path").ValueKind.Should().Be(JsonValueKind.Array);

        static JsonElement GetPropertyIgnoreCase(JsonElement element, string propertyName)
        {
            foreach (var property in element.EnumerateObject())
            {
                if (string.Equals(property.Name, propertyName, StringComparison.OrdinalIgnoreCase))
                {
                    return property.Value;
                }
            }

            throw new KeyNotFoundException($"Property '{propertyName}' was not found on JSON object.");
        }
    }

    [TestMethod]
    public async Task UpdateWorldEntity_WithPropertyBags_MergesWithExistingValues()
    {
        // Arrange
        var cancellationToken = TestContext!.CancellationTokenSource.Token;
        using var httpClient = AppHostFixture.App!.CreateHttpClient("api");

        var worldRequest = new { Name = "Test World for Merge", Description = "World to test merge update" };
        using var worldResponse = await httpClient.PostAsJsonAsync("/api/v1/worlds", worldRequest, cancellationToken);
        worldResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var worldId = worldResponse.Headers.Location!.Segments.Last();

        var createRequest = new
        {
            Name = "Entity Before Merge",
            Description = "Initial entity",
            EntityType = EntityType.Character,
            Properties = new
            {
                Strength = 10,
                Wisdom = 14,
            },
            SystemProperties = new
            {
                Source = "Import",
                Checksum = "abc123",
            },
        };

        using var createResponse = await httpClient.PostAsJsonAsync($"/api/v1/worlds/{worldId}/entities", createRequest, cancellationToken);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var entityId = createResponse.Headers.Location!.Segments.Last();

        var updateRequest = new
        {
            Name = "Entity After Merge",
            Description = "Updated entity",
            EntityType = EntityType.Character,
            Properties = new
            {
                Strength = 12,
                Agility = 16,
            },
            SystemProperties = new
            {
                Source = "Manual",
                Revision = 2,
            },
        };

        // Act
        using var updateResponse = await httpClient.PutAsJsonAsync($"/api/v1/worlds/{worldId}/entities/{entityId}", updateRequest, cancellationToken);

        // Assert
        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var responseJson = await updateResponse.Content.ReadAsStringAsync(cancellationToken);
        using var document = JsonDocument.Parse(responseJson);
        var data = document.RootElement.GetProperty("data");

        var properties = data.GetProperty("properties");
        GetPropertyIgnoreCase(properties, "Strength").GetInt32().Should().Be(12);
        GetPropertyIgnoreCase(properties, "Wisdom").GetInt32().Should().Be(14);
        GetPropertyIgnoreCase(properties, "Agility").GetInt32().Should().Be(16);

        var systemProperties = data.GetProperty("systemProperties");
        GetPropertyIgnoreCase(systemProperties, "Source").GetString().Should().Be("Manual");
        GetPropertyIgnoreCase(systemProperties, "Checksum").GetString().Should().Be("abc123");
        GetPropertyIgnoreCase(systemProperties, "Revision").GetInt32().Should().Be(2);

        static JsonElement GetPropertyIgnoreCase(JsonElement element, string propertyName)
        {
            foreach (var property in element.EnumerateObject())
            {
                if (string.Equals(property.Name, propertyName, StringComparison.OrdinalIgnoreCase))
                {
                    return property.Value;
                }
            }

            throw new KeyNotFoundException($"Property '{propertyName}' was not found on JSON object.");
        }
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
