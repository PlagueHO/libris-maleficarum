namespace LibrisMaleficarum.Infrastructure.Processors;

using LibrisMaleficarum.Domain.Entities;
using LibrisMaleficarum.Domain.Interfaces.Repositories;
using LibrisMaleficarum.Domain.Interfaces.Services;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

/// <summary>
/// Background service that processes pending delete operations asynchronously.
/// Provides checkpoint resume capability for operations interrupted by app restarts.
/// </summary>
/// <remarks>
/// This processor runs as a background service and:
/// - On startup: Resumes any in-progress operations (checkpoint resume for crash recovery)
/// - Continuously polls for new pending operations
/// - Processes each operation via DeleteService
/// - Rate limiting is enforced at the operation initiation level (5 per user per world)
/// </remarks>
public class DeleteOperationProcessor : BackgroundService
{
    private readonly IDeleteOperationRepository _deleteOperationRepository;
    private readonly IDeleteService _deleteService;
    private readonly ITelemetryService _telemetryService;
    private readonly ILogger<DeleteOperationProcessor> _logger;
    private readonly TimeSpan _pollingInterval = TimeSpan.FromSeconds(2);

    /// <summary>
    /// Initializes a new instance of the <see cref="DeleteOperationProcessor"/> class.
    /// </summary>
    /// <param name="deleteOperationRepository">The delete operation repository.</param>
    /// <param name="deleteService">The delete service for processing operations.</param>
    /// <param name="telemetryService">The telemetry service.</param>
    /// <param name="logger">The logger.</param>
    public DeleteOperationProcessor(
        IDeleteOperationRepository deleteOperationRepository,
        IDeleteService deleteService,
        ITelemetryService telemetryService,
        ILogger<DeleteOperationProcessor> logger)
    {
        _deleteOperationRepository = deleteOperationRepository ?? throw new ArgumentNullException(nameof(deleteOperationRepository));
        _deleteService = deleteService ?? throw new ArgumentNullException(nameof(deleteService));
        _telemetryService = telemetryService ?? throw new ArgumentNullException(nameof(telemetryService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Executes the background processor.
    /// On startup, resumes any in-progress operations (checkpoint resume).
    /// Then continuously polls for new pending operations.
    /// </summary>
    /// <param name="stoppingToken">Cancellation token for graceful shutdown.</param>
    /// <returns>A task representing the background execution.</returns>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("DeleteOperationProcessor starting");

        try
        {
            // T042: Checkpoint resume - Process any in-progress operations from previous app instance
            await ResumeInProgressOperationsAsync(stoppingToken);

            // Continuous polling loop for new pending operations
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ProcessPendingOperationsAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing pending delete operations");
                }

                // Wait before next poll
                await Task.Delay(_pollingInterval, stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("DeleteOperationProcessor stopping");
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "DeleteOperationProcessor encountered fatal error");
            throw;
        }
    }

    /// <summary>
    /// Resumes in-progress operations that were interrupted by app restart (checkpoint resume).
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    private async Task ResumeInProgressOperationsAsync(CancellationToken cancellationToken)
    {
        using var activity = _telemetryService.StartActivity("ResumeInProgressOperations");

        try
        {
            var inProgressOperations = (await _deleteOperationRepository.GetInProgressOperationsAsync(cancellationToken)).ToList();

            if (inProgressOperations.Count > 0)
            {
                _logger.LogInformation(
                    "Found {Count} in-progress operations to resume after restart",
                    inProgressOperations.Count);

                activity?.AddTag("inprogress_operations_count", inProgressOperations.Count);
            }

            foreach (var operation in inProgressOperations)
            {
                try
                {
                    _logger.LogInformation(
                        "Resuming in-progress operation {OperationId} for entity {EntityId}",
                        operation.Id,
                        operation.RootEntityId);

                    await _deleteService.ProcessDeleteAsync(operation.WorldId, operation.Id, cancellationToken);

                    _logger.LogInformation(
                        "Successfully resumed operation {OperationId}",
                        operation.Id);
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "Failed to resume in-progress operation {OperationId}",
                        operation.Id);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during checkpoint resume of in-progress operations");
            throw;
        }
    }

    /// <summary>
    /// Processes pending delete operations from the queue.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    private async Task ProcessPendingOperationsAsync(CancellationToken cancellationToken)
    {
        var pendingOperations = (await _deleteOperationRepository.GetPendingOperationsAsync(cancellationToken)).ToList();

        if (pendingOperations.Count == 0)
        {
            return;
        }

        _logger.LogDebug("Found {Count} pending delete operations to process", pendingOperations.Count);

        foreach (var operation in pendingOperations)
        {
            try
            {
                _logger.LogInformation(
                    "Processing delete operation {OperationId} for entity {EntityId} in world {WorldId}",
                    operation.Id,
                    operation.RootEntityId,
                    operation.WorldId);

                await _deleteService.ProcessDeleteAsync(operation.WorldId, operation.Id, cancellationToken);

                _logger.LogInformation(
                    "Completed delete operation {OperationId} with status {Status}",
                    operation.Id,
                    operation.Status);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error processing delete operation {OperationId}",
                    operation.Id);

                // Operation will remain in Pending/InProgress state and can be retried
                // Error is logged for monitoring/alerting
            }
        }
    }
}
