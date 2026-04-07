using LibrisMaleficarum.Domain.Configuration;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace LibrisMaleficarum.Api.Middleware;

/// <summary>
/// Middleware that validates the X-Access-Code header when an access code is configured.
/// When no access code is configured, all requests pass through without restriction.
/// </summary>
public class AccessCodeMiddleware
{
    private static readonly string[] AllowlistedPaths =
    [
        "/health",
        "/alive",
        "/api/config/access-status",
        "/api/error",
        "/openapi",
    ];

    private readonly RequestDelegate _next;
    private readonly ILogger<AccessCodeMiddleware> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="AccessCodeMiddleware"/> class.
    /// </summary>
    /// <param name="next">The next middleware in the pipeline.</param>
    /// <param name="logger">The logger instance.</param>
    public AccessCodeMiddleware(RequestDelegate next, ILogger<AccessCodeMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    /// <summary>
    /// Invokes the middleware to validate the access code header.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <param name="options">The access control options monitor for hot-reload support.</param>
    public async Task InvokeAsync(HttpContext context, IOptionsMonitor<AccessControlOptions> options)
    {
        var accessCode = options.CurrentValue.AccessCode;

        // If no access code is configured, pass through
        if (string.IsNullOrEmpty(accessCode))
        {
            await _next(context);
            return;
        }

        // Check if the request path is in the allowlist
        var requestPath = context.Request.Path.Value ?? string.Empty;
        foreach (var path in AllowlistedPaths)
        {
            if (requestPath.StartsWith(path, StringComparison.OrdinalIgnoreCase))
            {
                await _next(context);
                return;
            }
        }

        // Validate the X-Access-Code header
        var providedCode = context.Request.Headers["X-Access-Code"].FirstOrDefault();

        if (string.IsNullOrEmpty(providedCode))
        {
            _logger.LogWarning("Access code required but not provided for {Path}", requestPath);
            await WriteUnauthorizedResponse(context);
            return;
        }

        // Use constant-time comparison to prevent timing attacks
        var expectedBytes = Encoding.UTF8.GetBytes(accessCode);
        var providedBytes = Encoding.UTF8.GetBytes(providedCode);

        if (!CryptographicOperations.FixedTimeEquals(expectedBytes, providedBytes))
        {
            _logger.LogWarning("Invalid access code provided for {Path}", requestPath);
            await WriteUnauthorizedResponse(context);
            return;
        }

        await _next(context);
    }

    private static async Task WriteUnauthorizedResponse(HttpContext context)
    {
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsync(
            JsonSerializer.Serialize(new { error = "Access code required" }));
    }
}
