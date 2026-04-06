using LibrisMaleficarum.Api.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using OpenAI.Embeddings;

namespace LibrisMaleficarum.Api.Tests.HealthChecks;

[TestClass]
[TestCategory("Unit")]
public sealed class EmbeddingsHealthCheckTests
{
    [TestMethod]
    public async Task CheckHealthAsync_WhenClientRegistered_ReturnsHealthy()
    {
        var client = Substitute.For<EmbeddingClient>();

        var check = new EmbeddingsHealthCheck(client);
        var result = await check.CheckHealthAsync(new HealthCheckContext(), CancellationToken.None);

        result.Status.Should().Be(HealthStatus.Healthy);
        result.Description.Should().Contain("registered");
    }

    [TestMethod]
    public async Task CheckHealthAsync_WhenClientNull_ReturnsDegraded()
    {
        var check = new EmbeddingsHealthCheck(null);
        var result = await check.CheckHealthAsync(new HealthCheckContext(), CancellationToken.None);

        result.Status.Should().Be(HealthStatus.Degraded);
        result.Description.Should().Contain("not registered");
    }
}
