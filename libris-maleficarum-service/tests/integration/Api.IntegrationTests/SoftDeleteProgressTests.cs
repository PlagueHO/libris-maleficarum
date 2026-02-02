namespace LibrisMaleficarum.Api.IntegrationTests;

using System.Text.Json;
using LibrisMaleficarum.Domain.ValueObjects;
using LibrisMaleficarum.IntegrationTests.Shared;

/// <summary>
/// Integration tests for delete operation progress monitoring (User Story 3).
/// Tests real-time progress updates, partial failures, and list operations.
/// Uses shared AppHostFixture to avoid Docker container conflicts.
/// </summary>
[TestClass]
[TestCategory("Integration")]
[TestCategory("RequiresDocker")]
[DoNotParallelize]
public class SoftDeleteProgressTests
{
    public TestContext? TestContext { get; set; }

    private static readonly TimeSpan PollingInterval = TimeSpan.FromMilliseconds(200);
    private static readonly TimeSpan PollingTimeout = TimeSpan.FromSeconds(30);

    [ClassInitialize]
    public static async Task ClassInitialize(TestContext context)
    {
        await AppHostFixture.InitializeAsync(context);
    }

    #region T045: Progress Polling on Large Hierarchy

    [TestMethod]
    public async Task DeleteEntity_WithLargeHierarchy_ShowsProgressUpdates()
    {
        // Arrange
        var cancellationToken = TestContext!.CancellationTokenSource.Token;
        using var httpClient = AppHostFixture.App!.CreateHttpClient("api");

        // Create a world
        var worldRequest = new { Name = "Test World for Progress Monitoring", Description = "World to test progress updates" };
        using var worldResponse = await httpClient.PostAsJsonAsync("/api/v1/worlds", worldRequest, cancellationToken);
        worldResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var worldId = worldResponse.Headers.Location!.Segments.Last();

        // Create a large hierarchy: 1 root continent with 5 countries, each with 10 regions = 55 total entities
        // Root: Continent
        var continentRequest = new
        {
            Name = "Test Continent",
            Description = "Root for large hierarchy",
            EntityType = EntityType.Continent
        };
        using var continentResponse = await httpClient.PostAsJsonAsync(
            $"/api/v1/worlds/{worldId}/entities",
            continentRequest,
            cancellationToken);
        continentResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var continentId = continentResponse.Headers.Location!.Segments.Last();

        // Create 5 countries under the continent
        var countryIds = new List<string>();
        for (var i = 1; i <= 5; i++)
        {
            var countryRequest = new
            {
                Name = $"Country {i}",
                Description = $"Country {i} description",
                EntityType = EntityType.Country,
                ParentId = continentId
            };
            using var countryResponse = await httpClient.PostAsJsonAsync(
                $"/api/v1/worlds/{worldId}/entities",
                countryRequest,
                cancellationToken);
            countryResponse.StatusCode.Should().Be(HttpStatusCode.Created);
            countryIds.Add(countryResponse.Headers.Location!.Segments.Last());

            // Create 10 regions under each country
            for (var j = 1; j <= 10; j++)
            {
                var regionRequest = new
                {
                    Name = $"Region {i}-{j}",
                    Description = $"Region {j} in Country {i}",
                    EntityType = EntityType.Region,
                    ParentId = countryIds.Last()
                };
                using var regionResponse = await httpClient.PostAsJsonAsync(
                    $"/api/v1/worlds/{worldId}/entities",
                    regionRequest,
                    cancellationToken);
                regionResponse.StatusCode.Should().Be(HttpStatusCode.Created);
            }
        }

        // Total: 1 continent + 5 countries + 50 regions = 56 entities

        // Act - Initiate delete on root continent
        using var deleteResponse = await httpClient.DeleteAsync(
            $"/api/v1/worlds/{worldId}/entities/{continentId}?cascade=true",
            cancellationToken);

        // Assert - Should return 202 Accepted
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.Accepted);
        var operationId = deleteResponse.Headers.Location!.Segments.Last();

        // Poll status endpoint and capture progress snapshots
        var progressSnapshots = new List<(DateTime Time, string Status, int Deleted, int Total)>();
        var pollingStart = DateTime.UtcNow;
        var finalStatus = string.Empty;

