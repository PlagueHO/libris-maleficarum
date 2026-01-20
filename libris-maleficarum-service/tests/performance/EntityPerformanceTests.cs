using System.Diagnostics;
using System.Net.Http.Json;
using Aspire.Hosting;
using Aspire.Hosting.Testing;
using LibrisMaleficarum.Api.Models.Requests;
using LibrisMaleficarum.Api.Models.Responses;
using LibrisMaleficarum.Domain.ValueObjects;
using LibrisMaleficarum.IntegrationTests.Shared;

namespace LibrisMaleficarum.Performance.Tests;

/// <summary>
/// Performance tests for Entity Management API.
/// These tests verify pagination performance requirements from T157:
/// - Create 1000 entities in a single world
/// - Verify pagination performance
/// 
/// Run with: dotnet test --filter TestCategory=Performance
/// </summary>
[TestClass]
[TestCategory("Performance")]
public class EntityPerformanceTests
{
    private static DistributedApplication? _app;
    private static HttpClient? _client;

    [ClassInitialize]
    public static async Task ClassInitialize(TestContext context)
    {
        var appHost = await DistributedApplicationTestingBuilder
            .CreateAsync<Projects.LibrisMaleficarum_AppHost>();

        _app = await appHost.BuildAsync();
        await _app.StartAsync();

        _client = _app.CreateHttpClient("api");
    }

    [ClassCleanup]
    public static async Task ClassCleanup()
    {
        if (_app != null)
        {
            await _app.DisposeAsync();
        }
        _client?.Dispose();
    }

    /// <summary>
    /// T157: Create 1000 entities in a single world and verify pagination performance.
    /// </summary>
    [TestMethod]
    [TestCategory("Performance")]
    public async Task CreateAndPaginateEntities_1000Entities_PerformanceAcceptable()
    {
        // Arrange - Create a test world
        var createWorldRequest = new CreateWorldRequest
        {
            Name = "Performance Test World for Entities",
            Description = "World created to test entity pagination performance"
        };

        var createWorldResponse = await _client!.PostAsJsonAsync("/api/v1/worlds", createWorldRequest);
        createWorldResponse.IsSuccessStatusCode.Should().BeTrue();
        var createdWorld = await createWorldResponse.Content.ReadFromJsonAsync<ApiResponse<WorldResponse>>();
        var worldId = createdWorld!.Data!.Id;

        const int entityCount = 1000;
        Console.WriteLine($"Creating {entityCount} entities in world {worldId}...");

        // Act Part 1 - Create 1000 entities and measure total time
        var createStopwatch = Stopwatch.StartNew();
        var createTasks = new List<Task>(entityCount);

        for (int i = 0; i < entityCount; i++)
        {
            var index = i; // Capture for closure
            createTasks.Add(Task.Run(async () =>
            {
                var entityRequest = new CreateWorldEntityRequest
                {
                    Name = $"Performance Entity {index}",
                    Description = $"Entity created for performance testing iteration {index}",
                    EntityType = EntityType.Character,
                    Tags = new List<string> { "performance", "test", $"batch-{index / 100}" }
                };

                var response = await _client!.PostAsJsonAsync(
                    $"/api/v1/worlds/{worldId}/entities",
                    entityRequest);

                response.IsSuccessStatusCode.Should().BeTrue(
                    $"Entity creation request {index} should succeed");
            }));
        }

        await Task.WhenAll(createTasks);
        createStopwatch.Stop();

        Console.WriteLine($"Created {entityCount} entities in {createStopwatch.ElapsedMilliseconds}ms");
        Console.WriteLine($"Average creation time: {createStopwatch.ElapsedMilliseconds / (double)entityCount:F2}ms per entity");

        // Act Part 2 - Test pagination performance with different page sizes
        var paginationResults = new List<(int PageSize, long ResponseTime, int PageCount)>();
        var pageSizes = new[] { 50, 100, 200 }; // Test default, medium, and max page sizes

        foreach (var pageSize in pageSizes)
        {
            var pageStopwatch = Stopwatch.StartNew();
            var pageCount = 0;
            string? cursor = null;
            var totalRetrieved = 0;

            do
            {
                var url = $"/api/v1/worlds/{worldId}/entities?limit={pageSize}";
                if (!string.IsNullOrEmpty(cursor))
                {
                    url += $"&cursor={cursor}";
                }

                var response = await _client!.GetAsync(url);
                response.IsSuccessStatusCode.Should().BeTrue(
                    $"Pagination request for page size {pageSize} should succeed");

                var page = await response.Content.ReadFromJsonAsync<PaginatedApiResponse<List<EntityResponse>>>();
                page.Should().NotBeNull();
                page!.Data.Should().NotBeNull();

                totalRetrieved += page.Data!.Count;
                pageCount++;
                cursor = page.Meta?.NextCursor;

            } while (!string.IsNullOrEmpty(cursor));

            pageStopwatch.Stop();

            paginationResults.Add((pageSize, pageStopwatch.ElapsedMilliseconds, pageCount));
            totalRetrieved.Should().Be(entityCount,
                $"Should retrieve all {entityCount} entities with page size {pageSize}");
        }

        // Assert - Log and verify pagination performance
        Console.WriteLine("\nPagination Performance Results:");
        Console.WriteLine("Page Size | Total Time | Page Count | Avg Time/Page");
        Console.WriteLine("----------|------------|------------|---------------");

        foreach (var (pageSize, responseTime, pageCount) in paginationResults)
        {
            var avgTimePerPage = responseTime / (double)pageCount;
            Console.WriteLine($"{pageSize,9} | {responseTime,10}ms | {pageCount,10} | {avgTimePerPage,13:F2}ms");

            // Verify reasonable performance - each page should complete in reasonable time
            avgTimePerPage.Should().BeLessThan(500,
                $"Average pagination response time should be reasonable for page size {pageSize}");
        }

        // Verify larger page sizes are more efficient (fewer total requests)
        var smallPageResult = paginationResults.First(r => r.PageSize == 50);
        var largePageResult = paginationResults.First(r => r.PageSize == 200);

        largePageResult.PageCount.Should().BeLessThan(smallPageResult.PageCount,
            "Larger page sizes should require fewer page requests");
    }

