namespace LibrisMaleficarum.Api.IntegrationTests;

using LibrisMaleficarum.Domain.ValueObjects;
using LibrisMaleficarum.Infrastructure.Persistence;
using LibrisMaleficarum.IntegrationTests.Shared;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// End-to-end integration tests that verify complete workflows across the entire API.
/// These tests exercise multiple endpoints in sequence to validate full user scenarios.
/// Uses shared AppHostFixture from IntegrationTests.Shared project to avoid Docker container conflicts.
/// </summary>
[TestClass]
[TestCategory("Integration")]
[TestCategory("RequiresDocker")]
[DoNotParallelize] // AppHost tests must run sequentially to avoid port conflicts
public class EndToEndIntegrationTests
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
    public async Task EndToEnd_CreateWorldToSearchEntities_CompleteWorkflow()
    {
        // This test validates the complete user journey:
        // 1. Create a world
        // 2. Create multiple entities within that world
        // 3. Upload an asset to one entity
        // 4. Search for entities
        // 5. Verify all data is correct and interconnected

        // Arrange
        var cancellationToken = TestContext!.CancellationTokenSource.Token;
        using var httpClient = AppHostFixture.App!.CreateHttpClient("api");

        // Step 1: Create a world
        TestContext.WriteLine("[STEP 1] Creating world...");
        var worldRequest = new { Name = "End-to-End Test World", Description = "A comprehensive test world for E2E validation" };
        using var worldResponse = await httpClient.PostAsJsonAsync("/api/v1/worlds", worldRequest, cancellationToken);
        worldResponse.StatusCode.Should().Be(HttpStatusCode.Created, "World creation should succeed");
        worldResponse.Headers.Location.Should().NotBeNull("World creation should return Location header");
        var worldId = worldResponse.Headers.Location!.Segments.Last();
        TestContext.WriteLine($"[STEP 1] ✓ World created with ID: {worldId}");

        // Step 2a: Create first entity (Character)
        TestContext.WriteLine("[STEP 2a] Creating character entity...");
        var characterRequest = new
        {
            Name = "Aragorn",
            Description = "A ranger from the North, heir to the throne of Gondor",
            EntityType = EntityType.Character,
            Tags = new[] { "protagonist", "ranger", "king" }
        };
        using var characterResponse = await httpClient.PostAsJsonAsync(
            $"/api/v1/worlds/{worldId}/entities",
            characterRequest,
            cancellationToken);
        characterResponse.StatusCode.Should().Be(HttpStatusCode.Created, "Character entity creation should succeed");
        var characterId = characterResponse.Headers.Location!.Segments.Last();
        TestContext.WriteLine($"[STEP 2a] ✓ Character entity created with ID: {characterId}");

        // Step 2b: Create second entity (Location)
        TestContext.WriteLine("[STEP 2b] Creating location entity...");
        var locationRequest = new
        {
            Name = "Rivendell",
            Description = "The elven sanctuary in Middle-earth",
            EntityType = EntityType.Location,
            Tags = new[] { "sanctuary", "elven", "safe-haven" }
        };
        using var locationResponse = await httpClient.PostAsJsonAsync(
            $"/api/v1/worlds/{worldId}/entities",
            locationRequest,
            cancellationToken);
        locationResponse.StatusCode.Should().Be(HttpStatusCode.Created, "Location entity creation should succeed");
        var locationId = locationResponse.Headers.Location!.Segments.Last();
        TestContext.WriteLine($"[STEP 2b] ✓ Location entity created with ID: {locationId}");

        // Step 2c: Create third entity (Item)
        TestContext.WriteLine("[STEP 2c] Creating item entity...");
        var itemRequest = new
        {
            Name = "Andúril",
            Description = "The reforged sword of Elendil, wielded by Aragorn",
            EntityType = EntityType.Item,
            Tags = new[] { "weapon", "sword", "legendary" }
        };
        using var itemResponse = await httpClient.PostAsJsonAsync(
            $"/api/v1/worlds/{worldId}/entities",
            itemRequest,
            cancellationToken);
        itemResponse.StatusCode.Should().Be(HttpStatusCode.Created, "Item entity creation should succeed");
        var itemId = itemResponse.Headers.Location!.Segments.Last();
        TestContext.WriteLine($"[STEP 2c] ✓ Item entity created with ID: {itemId}");

        // Step 3: Upload an asset to the character entity
        TestContext.WriteLine("[STEP 3] Uploading asset to character entity...");
        using var assetContent = new MultipartFormDataContent();
        var imageBytes = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }; // PNG header
        var fileContent = new ByteArrayContent(imageBytes);
        fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/png");
        assetContent.Add(fileContent, "file", "aragorn-portrait.png");

        using var assetResponse = await httpClient.PostAsync(
            $"/api/v1/worlds/{worldId}/entities/{characterId}/assets",
            assetContent,
            cancellationToken);
        assetResponse.StatusCode.Should().Be(HttpStatusCode.Created, "Asset upload should succeed");
        assetResponse.Headers.Location.Should().NotBeNull("Asset upload should return Location header");
        var assetId = assetResponse.Headers.Location!.Segments.Last();
        TestContext.WriteLine($"[STEP 3] ✓ Asset uploaded with ID: {assetId}");

        // Step 4a: Search for entities by tag
        TestContext.WriteLine("[STEP 4a] Searching entities by tag 'ranger'...");
        using var searchByTagResponse = await httpClient.GetAsync(
            $"/api/v1/worlds/{worldId}/entities?tags=ranger",
            cancellationToken);
        searchByTagResponse.StatusCode.Should().Be(HttpStatusCode.OK, "Search by tag should succeed");
        var searchByTagContent = await searchByTagResponse.Content.ReadAsStringAsync(cancellationToken);
        searchByTagContent.Should().Contain("Aragorn", "Search results should contain character with 'ranger' tag");
        searchByTagContent.Should().NotContain("Rivendell", "Search results should not contain entities without 'ranger' tag");
        TestContext.WriteLine("[STEP 4a] ✓ Search by tag returned correct results");

        // Step 4b: Search for entities by type
        TestContext.WriteLine("[STEP 4b] Searching entities by type 'Location'...");
        using var searchByTypeResponse = await httpClient.GetAsync(
            $"/api/v1/worlds/{worldId}/entities?type={EntityType.Location}",
            cancellationToken);
        searchByTypeResponse.StatusCode.Should().Be(HttpStatusCode.OK, "Search by type should succeed");
        var searchByTypeContent = await searchByTypeResponse.Content.ReadAsStringAsync(cancellationToken);
        searchByTypeContent.Should().Contain("Rivendell", "Search results should contain location entity");
        searchByTypeContent.Should().NotContain("Aragorn", "Search results should not contain non-location entities");
        searchByTypeContent.Should().NotContain("Andúril", "Search results should not contain non-location entities");
        TestContext.WriteLine("[STEP 4b] ✓ Search by type returned correct results");

        // Step 5a: Verify world data
        TestContext.WriteLine("[STEP 5a] Verifying world data...");
        using var worldGetResponse = await httpClient.GetAsync($"/api/v1/worlds/{worldId}", cancellationToken);
        worldGetResponse.StatusCode.Should().Be(HttpStatusCode.OK, "World retrieval should succeed");
        var worldContent = await worldGetResponse.Content.ReadAsStringAsync(cancellationToken);
        worldContent.Should().Contain("End-to-End Test World", "World data should be correct");
        TestContext.WriteLine("[STEP 5a] ✓ World data verified");

        // Step 5b: Verify character entity data
        TestContext.WriteLine("[STEP 5b] Verifying character entity data...");
        using var characterGetResponse = await httpClient.GetAsync(
            $"/api/v1/worlds/{worldId}/entities/{characterId}",
            cancellationToken);
        characterGetResponse.StatusCode.Should().Be(HttpStatusCode.OK, "Character entity retrieval should succeed");
        var characterContent = await characterGetResponse.Content.ReadAsStringAsync(cancellationToken);
        characterContent.Should().Contain("Aragorn", "Character data should be correct");
        characterContent.Should().Contain("ranger", "Character tags should be correct");
        TestContext.WriteLine("[STEP 5b] ✓ Character entity data verified");

        // Step 5c: Verify asset data
        TestContext.WriteLine("[STEP 5c] Verifying asset data...");
        using var assetGetResponse = await httpClient.GetAsync(
            $"/api/v1/worlds/{worldId}/assets/{assetId}",
            cancellationToken);
        assetGetResponse.StatusCode.Should().Be(HttpStatusCode.OK, "Asset retrieval should succeed");
        var assetContentText = await assetGetResponse.Content.ReadAsStringAsync(cancellationToken);
        assetContentText.Should().Contain("aragorn-portrait.png", "Asset data should be correct");
        assetContentText.Should().Contain("image/png", "Asset content type should be correct");
        TestContext.WriteLine("[STEP 5c] ✓ Asset data verified");

        // Step 5d: Verify asset is linked to character entity
        TestContext.WriteLine("[STEP 5d] Verifying asset is linked to character entity...");
        using var entityAssetsResponse = await httpClient.GetAsync(
            $"/api/v1/worlds/{worldId}/entities/{characterId}/assets",
            cancellationToken);
        entityAssetsResponse.StatusCode.Should().Be(HttpStatusCode.OK, "Entity assets retrieval should succeed");
        var entityAssetsContent = await entityAssetsResponse.Content.ReadAsStringAsync(cancellationToken);
        entityAssetsContent.Should().Contain(assetId.TrimEnd('/'), "Entity should have the uploaded asset");
        TestContext.WriteLine("[STEP 5d] ✓ Asset linkage to entity verified");

        // Step 6: List all entities in world
        TestContext.WriteLine("[STEP 6] Listing all entities in world...");
        using var allEntitiesResponse = await httpClient.GetAsync(
            $"/api/v1/worlds/{worldId}/entities",
            cancellationToken);
        allEntitiesResponse.StatusCode.Should().Be(HttpStatusCode.OK, "List all entities should succeed");
        var allEntitiesContent = await allEntitiesResponse.Content.ReadAsStringAsync(cancellationToken);
        allEntitiesContent.Should().Contain("Aragorn", "All entities list should contain character");
        allEntitiesContent.Should().Contain("Rivendell", "All entities list should contain location");
        allEntitiesContent.Should().Contain("Andúril", "All entities list should contain item");
        TestContext.WriteLine("[STEP 6] ✓ All entities listed correctly");

        TestContext.WriteLine("\n[END-TO-END TEST] ✓✓✓ Complete workflow verified successfully! ✓✓✓");
    }

    [TestMethod]
    public async Task EndToEnd_CreateHierarchicalEntities_VerifyParentChildRelationships()
    {
        // This test validates hierarchical entity relationships:
        // 1. Create a world
        // 2. Create a parent entity (Continent)
        // 3. Create child entities (Countries)
        // 4. Verify parent-child relationships

        // Arrange
        var cancellationToken = TestContext!.CancellationTokenSource.Token;
        using var httpClient = AppHostFixture.App!.CreateHttpClient("api");

        // Step 1: Create a world
        TestContext.WriteLine("[STEP 1] Creating world for hierarchy test...");
        var worldRequest = new { Name = "Hierarchy Test World", Description = "Testing parent-child relationships" };
        using var worldResponse = await httpClient.PostAsJsonAsync("/api/v1/worlds", worldRequest, cancellationToken);
        worldResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var worldId = worldResponse.Headers.Location!.Segments.Last();
        TestContext.WriteLine($"[STEP 1] ✓ World created with ID: {worldId}");

        // Step 2: Create parent entity (Continent)
        TestContext.WriteLine("[STEP 2] Creating parent continent entity...");
        var continentRequest = new
        {
            Name = "Middle-earth",
            Description = "The continent of Middle-earth",
            EntityType = EntityType.Continent
        };
        using var continentResponse = await httpClient.PostAsJsonAsync(
            $"/api/v1/worlds/{worldId}/entities",
            continentRequest,
            cancellationToken);
        continentResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var continentId = continentResponse.Headers.Location!.Segments.Last();
        TestContext.WriteLine($"[STEP 2] ✓ Continent created with ID: {continentId}");

        // Step 3a: Create first child entity (Country)
        TestContext.WriteLine("[STEP 3a] Creating first child country entity...");
        var gondorRequest = new
        {
            Name = "Gondor",
            Description = "The kingdom of Gondor",
            EntityType = EntityType.Country,
            ParentId = Guid.Parse(continentId.TrimEnd('/'))
        };
        using var gondorResponse = await httpClient.PostAsJsonAsync(
            $"/api/v1/worlds/{worldId}/entities",
            gondorRequest,
            cancellationToken);
        gondorResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var gondorId = gondorResponse.Headers.Location!.Segments.Last();
        TestContext.WriteLine($"[STEP 3a] ✓ Gondor created with ID: {gondorId}");

        // Step 3b: Create second child entity (Country)
        TestContext.WriteLine("[STEP 3b] Creating second child country entity...");
        var rohanRequest = new
        {
            Name = "Rohan",
            Description = "The kingdom of Rohan",
            EntityType = EntityType.Country,
            ParentId = Guid.Parse(continentId.TrimEnd('/'))
        };
        using var rohanResponse = await httpClient.PostAsJsonAsync(
            $"/api/v1/worlds/{worldId}/entities",
            rohanRequest,
            cancellationToken);
        rohanResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var rohanId = rohanResponse.Headers.Location!.Segments.Last();
        TestContext.WriteLine($"[STEP 3b] ✓ Rohan created with ID: {rohanId}");

        // Step 4: Verify parent-child relationships
        TestContext.WriteLine("[STEP 4] Verifying parent-child relationships...");
        using var childrenResponse = await httpClient.GetAsync(
            $"/api/v1/worlds/{worldId}/entities/{continentId}/children",
            cancellationToken);
        childrenResponse.StatusCode.Should().Be(HttpStatusCode.OK, "Get children should succeed");
        var childrenContent = await childrenResponse.Content.ReadAsStringAsync(cancellationToken);
        childrenContent.Should().Contain("Gondor", "Children should include Gondor");
        childrenContent.Should().Contain("Rohan", "Children should include Rohan");
        TestContext.WriteLine("[STEP 4] ✓ Parent-child relationships verified");

        TestContext.WriteLine("\n[HIERARCHY TEST] ✓✓✓ Hierarchical relationships verified successfully! ✓✓✓");
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
