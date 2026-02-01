namespace LibrisMaleficarum.Api.Controllers;

using LibrisMaleficarum.Api.Models.Responses;
using LibrisMaleficarum.Domain.Entities;
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

    /// <summary>
    /// Initializes a new instance of the <see cref="DeleteOperationsController"/> class.
    /// </summary>
    /// <param name="deleteService">The delete service.</param>
    public DeleteOperationsController(IDeleteService deleteService)
    {
        _deleteService = deleteService ?? throw new ArgumentNullException(nameof(deleteService));
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
    [ProducesResponseType<ErrorResponse>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetDeleteOperation(
        Guid worldId,
        Guid operationId,
        CancellationToken cancellationToken)
    {
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
    public async Task<IActionResult> ListDeleteOperations(
        Guid worldId,
        [FromQuery] int limit = 20,
        CancellationToken cancellationToken = default)
    {
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
    /// Maps a DeleteOperation to DeleteOperationResponse DTO.
    /// </summary>
    private static DeleteOperationResponse MapToResponse(DeleteOperation operation)
    {
        return new DeleteOperationResponse
        {
            Id = operation.Id,
            WorldId = operation.WorldId,
            RootEntityId = operation.RootEntityId,
            RootEntityName = operation.RootEntityName,
            Status = operation.Status.ToString().ToLowerInvariant(),
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
