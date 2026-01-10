using System.Diagnostics;
using System.Net.Http.Json;
using Aspire.Hosting;
using Aspire.Hosting.Testing;
using LibrisMaleficarum.Api.Models.Requests;
using LibrisMaleficarum.Api.Models.Responses;
using LibrisMaleficarum.IntegrationTests.Shared;

namespace LibrisMaleficarum.Performance.Tests;

/// <summary>
/// Performance tests for World Management API.
/// These tests verify performance requirements from SC-002:
/// - p95 response time &lt;200ms for CRUD operations
/// 
/// Run with: dotnet test --filter TestCategory=Performance
/// </summary>
[TestClass]
[TestCategory("Performance")]
public class WorldPerformanceTests
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
    /// T156: Create 100 worlds and measure p95 response time (&lt;200ms per SC-002).
    /// </summary>
    [TestMethod]
    [TestCategory("Performance")]
    public async Task CreateWorlds_100Requests_P95ResponseTimeLessThan200ms()
    {
        // Arrange
        const int requestCount = 100;
        var responseTimes = new List<long>(requestCount);
        var tasks = new List<Task>(requestCount);

        // Act - Create 100 worlds and measure response times
        for (int i = 0; i < requestCount; i++)
        {
            var index = i; // Capture for closure
            tasks.Add(Task.Run(async () =>
            {
                var request = new CreateWorldRequest
                {
                    Name = $"Performance Test World {index}",
                    Description = $"World created for performance testing iteration {index}"
                };

                var stopwatch = Stopwatch.StartNew();
                var response = await _client!.PostAsJsonAsync("/api/v1/worlds", request);
                stopwatch.Stop();

                response.IsSuccessStatusCode.Should().BeTrue(
                    $"World creation request {index} should succeed");

                lock (responseTimes)
                {
                    responseTimes.Add(stopwatch.ElapsedMilliseconds);
                }
            }));
        }

        await Task.WhenAll(tasks);

        // Assert - Calculate p95 response time
        responseTimes.Sort();
        var p95Index = (int)Math.Ceiling(requestCount * 0.95) - 1;
        var p95ResponseTime = responseTimes[p95Index];
        var averageResponseTime = responseTimes.Average();
        var maxResponseTime = responseTimes.Max();
        var minResponseTime = responseTimes.Min();

        // Log performance metrics
        Console.WriteLine($"Performance Metrics for {requestCount} World Creations:");
        Console.WriteLine($"  Min Response Time: {minResponseTime}ms");
        Console.WriteLine($"  Average Response Time: {averageResponseTime:F2}ms");
        Console.WriteLine($"  P95 Response Time: {p95ResponseTime}ms");
        Console.WriteLine($"  Max Response Time: {maxResponseTime}ms");

        // Verify p95 requirement
        p95ResponseTime.Should().BeLessThan(200,
            $"P95 response time should be less than 200ms per SC-002, but was {p95ResponseTime}ms");
    }

    /// <summary>
    /// T156: Retrieve 100 worlds and measure p95 response time (&lt;200ms per SC-002).
    /// </summary>
    [TestMethod]
    [TestCategory("Performance")]
    public async Task GetWorlds_100Requests_P95ResponseTimeLessThan200ms()
    {
        // Arrange - Create a test world first
        var createRequest = new CreateWorldRequest
        {
            Name = "Performance Test World for Retrieval",
            Description = "World created to test retrieval performance"
        };

        var createResponse = await _client!.PostAsJsonAsync("/api/v1/worlds", createRequest);
        createResponse.IsSuccessStatusCode.Should().BeTrue();
        var createdWorld = await createResponse.Content.ReadFromJsonAsync<ApiResponse<WorldResponse>>();
        var worldId = createdWorld!.Data!.Id;

        const int requestCount = 100;
        var responseTimes = new List<long>(requestCount);
        var tasks = new List<Task>(requestCount);

        // Act - Retrieve the same world 100 times and measure response times
        for (int i = 0; i < requestCount; i++)
        {
            tasks.Add(Task.Run(async () =>
            {
                var stopwatch = Stopwatch.StartNew();
                var response = await _client!.GetAsync($"/api/v1/worlds/{worldId}");
                stopwatch.Stop();

                response.IsSuccessStatusCode.Should().BeTrue(
                    "World retrieval request should succeed");

                lock (responseTimes)
                {
                    responseTimes.Add(stopwatch.ElapsedMilliseconds);
                }
            }));
        }

        await Task.WhenAll(tasks);

        // Assert - Calculate p95 response time
        responseTimes.Sort();
        var p95Index = (int)Math.Ceiling(requestCount * 0.95) - 1;
        var p95ResponseTime = responseTimes[p95Index];
        var averageResponseTime = responseTimes.Average();
        var maxResponseTime = responseTimes.Max();
        var minResponseTime = responseTimes.Min();

        // Log performance metrics
        Console.WriteLine($"Performance Metrics for {requestCount} World Retrievals:");
        Console.WriteLine($"  Min Response Time: {minResponseTime}ms");
        Console.WriteLine($"  Average Response Time: {averageResponseTime:F2}ms");
        Console.WriteLine($"  P95 Response Time: {p95ResponseTime}ms");
        Console.WriteLine($"  Max Response Time: {maxResponseTime}ms");

        // Verify p95 requirement
        p95ResponseTime.Should().BeLessThan(200,
            $"P95 response time should be less than 200ms per SC-002, but was {p95ResponseTime}ms");
    }
}
