namespace LibrisMaleficarum.Api.Controllers;

using LibrisMaleficarum.Api.Models.Responses;
using LibrisMaleficarum.Domain.Entities;
using LibrisMaleficarum.Domain.Extensions;
using LibrisMaleficarum.Domain.Interfaces.Repositories;
using LibrisMaleficarum.Domain.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;

/// <summary>
/// Controller for managing delete operation status and tracking.
/// </summary>
[ApiController]
[Route("api/v1/worlds/{worldId:guid}/delete-operations")]
public class DeleteOperationsController : ControllerBase
{
    private readonly IDeleteService _deleteService;
    private readonly IWorldRepository _worldRepository;

    /// <summary>
    /// Initializes a new instance of the <see cref="DeleteOperationsController"/> class.
    /// </summary>
    /// <param name="deleteService">The delete service.</param>
    /// <param name="worldRepository">The world repository for authorization checks.</param>
    public DeleteOperationsController(IDeleteService deleteService, IWorldRepository worldRepository)
    {
        _deleteService = deleteService ?? throw new ArgumentNullException(nameof(deleteService));
        _worldRepository = worldRepository ?? throw new ArgumentNullException(nameof(worldRepository));
    }

    /// <summary>
    /// Gets the status of a delete operation.
    /// </summary>
    /// <param name="worldId">The world identifier.</param>
    /// <param name="operationId">The operation identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The delete operation status.</returns>
    [HttpGet("{operationId:guid}")]
    [ProducesResponseType<ApiResponse<DeleteOperationResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ErrorResponse>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ErrorResponse>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetDeleteOperation(
        Guid worldId,
        Guid operationId,
        CancellationToken cancellationToken)
    {
        // Validate world ownership - will throw UnauthorizedWorldAccessException if user doesn't own world
        await _worldRepository.GetByIdAsync(worldId, cancellationToken);

        var operation = await _deleteService.GetOperationStatusAsync(worldId, operationId, cancellationToken);

        if (operation == null)
        {
            return NotFound(new ErrorResponse
            {
                Error = new ErrorDetail
                {
                    Code = "OPERATION_NOT_FOUND",
                    Message = $"Delete operation '{operationId}' not found or has expired."
                }
            });
        }

        return Ok(new ApiResponse<DeleteOperationResponse>
        {
            Data = MapToResponse(operation)
        });
    }

    /// <summary>
    /// Lists recent delete operations for a world.
    /// </summary>
    /// <param name="worldId">The world identifier.</param>
    /// <param name="limit">Maximum number of operations to return (default: 20, max: 100).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of recent delete operations.</returns>
    [HttpGet]
    [ProducesResponseType<ApiResponse<IEnumerable<DeleteOperationResponse>>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ErrorResponse>(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ListDeleteOperations(
        Guid worldId,
        [FromQuery] int limit = 20,
        CancellationToken cancellationToken = default)
    {
        // Validate world ownership - will throw UnauthorizedWorldAccessException if user doesn't own world
        await _worldRepository.GetByIdAsync(worldId, cancellationToken);

        var operations = await _deleteService.ListRecentOperationsAsync(worldId, limit, cancellationToken);

        return Ok(new ApiResponse<IEnumerable<DeleteOperationResponse>>
        {
            Data = operations.Select(MapToResponse),
            Meta = new Dictionary<string, object>
            {
                { "count", operations.Count() }
            }
        });
    }

    /// <summary>
    /// Retries a failed or partial delete operation.
    /// </summary>
    /// <param name="worldId">The world identifier.</param>
    /// <param name="operationId">The operation identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The new delete operation created for the retry.</returns>
    [HttpPost("{operationId:guid}/retry")]
    [ProducesResponseType<ApiResponse<DeleteOperationResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ErrorResponse>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ErrorResponse>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ErrorResponse>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RetryDeleteOperation(
        Guid worldId,
        Guid operationId,
        CancellationToken cancellationToken)
    {
        try
        {
            var retriedOperation = await _deleteService.RetryDeleteOperationAsync(worldId, operationId, cancellationToken);

            return Ok(new ApiResponse<DeleteOperationResponse>
            {
                Data = MapToResponse(retriedOperation)
            });
        }
        catch (Domain.Exceptions.EntityNotFoundException)
        {
            return NotFound(new ErrorResponse
            {
                Error = new ErrorDetail
                {
                    Code = "OPERATION_NOT_FOUND",
                    Message = $"Delete operation '{operationId}' not found or has expired."
                }
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new ErrorResponse
            {
                Error = new ErrorDetail
                {
                    Code = "INVALID_OPERATION_STATUS",
                    Message = ex.Message
                }
            });
        }
    }

    /// <summary>
    /// Maps a DeleteOperation to DeleteOperationResponse DTO.
    /// </summary>
    internal static DeleteOperationResponse MapToResponse(DeleteOperation operation)
    {
        return new DeleteOperationResponse
        {
            Id = operation.Id,
            WorldId = operation.WorldId,
            RootEntityId = operation.RootEntityId,
            RootEntityName = operation.RootEntityName,
            Status = operation.Status.ToApiString(),
            TotalEntities = operation.TotalEntities,
            DeletedCount = operation.DeletedCount,
            FailedCount = operation.FailedCount,
            FailedEntityIds = operation.FailedEntityIds,
            ErrorDetails = operation.ErrorDetails,
            Cascade = operation.Cascade,
            CreatedBy = operation.CreatedBy,
            CreatedAt = operation.CreatedAt,
            StartedAt = operation.StartedAt,
            CompletedAt = operation.CompletedAt
        };
    }
}
