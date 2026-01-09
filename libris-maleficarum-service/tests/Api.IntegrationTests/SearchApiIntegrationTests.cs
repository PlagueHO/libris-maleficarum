namespace LibrisMaleficarum.Api.IntegrationTests;

using LibrisMaleficarum.Domain.ValueObjects;
using LibrisMaleficarum.Infrastructure.Persistence;
using LibrisMaleficarum.IntegrationTests.Shared;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Basic API integration tests for Search and Discovery endpoints.
/// These tests verify the API can search and filter entities within worlds via HTTP requests.
/// Uses shared AppHostFixture from IntegrationTests.Shared project to avoid Docker container conflicts.
/// </summary>
[TestClass]
[TestCategory("Integration")]
[TestCategory("RequiresDocker")]
[DoNotParallelize] // AppHost tests must run sequentially to avoid port conflicts
public class SearchApiIntegrationTests
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
    public async Task SearchEntities_ReturnsSuccessStatusCode()
    {
        // Arrange
        var cancellationToken = TestContext!.CancellationTokenSource.Token;
        using var httpClient = AppHostFixture.App!.CreateHttpClient("api");

        // Create a world first
        var worldRequest = new { Name = "Test World for Search", Description = "World to test search" };
        using var worldResponse = await httpClient.PostAsJsonAsync("/api/v1/worlds", worldRequest, cancellationToken);
        worldResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var worldId = worldResponse.Headers.Location!.Segments.Last();

        // Act - Search entities by name
        using var response = await httpClient.GetAsync($"/api/v1/worlds/{worldId}/search?query=test", cancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK, "API should return 200 OK for GET /api/v1/worlds/{worldId}/search");

        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        content.Should().NotBeNullOrWhiteSpace("Response should have content");
    }

    [TestMethod]
    public async Task FilterEntitiesByType_ReturnsSuccessStatusCode()
    {
        // Arrange
        var cancellationToken = TestContext!.CancellationTokenSource.Token;
        using var httpClient = AppHostFixture.App!.CreateHttpClient("api");

        // Create a world first
        var worldRequest = new { Name = "Test World for Filter by Type", Description = "World to test filter by type" };
        using var worldResponse = await httpClient.PostAsJsonAsync("/api/v1/worlds", worldRequest, cancellationToken);
        worldResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var worldId = worldResponse.Headers.Location!.Segments.Last();

        // Create entities of different types
        var characterRequest = new { Name = "Test Character", Description = "A character", EntityType = EntityType.Character };
        await httpClient.PostAsJsonAsync($"/api/v1/worlds/{worldId}/entities", characterRequest, cancellationToken);

        var locationRequest = new { Name = "Test Location", Description = "A location", EntityType = EntityType.Location };
        await httpClient.PostAsJsonAsync($"/api/v1/worlds/{worldId}/entities", locationRequest, cancellationToken);

        // Act - Filter entities by type
        using var response = await httpClient.GetAsync($"/api/v1/worlds/{worldId}/entities?type=Character", cancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK, "API should return 200 OK for GET /api/v1/worlds/{worldId}/entities?type=Character");

        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        content.Should().NotBeNullOrWhiteSpace("Response should have content");
        content.Should().Contain("Test Character", "Response should contain the character entity");
    }

    [TestMethod]
    public async Task FilterEntitiesByTags_ReturnsSuccessStatusCode()
    {
        // Arrange
        var cancellationToken = TestContext!.CancellationTokenSource.Token;
        using var httpClient = AppHostFixture.App!.CreateHttpClient("api");

        // Create a world first
        var worldRequest = new { Name = "Test World for Filter by Tags", Description = "World to test filter by tags" };
        using var worldResponse = await httpClient.PostAsJsonAsync("/api/v1/worlds", worldRequest, cancellationToken);
        worldResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var worldId = worldResponse.Headers.Location!.Segments.Last();

        // Create an entity with tags
        var entityRequest = new
        {
            Name = "Tagged Entity",
            Description = "An entity with tags",
            EntityType = EntityType.Character,
            Tags = new[] { "npc", "merchant" }
        };
        await httpClient.PostAsJsonAsync($"/api/v1/worlds/{worldId}/entities", entityRequest, cancellationToken);

        // Act - Filter entities by tags
        using var response = await httpClient.GetAsync($"/api/v1/worlds/{worldId}/entities?tags=npc,merchant", cancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK, "API should return 200 OK for GET /api/v1/worlds/{worldId}/entities?tags=npc,merchant");

        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        content.Should().NotBeNullOrWhiteSpace("Response should have content");
        content.Should().Contain("Tagged Entity", "Response should contain the tagged entity");
    }

    [TestMethod]
    public async Task SearchWithPagination_ReturnsSuccessStatusCode()
    {
        // Arrange
        var cancellationToken = TestContext!.CancellationTokenSource.Token;
        using var httpClient = AppHostFixture.App!.CreateHttpClient("api");

        // Create a world first
        var worldRequest = new { Name = "Test World for Pagination", Description = "World to test pagination" };
        using var worldResponse = await httpClient.PostAsJsonAsync("/api/v1/worlds", worldRequest, cancellationToken);
        worldResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var worldId = worldResponse.Headers.Location!.Segments.Last();

        // Create multiple entities
        for (int i = 0; i < 5; i++)
        {
            var entityRequest = new { Name = $"Entity {i}", Description = $"Description {i}", EntityType = EntityType.Character };
            await httpClient.PostAsJsonAsync($"/api/v1/worlds/{worldId}/entities", entityRequest, cancellationToken);
        }

        // Act - Search with pagination
        using var response = await httpClient.GetAsync($"/api/v1/worlds/{worldId}/entities?limit=2", cancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK, "API should return 200 OK for GET /api/v1/worlds/{worldId}/entities?limit=2");

        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        content.Should().NotBeNullOrWhiteSpace("Response should have content");
    }

    [TestMethod]
    public async Task SearchWithSorting_ReturnsSuccessStatusCode()
    {
        // Arrange
        var cancellationToken = TestContext!.CancellationTokenSource.Token;
        using var httpClient = AppHostFixture.App!.CreateHttpClient("api");

        // Create a world first
        var worldRequest = new { Name = "Test World for Sorting", Description = "World to test sorting" };
        using var worldResponse = await httpClient.PostAsJsonAsync("/api/v1/worlds", worldRequest, cancellationToken);
        worldResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var worldId = worldResponse.Headers.Location!.Segments.Last();

        // Create entities with different names
        var entityA = new { Name = "Alpha Entity", Description = "First alphabetically", EntityType = EntityType.Character };
        await httpClient.PostAsJsonAsync($"/api/v1/worlds/{worldId}/entities", entityA, cancellationToken);

        var entityZ = new { Name = "Zulu Entity", Description = "Last alphabetically", EntityType = EntityType.Character };
        await httpClient.PostAsJsonAsync($"/api/v1/worlds/{worldId}/entities", entityZ, cancellationToken);

        // Act - Search with sorting
        using var response = await httpClient.GetAsync($"/api/v1/worlds/{worldId}/entities?sort=name&order=asc", cancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK, "API should return 200 OK for GET /api/v1/worlds/{worldId}/entities?sort=name&order=asc");

        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        content.Should().NotBeNullOrWhiteSpace("Response should have content");
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
