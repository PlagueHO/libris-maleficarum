using LibrisMaleficarum.Api.Models.Responses;
using LibrisMaleficarum.Domain.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace LibrisMaleficarum.Api.Filters;

/// <summary>
/// Exception filter to convert domain exceptions to appropriate HTTP responses.
/// </summary>
public sealed class DomainExceptionFilter : IExceptionFilter
{
    private readonly ILogger<DomainExceptionFilter> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="DomainExceptionFilter"/> class.
    /// </summary>
    /// <param name="logger">Logger instance.</param>
    public DomainExceptionFilter(ILogger<DomainExceptionFilter> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public void OnException(ExceptionContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        // Handle domain-specific exceptions
        switch (context.Exception)
        {
            case WorldNotFoundException ex:
                context.Result = new NotFoundObjectResult(new ErrorResponse
                {
                    Error = new ErrorDetail
                    {
                        Code = "WORLD_NOT_FOUND",
                        Message = ex.Message
                    }
                });
                context.ExceptionHandled = true;
                _logger.LogWarning(ex, "World not found: {WorldId}", ex.WorldId);
                break;

            case EntityNotFoundException ex:
                context.Result = new NotFoundObjectResult(new ErrorResponse
                {
                    Error = new ErrorDetail
                    {
                        Code = "ENTITY_NOT_FOUND",
                        Message = ex.Message
                    }
                });
                context.ExceptionHandled = true;
                _logger.LogWarning(ex, "Entity not found: WorldId={WorldId}, EntityId={EntityId}", ex.WorldId, ex.EntityId);
                break;

            case AssetNotFoundException ex:
                context.Result = new NotFoundObjectResult(new ErrorResponse
                {
                    Error = new ErrorDetail
                    {
                        Code = "ASSET_NOT_FOUND",
                        Message = ex.Message
                    }
                });
                context.ExceptionHandled = true;
                _logger.LogWarning(ex, "Asset not found: {AssetId}", ex.AssetId);
                break;

            case UnauthorizedWorldAccessException ex:
                context.Result = new ObjectResult(new ErrorResponse
                {
                    Error = new ErrorDetail
                    {
                        Code = "UNAUTHORIZED_WORLD_ACCESS",
                        Message = ex.Message
                    }
                })
                {
                    StatusCode = StatusCodes.Status403Forbidden
                };
                context.ExceptionHandled = true;
                _logger.LogWarning(ex, "Unauthorized world access: WorldId={WorldId}, UserId={UserId}", ex.WorldId, ex.UserId);
                break;

            // Validation exceptions (ArgumentException from domain entities)
            case ArgumentException ex:
                context.Result = new BadRequestObjectResult(new ErrorResponse
                {
                    Error = new ErrorDetail
                    {
                        Code = "VALIDATION_ERROR",
                        Message = ex.Message
                    }
                });
                context.ExceptionHandled = true;
                _logger.LogWarning(ex, "Validation error: {Message}", ex.Message);
                break;

            default:
                // Let unhandled exceptions bubble up to global error handling
                _logger.LogError(context.Exception, "Unhandled exception occurred");
                break;
        }
    }
}
