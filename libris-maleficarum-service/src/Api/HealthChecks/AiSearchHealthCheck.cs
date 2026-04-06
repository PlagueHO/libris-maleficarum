using Azure.Search.Documents.Indexes;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace LibrisMaleficarum.Api.HealthChecks;

public sealed class AiSearchHealthCheck(SearchIndexClient searchIndexClient) : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var serviceName = searchIndexClient.ServiceName;
            return Task.FromResult(HealthCheckResult.Healthy(
                $"SearchIndexClient configured (service: {serviceName})"));
        }
        catch (Exception ex)
        {
            return Task.FromResult(HealthCheckResult.Degraded(
                "SearchIndexClient not available", ex));
        }
    }
}
