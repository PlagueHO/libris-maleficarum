using LibrisMaleficarum.Api.HealthChecks;
using LibrisMaleficarum.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace LibrisMaleficarum.Api.Tests.HealthChecks;

[TestClass]
[TestCategory("Unit")]
public sealed class CosmosDbHealthCheckTests
{
    private static ApplicationDbContext CreateMockDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return Substitute.ForPartsOf<ApplicationDbContext>(options);
    }

    [TestMethod]
    public async Task CheckHealthAsync_WhenCanConnect_ReturnsHealthy()
    {
        var dbContext = CreateMockDbContext();
        var databaseFacade = Substitute.ForPartsOf<DatabaseFacade>(dbContext);
        dbContext.Database.Returns(databaseFacade);
        databaseFacade.CanConnectAsync(Arg.Any<CancellationToken>()).Returns(true);

        var check = new CosmosDbHealthCheck(dbContext);
        var result = await check.CheckHealthAsync(new HealthCheckContext(), CancellationToken.None);

        result.Status.Should().Be(HealthStatus.Healthy);
        result.Description.Should().Contain("Cosmos DB is reachable");
    }

    [TestMethod]
    public async Task CheckHealthAsync_WhenCannotConnect_ReturnsUnhealthy()
    {
        var dbContext = CreateMockDbContext();
        var databaseFacade = Substitute.ForPartsOf<DatabaseFacade>(dbContext);
        dbContext.Database.Returns(databaseFacade);
        databaseFacade.CanConnectAsync(Arg.Any<CancellationToken>()).Returns(false);

        var check = new CosmosDbHealthCheck(dbContext);
        var result = await check.CheckHealthAsync(new HealthCheckContext(), CancellationToken.None);

        result.Status.Should().Be(HealthStatus.Unhealthy);
        result.Description.Should().Contain("connection check returned false");
    }

    [TestMethod]
    public async Task CheckHealthAsync_WhenExceptionThrown_ReturnsUnhealthy()
    {
        var dbContext = CreateMockDbContext();
        var databaseFacade = Substitute.ForPartsOf<DatabaseFacade>(dbContext);
        dbContext.Database.Returns(databaseFacade);
        databaseFacade.CanConnectAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromException<bool>(new InvalidOperationException("Connection failed")));

        var check = new CosmosDbHealthCheck(dbContext);
        var result = await check.CheckHealthAsync(new HealthCheckContext(), CancellationToken.None);

        result.Status.Should().Be(HealthStatus.Unhealthy);
        result.Description.Should().Contain("InvalidOperationException");
    }

    [TestMethod]
    public async Task CheckHealthAsync_WithCredentialException_UnwrapsInnerException()
    {
        var innerEx = new Azure.Identity.CredentialUnavailableException("No credential found");
        var outerEx = new InvalidOperationException("Auth wrapper", innerEx);

        var dbContext = CreateMockDbContext();
        var databaseFacade = Substitute.ForPartsOf<DatabaseFacade>(dbContext);
        dbContext.Database.Returns(databaseFacade);
        databaseFacade.CanConnectAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromException<bool>(outerEx));

        var check = new CosmosDbHealthCheck(dbContext);
        var result = await check.CheckHealthAsync(new HealthCheckContext(), CancellationToken.None);

        result.Status.Should().Be(HealthStatus.Unhealthy);
        result.Description.Should().Contain("CredentialUnavailableException");
    }
}
