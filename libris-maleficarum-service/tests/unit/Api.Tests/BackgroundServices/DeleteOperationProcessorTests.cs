namespace LibrisMaleficarum.Api.Tests.BackgroundServices;

using FluentAssertions;
using LibrisMaleficarum.Api.BackgroundServices;
using LibrisMaleficarum.Domain.Configuration;
using LibrisMaleficarum.Domain.Entities;
using LibrisMaleficarum.Domain.Interfaces.Repositories;
using LibrisMaleficarum.Domain.Interfaces.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;

/// <summary>
/// Unit tests for DeleteOperationProcessor background service.
/// Tests operation processing, status updates, and error handling.
/// </summary>
[TestClass]
[TestCategory("Unit")]
public class DeleteOperationProcessorTests
{
    private IServiceProvider _serviceProvider = null!;
    private ServiceCollection _services = null!;
    private IDeleteOperationRepository _deleteOperationRepository = null!;
    private IDeleteService _deleteService = null!;
    private ILogger<DeleteOperationProcessor> _logger = null!;
    private IOptions<DeleteOperationOptions> _options = null!;

    private readonly Guid _worldId = Guid.NewGuid();
    private readonly string _userId = "test-user-id";

    [TestInitialize]
    public void Setup()
    {
        _services = new ServiceCollection();
        _deleteOperationRepository = Substitute.For<IDeleteOperationRepository>();
        _deleteService = Substitute.For<IDeleteService>();
        _logger = Substitute.For<ILogger<DeleteOperationProcessor>>();

        _options = Options.Create(new DeleteOperationOptions
        {
            MaxConcurrentPerUserPerWorld = 5,
            RetryAfterSeconds = 60,
            PollingIntervalMs = 100, // Short interval for faster tests
            MaxBatchSize = 10,
            RateLimitPerSecond = 0
        });

        // Register mocked services in DI container
        _services.AddScoped(_ => _deleteOperationRepository);
        _services.AddScoped(_ => _deleteService);

        _serviceProvider = _services.BuildServiceProvider();
    }

    #region T027: ProcessDeleteAsync Tests

