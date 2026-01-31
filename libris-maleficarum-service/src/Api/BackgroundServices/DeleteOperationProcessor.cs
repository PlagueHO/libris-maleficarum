namespace LibrisMaleficarum.Api.BackgroundServices;

using LibrisMaleficarum.Domain.Configuration;
using LibrisMaleficarum.Domain.Interfaces.Repositories;
using LibrisMaleficarum.Domain.Interfaces.Services;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

/// <summary>
/// Background service that processes pending delete operations.
/// Polls for pending operations and executes them asynchronously.
/// </summary>
public class DeleteOperationProcessor : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DeleteOperationProcessor> _logger;
    private readonly DeleteOperationOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="DeleteOperationProcessor"/> class.
    /// </summary>
    /// <param name="serviceProvider">The service provider for creating scoped services.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="options">The delete operation options.</param>
    public DeleteOperationProcessor(
        IServiceProvider serviceProvider,
        ILogger<DeleteOperationProcessor> logger,
        IOptions<DeleteOperationOptions> options)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
    }

    /// <summary>
    /// Executes the background processing loop.
    /// </summary>
    /// <param name="stoppingToken">Cancellation token for stopping the service.</param>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("DeleteOperationProcessor starting...");

        // Resume any in-progress operations on startup
        await ResumeInProgressOperationsAsync(stoppingToken);

        // Main processing loop
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessPendingOperationsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing delete operations");
            }

            // Wait before next poll
            await Task.Delay(_options.PollingIntervalMs, stoppingToken);
        }

        _logger.LogInformation("DeleteOperationProcessor stopped.");
    }

    /// <summary>
    /// Resumes any in-progress operations that were interrupted by a restart.
    /// </summary>
    private async Task ResumeInProgressOperationsAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var deleteOperationRepository = scope.ServiceProvider.GetRequiredService<IDeleteOperationRepository>();

        var inProgressOperations = await deleteOperationRepository.GetInProgressOperationsAsync(cancellationToken);
        var operations = inProgressOperations.ToList();

        if (operations.Any())
        {
            _logger.LogInformation("Found {Count} in-progress operations to resume", operations.Count);

            foreach (var operation in operations)
            {
                try
                {
                    await ProcessOperationAsync(operation.WorldId, operation.Id, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error resuming operation {OperationId}", operation.Id);
                }
            }
        }
    }

    /// <summary>
    /// Processes all pending delete operations.
    /// </summary>
    private async Task ProcessPendingOperationsAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var deleteOperationRepository = scope.ServiceProvider.GetRequiredService<IDeleteOperationRepository>();

        var pendingOperations = await deleteOperationRepository.GetPendingOperationsAsync(cancellationToken);
        var operations = pendingOperations.Take(_options.MaxBatchSize).ToList();

        if (!operations.Any())
        {
            return;
        }

        _logger.LogInformation("Processing {Count} pending delete operations", operations.Count);

        // Process each operation
        foreach (var operation in operations)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                break;
            }

            try
            {
                await ProcessOperationAsync(operation.WorldId, operation.Id, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing operation {OperationId}", operation.Id);
            }

            // Rate limiting: delay between operations
            if (_options.RateLimitPerSecond > 0)
            {
                var delayMs = 1000 / _options.RateLimitPerSecond;
                await Task.Delay(delayMs, cancellationToken);
            }
        }
    }

    /// <summary>
    /// Processes a single delete operation.
    /// </summary>
    private async Task ProcessOperationAsync(Guid worldId, Guid operationId, CancellationToken cancellationToken)
    {
        // Create a new scope for this operation to avoid DbContext lifetime issues
        using var scope = _serviceProvider.CreateScope();
        var deleteService = scope.ServiceProvider.GetRequiredService<IDeleteService>();

        try
        {
            _logger.LogInformation("Processing delete operation {OperationId} in world {WorldId}", operationId, worldId);

            await deleteService.ProcessDeleteAsync(worldId, operationId, cancellationToken);

            _logger.LogInformation("Delete operation {OperationId} completed successfully", operationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process delete operation {OperationId}", operationId);
            throw;
        }
    }
}
