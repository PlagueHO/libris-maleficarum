namespace LibrisMaleficarum.Api.IntegrationTests;

using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using LibrisMaleficarum.Api.Models.Responses;
using LibrisMaleficarum.Domain.ValueObjects;
using LibrisMaleficarum.IntegrationTests.Shared;

/// <summary>
/// Performance tests for delete operations to establish baseline metrics.
/// </summary>
[TestClass]
[TestCategory("Performance")]
[TestCategory("Integration")]
[TestCategory("RequiresDocker")]
[DoNotParallelize]
public class PerformanceTests
{
    public TestContext? TestContext { get; set; }

    [ClassInitialize]
public static async Task ClassInitialize(TestContext context)
    {
        await AppHostFixture.InitializeAsync(context);
    }

    /// <summary>
    /// Tests that DELETE endpoint returns 202 Accepted within 200ms.
    /// This measures API response time, not full processing time.
    /// </summary>
    [TestMethod]
    public async Task DeleteEntity_ReturnsWithin200ms()
    {
        // Arrange: Create a world and a simple entity
        var cancellationToken = TestContext!.CancellationTokenSource.Token;
        using var httpClient = AppHostFixture.App!.CreateHttpClient("api");

        // Create world
        var worldRequest = new { Name = "Performance Test World", Description = "Testing performance" };
        using var worldResponse = await httpClient.PostAsJsonAsync("/api/v1/worlds", worldRequest, cancellationToken);
        worldResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var worldId = Guid.Parse(worldResponse.Headers.Location!.Segments.Last());

        // Create entity
        var createRequest = new { Name = "Test Entity", EntityType = EntityType.Location };
        using var entityResponse = await httpClient.PostAsJsonAsync($"/api/v1/worlds/{worldId}/entities", createRequest, cancellationToken);
        entityResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var entityId = Guid.Parse(entityResponse.Headers.Location!.Segments.Last());

        // Act: Measure DELETE response time
        var stopwatch = Stopwatch.StartNew();
        using var response = await httpClient.DeleteAsync($"/api/v1/worlds/{worldId}/entities/{entityId}?cascade=false", cancellationToken);
        stopwatch.Stop();

        // Assert: Response received and within 200ms
        response.StatusCode.Should().Be(HttpStatusCode.Accepted, "DELETE should return 202 Accepted");
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(200,
            "DELETE endpoint should respond within 200ms (excluding network latency)");

        // Verify response includes Location header
        response.Headers.Location.Should().NotBeNull("202 response should include Location header for status polling");
    }

    /// <summary>
    /// Tests that a large cascade delete operation completes efficiently.
    /// Creates a hierarchy of 100 entities and verifies completion within 30 seconds.
    /// Reduced from 500+ entities to improve CI performance.
    /// </summary>
    [TestMethod]
    [Timeout(60000)] // 60 second timeout for entire test
    public async Task DeleteEntity_With100PlusEntities_CompletesWithin30Seconds()
    {
        // Arrange: Create hierarchy - 1 root + 10 branches + 90 children = 101 entities
        var cancellationToken = TestContext!.CancellationTokenSource.Token;
        using var httpClient = AppHostFixture.App!.CreateHttpClient("api");
        var jsonOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

        // Create world
        var worldRequest = new { Name = "Large Cascade Test World", Description = "Testing large cascade" };
        using var worldResponse = await httpClient.PostAsJsonAsync("/api/v1/worlds", worldRequest, cancellationToken);
        worldResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var worldId = Guid.Parse(worldResponse.Headers.Location!.Segments.Last());

        // Create root entity
        var rootRequest = new { Name = "Root", EntityType = EntityType.Location };
        using var rootResponse = await httpClient.PostAsJsonAsync($"/api/v1/worlds/{worldId}/entities", rootRequest, cancellationToken);
        rootResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var rootId = Guid.Parse(rootResponse.Headers.Location!.Segments.Last());

        // Create 10 branch entities under root
        var branchIds = new List<Guid>();
        for (var b = 0; b < 10; b++)
        {
            var branchRequest = new { Name = $"Branch {b + 1}", EntityType = EntityType.Location, ParentId = rootId };
            using var branchResponse = await httpClient.PostAsJsonAsync($"/api/v1/worlds/{worldId}/entities", branchRequest, cancellationToken);
            branchResponse.StatusCode.Should().Be(HttpStatusCode.Created);
            var branchId = Guid.Parse(branchResponse.Headers.Location!.Segments.Last());
            branchIds.Add(branchId);

            // Create 9 children under each branch (parallelized for speed)
            var childTasks = Enumerable.Range(1, 9).Select(async c =>
            {
                var childRequest = new { Name = $"Branch {b + 1} - Child {c}", EntityType = EntityType.Location, ParentId = branchId };
                using var childResponse = await httpClient.PostAsJsonAsync($"/api/v1/worlds/{worldId}/entities", childRequest, cancellationToken);
                childResponse.StatusCode.Should().Be(HttpStatusCode.Created);
            });

            await Task.WhenAll(childTasks);
        }

        // Act: Initiate cascade delete and measure completion time
        var overallStopwatch = Stopwatch.StartNew();

        using var deleteResponse = await httpClient.DeleteAsync($"/api/v1/worlds/{worldId}/entities/{rootId}?cascade=true", cancellationToken);
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.Accepted);