    /// <summary>
    /// Performance test for filtering entities by type and tags with large dataset.
    /// </summary>
    [TestMethod]
    [TestCategory("Performance")]
    public async Task FilterEntities_1000Entities_FilterPerformanceAcceptable()
    {
        // Arrange - Create a test world
        var createWorldRequest = new CreateWorldRequest
        {
            Name = "Performance Test World for Filtering",
            Description = "World created to test entity filtering performance"
        };

        var createWorldResponse = await _client!.PostAsJsonAsync("/api/v1/worlds", createWorldRequest);
        createWorldResponse.IsSuccessStatusCode.Should().BeTrue();
        var createdWorld = await createWorldResponse.Content.ReadFromJsonAsync<ApiResponse<WorldResponse>>();
        var worldId = createdWorld!.Data!.Id;

        // Create 1000 entities with different types and tags
        const int entityCount = 1000;
        var entityTypes = new[] { EntityType.Character, EntityType.Location, EntityType.Item, EntityType.Quest };
        var tags = new[] { "common", "rare", "epic", "legendary" };

        Console.WriteLine($"Creating {entityCount} entities with varied types and tags...");

        var createTasks = new List<Task>(entityCount);
        for (int i = 0; i < entityCount; i++)
        {
            var index = i;
            createTasks.Add(Task.Run(async () =>
            {
                var entityRequest = new CreateWorldEntityRequest
                {
                    Name = $"Filter Test Entity {index}",
                    Description = $"Entity for filter testing",
                    EntityType = entityTypes[index % entityTypes.Length],
                    Tags = new List<string> { tags[index % tags.Length], $"group-{index / 250}" }
                };

                var response = await _client!.PostAsJsonAsync(
                    $"/api/v1/worlds/{worldId}/entities",
                    entityRequest);

                response.IsSuccessStatusCode.Should().BeTrue();
            }));
        }

        await Task.WhenAll(createTasks);

        // Act - Test filtering performance
        var filterTests = new List<(string Description, string QueryString, long ResponseTime, int ResultCount)>();

        // Test 1: Filter by type
        var typeFilterStopwatch = Stopwatch.StartNew();
        var typeFilterResponse = await _client!.GetAsync(
            $"/api/v1/worlds/{worldId}/entities?type={EntityType.Character}&limit=200");
        typeFilterStopwatch.Stop();
        typeFilterResponse.IsSuccessStatusCode.Should().BeTrue();
        var typeFilterResult = await typeFilterResponse.Content.ReadFromJsonAsync<PaginatedApiResponse<List<EntityResponse>>>();
        filterTests.Add(("Filter by Type (Character)", $"type={EntityType.Character}",
            typeFilterStopwatch.ElapsedMilliseconds, typeFilterResult!.Data!.Count));

        // Test 2: Filter by tag
        var tagFilterStopwatch = Stopwatch.StartNew();
        var tagFilterResponse = await _client!.GetAsync(
            $"/api/v1/worlds/{worldId}/entities?tags=rare&limit=200");
        tagFilterStopwatch.Stop();
        tagFilterResponse.IsSuccessStatusCode.Should().BeTrue();
        var tagFilterResult = await tagFilterResponse.Content.ReadFromJsonAsync<PaginatedApiResponse<List<EntityResponse>>>();
        filterTests.Add(("Filter by Tag (rare)", "tags=rare",
            tagFilterStopwatch.ElapsedMilliseconds, tagFilterResult!.Data!.Count));

        // Test 3: Combined filter
        var combinedFilterStopwatch = Stopwatch.StartNew();
        var combinedFilterResponse = await _client!.GetAsync(
            $"/api/v1/worlds/{worldId}/entities?type={EntityType.Location}&tags=epic&limit=200");
        combinedFilterStopwatch.Stop();
        combinedFilterResponse.IsSuccessStatusCode.Should().BeTrue();
        var combinedFilterResult = await combinedFilterResponse.Content.ReadFromJsonAsync<PaginatedApiResponse<List<EntityResponse>>>();
        filterTests.Add(("Combined Filter (Location + epic)", $"type={EntityType.Location}&tags=epic",
            combinedFilterStopwatch.ElapsedMilliseconds, combinedFilterResult!.Data!.Count));

        // Assert - Log and verify filtering performance
        Console.WriteLine("\nFiltering Performance Results:");
        Console.WriteLine("Test Description              | Query String                  | Response Time | Result Count");
        Console.WriteLine("------------------------------|-------------------------------|---------------|-------------");

        foreach (var (description, queryString, responseTime, resultCount) in filterTests)
        {
            Console.WriteLine($"{description,-29} | {queryString,-29} | {responseTime,13}ms | {resultCount,12}");

            // Verify filtering completes in reasonable time
            responseTime.Should().BeLessThan(1000,
                $"Filter query '{description}' should complete in reasonable time");
        }
    }
}