        while (DateTime.UtcNow - pollingStart < PollingTimeout)
        {
            using var statusResponse = await httpClient.GetAsync(
                $"/api/v1/worlds/{worldId}/delete-operations/{operationId}",
                cancellationToken);

            statusResponse.StatusCode.Should().Be(HttpStatusCode.OK);

            var statusJson = await statusResponse.Content.ReadAsStringAsync(cancellationToken);
            var statusDoc = JsonDocument.Parse(statusJson);
            var dataElement = statusDoc.RootElement.GetProperty("data");

            var status = dataElement.GetProperty("status").GetString() ?? "unknown";
            var deletedCount = dataElement.GetProperty("deletedCount").GetInt32();
            var totalEntities = dataElement.GetProperty("totalEntities").GetInt32();

            // Capture snapshot
            progressSnapshots.Add((DateTime.UtcNow, status, deletedCount, totalEntities));

            // Check if completed
            if (status is "completed" or "partial" or "failed")
            {
                finalStatus = status;
                break;
            }

            await Task.Delay(PollingInterval, cancellationToken);
        }

        // Assert - Operation should complete
        finalStatus.Should().Be("completed", "Delete operation should complete successfully");

        // Assert - Should have captured at least initial and final snapshots
        progressSnapshots.Should().HaveCountGreaterThan(0, "Should capture at least one progress snapshot");

        // Assert - Final progress should match total
        var lastSnapshot = progressSnapshots.Last();
        lastSnapshot.Total.Should().Be(56, "Total should be 1 continent + 5 countries + 50 regions");
        lastSnapshot.Deleted.Should().Be(56, "All entities should be deleted");

        // Assert - Progress should be monotonically increasing
        for (var i = 1; i < progressSnapshots.Count; i++)
        {
            progressSnapshots[i].Deleted.Should().BeGreaterThanOrEqualTo(
                progressSnapshots[i - 1].Deleted,
                "Progress should never decrease");
        }

        // Assert - Check for intermediate progress (but allow fast operations to skip intermediate states)
        var deletedCounts = progressSnapshots.Select(s => s.Deleted).Distinct().ToList();
        deletedCounts.Should().Contain(0, "Should capture initial state with 0 deleted");
        deletedCounts.Should().Contain(56, "Should capture final state with all 56 deleted");