        // Extract operation ID from response
        var content = await deleteResponse.Content.ReadAsStringAsync(cancellationToken);
        var apiResponse = JsonSerializer.Deserialize<ApiResponse<DeleteOperationResponse>>(content, jsonOptions);
        var operationId = apiResponse!.Data.Id;

        // Poll for completion
        var completed = false;
        DeleteOperationResponse? finalOperation = null;

        while (overallStopwatch.Elapsed < TimeSpan.FromSeconds(30) && !completed)
        {
            using var statusResponse = await httpClient.GetAsync($"/api/v1/worlds/{worldId}/delete-operations/{operationId}", cancellationToken);
            statusResponse.StatusCode.Should().Be(HttpStatusCode.OK);

            var statusContent = await statusResponse.Content.ReadAsStringAsync(cancellationToken);
            var statusApiResponse = JsonSerializer.Deserialize<ApiResponse<DeleteOperationResponse>>(statusContent, jsonOptions);
            finalOperation = statusApiResponse!.Data;

            if (finalOperation.Status == "completed" || finalOperation.Status == "failed" || finalOperation.Status == "partial")
            {
                completed = true;
                break;
            }

            await Task.Delay(500, cancellationToken); // Poll every 500ms
        }

        overallStopwatch.Stop();

        // Assert: Operation completed successfully within time limit
        completed.Should().BeTrue("cascade delete should complete within 30 seconds");
        finalOperation.Should().NotBeNull();
        finalOperation!.Status.Should().Be("completed", "all 101 entities should be deleted successfully");
        finalOperation.TotalEntities.Should().Be(101, "should process root + 10 branches + 90 children");
        finalOperation.DeletedCount.Should().Be(101, "should successfully delete all entities");
        finalOperation.FailedCount.Should().Be(0, "should have zero failures");

        overallStopwatch.Elapsed.Should().BeLessThan(TimeSpan.FromSeconds(30),
            "cascade delete of 101 entities should complete within 30 seconds");