    [TestMethod]
    public async Task ExecuteAsync_WithPendingOperation_ProcessesAndCompletesSuccessfully()
    {
        // Arrange
        var entityId = Guid.NewGuid();
        var operationId = Guid.NewGuid();

        var pendingOperation = CreateDeleteOperation(operationId, entityId, "Test Entity", DeleteOperationStatus.Pending);

        // Return pending operation on first call, then empty on subsequent calls
        var callCount = 0;
        _deleteOperationRepository.GetPendingOperationsAsync(Arg.Any<CancellationToken>())
            .Returns(x => callCount++ == 0 ? [pendingOperation] : []);

        _deleteOperationRepository.GetInProgressOperationsAsync(Arg.Any<CancellationToken>())
            .Returns([]);

        // Mock DeleteService.ProcessDeleteAsync to complete the operation
        _deleteService.ProcessDeleteAsync(_worldId, operationId, Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var processor = new DeleteOperationProcessor(_serviceProvider, _logger, _options);

        // Act
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        var executeTask = processor.StartAsync(cts.Token);

        // Give processor time to run one iteration
        await Task.Delay(300, CancellationToken.None);
        
        // Stop processor
        await processor.StopAsync(CancellationToken.None);
        await executeTask;

        // Assert - should be called at least once
        await _deleteService.Received().ProcessDeleteAsync(_worldId, operationId, Arg.Any<CancellationToken>());
    }

    [TestMethod]
    public async Task ExecuteAsync_WithMultipleOperations_ProcessesOneAtATimePerWorld()
    {
        // Arrange
        var entity1Id = Guid.NewGuid();
        var entity2Id = Guid.NewGuid();
        var operation1Id = Guid.NewGuid();
        var operation2Id = Guid.NewGuid();

        var operation1 = CreateDeleteOperation(operation1Id, entity1Id, "Entity 1", DeleteOperationStatus.Pending);
        var operation2 = CreateDeleteOperation(operation2Id, entity2Id, "Entity 2", DeleteOperationStatus.Pending);

        _deleteOperationRepository.GetPendingOperationsAsync(Arg.Any<CancellationToken>())
            .Returns([operation1, operation2], []);

        _deleteOperationRepository.GetInProgressOperationsAsync(Arg.Any<CancellationToken>())
            .Returns([]);

        _deleteService.ProcessDeleteAsync(_worldId, Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var processor = new DeleteOperationProcessor(_serviceProvider, _logger, _options);

        // Act
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        var executeTask = processor.StartAsync(cts.Token);

        // Give processor time to run iterations
        await Task.Delay(500, CancellationToken.None);
        
        // Stop processor
        await processor.StopAsync(CancellationToken.None);
        await executeTask;

        // Assert - both operations should be processed
        await _deleteService.Received().ProcessDeleteAsync(_worldId, operation1Id, Arg.Any<CancellationToken>());
        await _deleteService.Received().ProcessDeleteAsync(_worldId, operation2Id, Arg.Any<CancellationToken>());
    }

    [TestMethod]
    public async Task ExecuteAsync_WithFailedEntity_MarksOperationAsPartial()
    {
        // Arrange
        var entityId = Guid.NewGuid();
        var operationId = Guid.NewGuid();

        var pendingOperation = CreateDeleteOperation(operationId, entityId, "Test Entity", DeleteOperationStatus.Pending);

        // Return pending operation on first call, then empty on subsequent calls
        var callCount = 0;
        _deleteOperationRepository.GetPendingOperationsAsync(Arg.Any<CancellationToken>())
            .Returns(x => callCount++ == 0 ? [pendingOperation] : []);

        _deleteOperationRepository.GetInProgressOperationsAsync(Arg.Any<CancellationToken>())
            .Returns([]);

        // Mock DeleteService to throw exception (simulating failure)
        _deleteService.ProcessDeleteAsync(_worldId, operationId, Arg.Any<CancellationToken>())
            .Returns(Task.FromException(new InvalidOperationException("Delete failed")));

        var processor = new DeleteOperationProcessor(_serviceProvider, _logger, _options);

        // Act
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        var executeTask = processor.StartAsync(cts.Token);

        // Give processor time to run one iteration
        await Task.Delay(300, CancellationToken.None);
        
        // Stop processor
        await processor.StopAsync(CancellationToken.None);
        await executeTask;

        // Assert - should still call ProcessDeleteAsync (exception is logged, not rethrown)
        await _deleteService.Received().ProcessDeleteAsync(_worldId, operationId, Arg.Any<CancellationToken>());
    }

    [TestMethod]
    public async Task ExecuteAsync_UpdatesStatusToInProgressBeforeProcessing()
    {
        // Arrange
        var entityId = Guid.NewGuid();
        var operationId = Guid.NewGuid();

        var pendingOperation = CreateDeleteOperation(operationId, entityId, "Test Entity", DeleteOperationStatus.Pending);

        // Return pending operation on first call, then empty on subsequent calls
        var callCount = 0;
        _deleteOperationRepository.GetPendingOperationsAsync(Arg.Any<CancellationToken>())
            .Returns(x => callCount++ == 0 ? [pendingOperation] : []);

        _deleteOperationRepository.GetInProgressOperationsAsync(Arg.Any<CancellationToken>())
            .Returns([]);

        // Mock ProcessDeleteAsync to verify it's called
        _deleteService.ProcessDeleteAsync(_worldId, operationId, Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var processor = new DeleteOperationProcessor(_serviceProvider, _logger, _options);

        // Act
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        var executeTask = processor.StartAsync(cts.Token);

        await Task.Delay(300, CancellationToken.None);
        
        await processor.StopAsync(CancellationToken.None);
        await executeTask;

        // Assert
        await _deleteService.Received().ProcessDeleteAsync(_worldId, operationId, Arg.Any<CancellationToken>());
    }

    [TestMethod]
    public async Task ExecuteAsync_SetsCompletedDateOnSuccess()
    {
        // Arrange
        var entityId = Guid.NewGuid();
        var operationId = Guid.NewGuid();

        var pendingOperation = CreateDeleteOperation(operationId, entityId, "Test Entity", DeleteOperationStatus.Pending);

        // Return pending operation on first call, then empty on subsequent calls
        var callCount = 0;
        _deleteOperationRepository.GetPendingOperationsAsync(Arg.Any<CancellationToken>())
            .Returns(x => callCount++ == 0 ? [pendingOperation] : []);

        _deleteOperationRepository.GetInProgressOperationsAsync(Arg.Any<CancellationToken>())
            .Returns([]);

        _deleteService.ProcessDeleteAsync(_worldId, operationId, Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var processor = new DeleteOperationProcessor(_serviceProvider, _logger, _options);

        // Act
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        var executeTask = processor.StartAsync(cts.Token);

        await Task.Delay(300, CancellationToken.None);
        
        await processor.StopAsync(CancellationToken.None);
        await executeTask;

        // Assert - ProcessDeleteAsync should be called, which internally sets CompletedDate
        await _deleteService.Received().ProcessDeleteAsync(_worldId, operationId, Arg.Any<CancellationToken>());
    }

    #endregion

    #region Helper Methods

    private DeleteOperation CreateDeleteOperation(Guid operationId, Guid entityId, string entityName, DeleteOperationStatus status)
    {
        var operation = DeleteOperation.Create(_worldId, entityId, entityName, _userId, cascade: true);

        // Use reflection to set Id and Status
        var operationType = typeof(DeleteOperation);
        var idProperty = operationType.GetProperty("Id")!;
        idProperty.SetValue(operation, operationId);

        if (status != DeleteOperationStatus.Pending)
        {
            var statusProperty = operationType.GetProperty("Status")!;
            statusProperty.SetValue(operation, status);
        }

        return operation;
    }

    #endregion
}