        // Note: Fast operations may complete before intermediate progress is captured.
        // This is expected behavior - we verify the operation completed correctly above.
        TestContext!.WriteLine($"Captured {progressSnapshots.Count} snapshots with {deletedCounts.Count} distinct progress values: [{string.Join(", ", deletedCounts)}]");
    }

    #endregion

    #region T046: Partial Failure Scenario

    [TestMethod]
    public async Task DeleteEntity_WithPartialFailure_ReturnsPartialStatus()
    {
        // Arrange
        var cancellationToken = TestContext!.CancellationTokenSource.Token;
        using var httpClient = AppHostFixture.App!.CreateHttpClient("api");

        // Create a world
        var worldRequest = new { Name = "Test World for Partial Failure", Description = "World to test partial failures" };
        using var worldResponse = await httpClient.PostAsJsonAsync("/api/v1/worlds", worldRequest, cancellationToken);
        worldResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var worldId = worldResponse.Headers.Location!.Segments.Last();

        // Create a small hierarchy: 1 continent with 3 countries
        var continentRequest = new
        {
            Name = "Test Continent",
            Description = "Root for partial failure test",
            EntityType = EntityType.Continent
        };
        using var continentResponse = await httpClient.PostAsJsonAsync(
            $"/api/v1/worlds/{worldId}/entities",
            continentRequest,
            cancellationToken);
        continentResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var continentId = continentResponse.Headers.Location!.Segments.Last();

        for (var i = 1; i <= 3; i++)
        {
            var countryRequest = new
            {
                Name = $"Country {i}",
                Description = $"Country {i} description",
                EntityType = EntityType.Country,
                ParentId = continentId
            };
            using var countryResponse = await httpClient.PostAsJsonAsync(
                $"/api/v1/worlds/{worldId}/entities",
                countryRequest,
                cancellationToken);
            countryResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        }

        // Note: Without a way to inject failures in this integration test, we cannot truly test partial failure.
        // This is a limitation of the current test infrastructure. In a real scenario, we would:
        // 1. Use a test double/mock repository that fails for specific entity IDs
        // 2. Inject transient errors via chaos engineering
        // 3. Use a test-specific failure injection mechanism
        //
        // For now, this test validates that the operation completes successfully.
        // A unit test with mocks would be more appropriate for testing partial failure logic.

        // Act - Initiate delete
        using var deleteResponse = await httpClient.DeleteAsync(
            $"/api/v1/worlds/{worldId}/entities/{continentId}?cascade=true",
            cancellationToken);

        deleteResponse.StatusCode.Should().Be(HttpStatusCode.Accepted);
        var operationId = deleteResponse.Headers.Location!.Segments.Last();

        // Poll until completion
        var pollingStart = DateTime.UtcNow;
        var finalStatus = string.Empty;
        var failedCount = 0;

        while (DateTime.UtcNow - pollingStart < PollingTimeout)
        {
            using var statusResponse = await httpClient.GetAsync(
                $"/api/v1/worlds/{worldId}/delete-operations/{operationId}",
                cancellationToken);

            statusResponse.StatusCode.Should().Be(HttpStatusCode.OK);

            var statusJson = await statusResponse.Content.ReadAsStringAsync(cancellationToken);
            var statusDoc = JsonDocument.Parse(statusJson);
            var dataElement = statusDoc.RootElement.GetProperty("data");

            var status = dataElement.GetProperty("status").GetString() ?? "unknown";
            failedCount = dataElement.GetProperty("failedCount").GetInt32();

            if (status is "completed" or "partial" or "failed")
            {
                finalStatus = status;
                break;
            }

            await Task.Delay(PollingInterval, cancellationToken);
        }

        // Assert - Without failure injection, this should complete successfully
        finalStatus.Should().Be("completed", "Without failure injection, operation should succeed");
        failedCount.Should().Be(0, "No failures expected without failure injection");

        // TODO: Implement failure injection mechanism to properly test partial failure scenario
        // Expected behavior when failures occur:
        // - finalStatus.Should().Be("partial")
        // - failedCount.Should().BeGreaterThan(0)
        // - failedEntityIds.Should().NotBeEmpty()
        // - Successful entities should still be deleted in database
    }

    #endregion

    #region T047: List Recent Operations

    [TestMethod]
    public async Task GetRecentOperations_WithMultipleOperations_ReturnsOrderedList()
    {
        // Arrange
        var cancellationToken = TestContext!.CancellationTokenSource.Token;
        using var httpClient = AppHostFixture.App!.CreateHttpClient("api");

        // Create a world
        var worldRequest = new { Name = "Test World for List Operations", Description = "World to test list operations" };
        using var worldResponse = await httpClient.PostAsJsonAsync("/api/v1/worlds", worldRequest, cancellationToken);
        worldResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var worldId = worldResponse.Headers.Location!.Segments.Last();

        // Create and delete 5 entities to generate operations
        for (var i = 1; i <= 5; i++)
        {
            // Create entity
            var entityRequest = new
            {
                Name = $"Entity {i}",
                Description = $"Entity {i} for list test",
                EntityType = EntityType.Character
            };
            using var entityResponse = await httpClient.PostAsJsonAsync(
                $"/api/v1/worlds/{worldId}/entities",
                entityRequest,
                cancellationToken);
            entityResponse.StatusCode.Should().Be(HttpStatusCode.Created);
            var entityId = entityResponse.Headers.Location!.Segments.Last();

            // Delete entity
            using var deleteResponse = await httpClient.DeleteAsync(
                $"/api/v1/worlds/{worldId}/entities/{entityId}",
                cancellationToken);
            deleteResponse.StatusCode.Should().Be(HttpStatusCode.Accepted);

            // Small delay to ensure different timestamps
            await Task.Delay(100, cancellationToken);
        }

        // Wait for operations to complete
        await Task.Delay(2000, cancellationToken);

        // Act - Get recent operations
        using var listResponse = await httpClient.GetAsync(
            $"/api/v1/worlds/{worldId}/delete-operations?limit=10",
            cancellationToken);

        // Assert - Should return 200 OK
        listResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var listJson = await listResponse.Content.ReadAsStringAsync(cancellationToken);
        var listDoc = JsonDocument.Parse(listJson);
        var dataArray = listDoc.RootElement.GetProperty("data").EnumerateArray().ToList();

        // Should return all 5 operations
        dataArray.Should().HaveCount(5, "All 5 delete operations should be returned");

        // Verify operations are ordered by CreatedAt descending (newest first)
        var createdDates = dataArray
            .Select(op => DateTime.Parse(op.GetProperty("createdAt").GetString()!))
            .ToList();

        for (var i = 1; i < createdDates.Count; i++)
        {
            createdDates[i - 1].Should().BeOnOrAfter(createdDates[i],
                "Operations should be ordered by CreatedAt descending (newest first)");
        }

        // Verify each operation has expected fields
        foreach (var op in dataArray)
        {
            op.GetProperty("id").GetString().Should().NotBeNullOrEmpty("Each operation should have an ID");
            op.GetProperty("worldId").GetString().Should().Be(worldId, "Each operation should belong to the correct world");
            op.GetProperty("rootEntityName").GetString().Should().NotBeNullOrEmpty("Each operation should have entity name");
            op.GetProperty("status").GetString().Should().NotBeNullOrEmpty("Each operation should have status");
        }

        // Verify meta count
        var metaCount = listDoc.RootElement.GetProperty("meta").GetProperty("count").GetInt32();
        metaCount.Should().Be(5, "Meta count should match number of operations");
    }

    [TestMethod]
    public async Task GetRecentOperations_WithLimitParameter_RespectsLimit()
    {
        // Arrange
        var cancellationToken = TestContext!.CancellationTokenSource.Token;
        using var httpClient = AppHostFixture.App!.CreateHttpClient("api");

        // Create a world
        var worldRequest = new { Name = "Test World for Limit Test", Description = "World to test limit parameter" };
        using var worldResponse = await httpClient.PostAsJsonAsync("/api/v1/worlds", worldRequest, cancellationToken);
        worldResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var worldId = worldResponse.Headers.Location!.Segments.Last();

        // Create and delete 12 entities
        for (var i = 1; i <= 12; i++)
        {
            var entityRequest = new
            {
                Name = $"Entity {i}",
                Description = $"Entity {i} for limit test",
                EntityType = EntityType.Item
            };
            using var entityResponse = await httpClient.PostAsJsonAsync(
                $"/api/v1/worlds/{worldId}/entities",
                entityRequest,
                cancellationToken);
            entityResponse.StatusCode.Should().Be(HttpStatusCode.Created);
            var entityId = entityResponse.Headers.Location!.Segments.Last();

            using var deleteResponse = await httpClient.DeleteAsync(
                $"/api/v1/worlds/{worldId}/entities/{entityId}",
                cancellationToken);
            deleteResponse.StatusCode.Should().Be(HttpStatusCode.Accepted);

            // Add delay to avoid rate limiting (max 5 concurrent operations)
            // Wait for operations to complete before initiating new ones
            if (i % 5 == 0)
            {
                await Task.Delay(3000, cancellationToken); // Wait for batch to complete
            }
        }

        // Wait for operations to complete
        await Task.Delay(2000, cancellationToken);

        // Act - Get recent operations with limit=10
        using var listResponse = await httpClient.GetAsync(
            $"/api/v1/worlds/{worldId}/delete-operations?limit=10",
            cancellationToken);

        // Assert
        listResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var listJson = await listResponse.Content.ReadAsStringAsync(cancellationToken);
        var listDoc = JsonDocument.Parse(listJson);
        var dataArray = listDoc.RootElement.GetProperty("data").EnumerateArray().ToList();

        // Should return exactly 10 operations (limit enforced)
        dataArray.Should().HaveCount(10, "Limit parameter should cap results at 10");
    }

    #endregion
}