        // Output performance metrics
        TestContext!.WriteLine($"✓ Cascade delete performance metrics:");
        TestContext.WriteLine($"  - Total entities: {finalOperation.TotalEntities}");
        TestContext.WriteLine($"  - Deleted count: {finalOperation.DeletedCount}");
        TestContext.WriteLine($"  - Total time: {overallStopwatch.ElapsedMilliseconds}ms");
        TestContext.WriteLine($"  - Average per entity: {overallStopwatch.ElapsedMilliseconds / (double)finalOperation.TotalEntities:F2}ms");
    }

    /// <summary>
    /// Tests that a medium-sized cascade (20 entities) completes quickly.
    /// This is a more realistic scenario for typical user operations.
    /// </summary>
    [TestMethod]
    public async Task DeleteEntity_With20Entities_CompletesWithin5Seconds()
    {
        // Arrange: Create hierarchy - 1 root + 19 children = 20 entities
        var cancellationToken = TestContext!.CancellationTokenSource.Token;
        using var httpClient = AppHostFixture.App!.CreateHttpClient("api");
        var jsonOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

        // Create world
        var worldRequest = new { Name = "Medium Cascade Test World", Description = "Testing medium cascade" };
        using var worldResponse = await httpClient.PostAsJsonAsync("/api/v1/worlds", worldRequest, cancellationToken);
        worldResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var worldId = Guid.Parse(worldResponse.Headers.Location!.Segments.Last());

        // Create root entity
        var rootRequest = new { Name = "Root", EntityType = EntityType.Location };
        using var rootResponse = await httpClient.PostAsJsonAsync($"/api/v1/worlds/{worldId}/entities", rootRequest, cancellationToken);
        rootResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var rootId = Guid.Parse(rootResponse.Headers.Location!.Segments.Last());

        // Create 19 children under root (parallelized)
        var childTasks = Enumerable.Range(1, 19).Select(async i =>
        {
            var childRequest = new { Name = $"Child {i}", EntityType = EntityType.Location, ParentId = rootId };
            using var childResponse = await httpClient.PostAsJsonAsync($"/api/v1/worlds/{worldId}/entities", childRequest, cancellationToken);
            childResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        });

        await Task.WhenAll(childTasks);

        // Act: Initiate cascade delete and measure completion time
        var overallStopwatch = Stopwatch.StartNew();

        using var deleteResponse = await httpClient.DeleteAsync($"/api/v1/worlds/{worldId}/entities/{rootId}?cascade=true", cancellationToken);
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.Accepted);

        // Extract operation ID from response
        var content = await deleteResponse.Content.ReadAsStringAsync(cancellationToken);
        var apiResponse = JsonSerializer.Deserialize<ApiResponse<DeleteOperationResponse>>(content, jsonOptions);
        var operationId = apiResponse!.Data.Id;

        // Poll for completion
        var completed = false;
        DeleteOperationResponse? finalOperation = null;

        while (overallStopwatch.Elapsed < TimeSpan.FromSeconds(5) && !completed)
        {
            using var statusResponse = await httpClient.GetAsync($"/api/v1/worlds/{worldId}/delete-operations/{operationId}", cancellationToken);
            statusResponse.StatusCode.Should().Be(HttpStatusCode.OK);

            var statusContent = await statusResponse.Content.ReadAsStringAsync(cancellationToken);
            var statusApiResponse = JsonSerializer.Deserialize<ApiResponse<DeleteOperationResponse>>(statusContent, jsonOptions);
            finalOperation = statusApiResponse!.Data;

            if (finalOperation.Status == "completed" || finalOperation.Status == "failed" || finalOperation.Status == "partial")
            {
                completed = true;
                break;
            }

            await Task.Delay(200, cancellationToken); // Poll every 200ms
        }

        overallStopwatch.Stop();

        // Assert: Operation completed successfully within time limit
        completed.Should().BeTrue("cascade delete should complete within 5 seconds");
        finalOperation.Should().NotBeNull();
        finalOperation!.Status.Should().Be("completed", "all 20 entities should be deleted successfully");
        finalOperation.TotalEntities.Should().Be(20);
        finalOperation.DeletedCount.Should().Be(20);
        finalOperation.FailedCount.Should().Be(0);

        overallStopwatch.Elapsed.Should().BeLessThan(TimeSpan.FromSeconds(5),
            "cascade delete of 20 entities should complete within 5 seconds");

        // Output performance metrics
        TestContext!.WriteLine($"✓ Medium cascade delete performance:");
        TestContext.WriteLine($"  - Total entities: {finalOperation.TotalEntities}");
        TestContext.WriteLine($"  - Total time: {overallStopwatch.ElapsedMilliseconds}ms");
    }
}
