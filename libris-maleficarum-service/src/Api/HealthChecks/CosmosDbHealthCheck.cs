using Azure.Identity;
using LibrisMaleficarum.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace LibrisMaleficarum.Api.HealthChecks;

public sealed class CosmosDbHealthCheck(ApplicationDbContext dbContext) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var canConnect = await dbContext.Database.CanConnectAsync(cancellationToken);

            return canConnect
                ? HealthCheckResult.Healthy("Cosmos DB is reachable")
                : HealthCheckResult.Unhealthy("Cosmos DB connection check returned false");
        }
        catch (Exception ex)
        {
            var rootCause = ex;
            if (ex.InnerException is AuthenticationFailedException or CredentialUnavailableException)
            {
                rootCause = ex.InnerException;
            }

            var message = $"{rootCause.GetType().Name}: {rootCause.Message}";
            return HealthCheckResult.Unhealthy(
                message.Length > 500 ? message[..500] : message,
                ex);
        }
    }
}
