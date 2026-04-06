using Microsoft.Extensions.Diagnostics.HealthChecks;
using OpenAI.Embeddings;

namespace LibrisMaleficarum.Api.HealthChecks;

public sealed class EmbeddingsHealthCheck(EmbeddingClient? embeddingClient = null) : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        if (embeddingClient is null)
        {
            return Task.FromResult(HealthCheckResult.Degraded(
                "EmbeddingClient not registered — embedding features unavailable"));
        }

        return Task.FromResult(HealthCheckResult.Healthy("EmbeddingClient registered"));
    }
}
