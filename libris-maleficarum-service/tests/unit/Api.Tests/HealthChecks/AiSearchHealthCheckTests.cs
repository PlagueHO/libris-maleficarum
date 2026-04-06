using Azure.Search.Documents.Indexes;
using LibrisMaleficarum.Api.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace LibrisMaleficarum.Api.Tests.HealthChecks;

[TestClass]
[TestCategory("Unit")]
public sealed class AiSearchHealthCheckTests
{
    [TestMethod]
    public async Task CheckHealthAsync_WhenClientConfigured_ReturnsHealthy()
    {
        var client = Substitute.For<SearchIndexClient>();
        client.ServiceName.Returns("test-search-service");

        var check = new AiSearchHealthCheck(client);
        var result = await check.CheckHealthAsync(new HealthCheckContext(), CancellationToken.None);

        result.Status.Should().Be(HealthStatus.Healthy);
        result.Description.Should().Contain("test-search-service");
    }

    [TestMethod]
    public async Task CheckHealthAsync_WhenClientThrows_ReturnsDegraded()
    {
        var client = Substitute.For<SearchIndexClient>();
        client.When(c => { _ = c.ServiceName; })
            .Do(_ => throw new InvalidOperationException("Not configured"));

        var check = new AiSearchHealthCheck(client);
        var result = await check.CheckHealthAsync(new HealthCheckContext(), CancellationToken.None);

        result.Status.Should().Be(HealthStatus.Degraded);
        result.Description.Should().Contain("not available");
    }
}
