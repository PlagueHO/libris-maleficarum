using LibrisMaleficarum.Api.Models.Responses;
using LibrisMaleficarum.Domain.Exceptions;
using System.Net;
using System.Text.Json;

namespace LibrisMaleficarum.Api.Middleware;

/// <summary>
/// Middleware for global exception handling.
/// Catches unhandled exceptions and returns standardized error responses.
/// </summary>
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ExceptionHandlingMiddleware"/> class.
    /// </summary>
    /// <param name="next">The next middleware in the pipeline.</param>
    /// <param name="logger">The logger instance.</param>
    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    /// <summary>
    /// Invokes the middleware to handle exceptions.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unhandled exception occurred");
            await HandleExceptionAsync(context, ex);
        }
    }

    private static Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var (statusCode, errorCode, message, details) = exception switch
        {
            SchemaVersionException sve => (
                HttpStatusCode.BadRequest,
                sve.ErrorCode,
                sve.Message,
                new Dictionary<string, object?>
                {
                    ["entityType"] = sve.EntityType,
                    ["requestedVersion"] = sve.RequestedVersion,
                    ["currentVersion"] = sve.CurrentVersion,
                    ["minSupportedVersion"] = sve.MinSupportedVersion,
                    ["maxSupportedVersion"] = sve.MaxSupportedVersion
                }),
            WorldNotFoundException => (HttpStatusCode.NotFound, "WORLD_NOT_FOUND", exception.Message, null),
            EntityNotFoundException => (HttpStatusCode.NotFound, "ENTITY_NOT_FOUND", exception.Message, null),
            UnauthorizedWorldAccessException => (HttpStatusCode.Forbidden, "UNAUTHORIZED_ACCESS", exception.Message, null),
            ArgumentException => (HttpStatusCode.BadRequest, "INVALID_ARGUMENT", exception.Message, null),
            KeyNotFoundException => (HttpStatusCode.NotFound, "NOT_FOUND", exception.Message, null),
            UnauthorizedAccessException => (HttpStatusCode.Forbidden, "FORBIDDEN", exception.Message, null),
            InvalidOperationException => (HttpStatusCode.Conflict, "CONFLICT", exception.Message, null),
            _ => (HttpStatusCode.InternalServerError, "INTERNAL_ERROR", "An unexpected error occurred", null)
        };

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)statusCode;

        var errorResponse = new ErrorResponse
        {
            Error = new ErrorDetail
            {
                Code = errorCode,
                Message = message,
                Details = details
            },
            Meta = new Dictionary<string, object>
            {
                ["timestamp"] = DateTime.UtcNow,
                ["traceId"] = context.TraceIdentifier
            }
        };

        var json = JsonSerializer.Serialize(errorResponse, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        return context.Response.WriteAsync(json);
    }
}

/// <summary>
/// Extension methods for <see cref="IApplicationBuilder"/> to register exception handling middleware.
/// </summary>
public static class ExceptionHandlingMiddlewareExtensions
{
    /// <summary>
    /// Adds exception handling middleware to the application pipeline.
    /// </summary>
    /// <param name="builder">The application builder.</param>
    /// <returns>The application builder for method chaining.</returns>
    public static IApplicationBuilder UseExceptionHandling(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<ExceptionHandlingMiddleware>();
    }
}
