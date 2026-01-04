namespace LibrisMaleficarum.Api.Tests.Middleware;

using FluentAssertions;
using LibrisMaleficarum.Api.Middleware;
using LibrisMaleficarum.Api.Models.Responses;
using LibrisMaleficarum.Domain.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using NSubstitute;
using System.Net;
using System.Text.Json;

/// <summary>
/// Unit tests for ExceptionHandlingMiddleware.
/// Tests exception-to-HTTP-status mapping and error response formatting.
/// </summary>
[TestClass]
public class ExceptionHandlingMiddlewareTests
{
    private ILogger<ExceptionHandlingMiddleware> _logger = null!;
    private DefaultHttpContext _context = null!;

    [TestInitialize]
    public void Setup()
    {
        _logger = Substitute.For<ILogger<ExceptionHandlingMiddleware>>();
        _context = new DefaultHttpContext();
        _context.Response.Body = new MemoryStream();
    }

    #region WorldNotFoundException Tests

    [TestMethod]
    public async Task InvokeAsync_WithWorldNotFoundException_Returns404()
    {
        // Arrange
        var worldId = Guid.NewGuid();
        var exception = new WorldNotFoundException(worldId);

        RequestDelegate next = (HttpContext hc) => throw exception;
        var middleware = new ExceptionHandlingMiddleware(next, _logger);

        // Act
        await middleware.InvokeAsync(_context);

        // Assert
        _context.Response.StatusCode.Should().Be((int)HttpStatusCode.NotFound);
        _context.Response.ContentType.Should().Be("application/json");

        var response = await GetResponseBody<ErrorResponse>();
        response.Error.Code.Should().Be("WORLD_NOT_FOUND");
        response.Error.Message.Should().Contain(worldId.ToString());
        response.Meta.Should().ContainKey("timestamp");
        response.Meta.Should().ContainKey("traceId");
    }

    #endregion

    #region EntityNotFoundException Tests

    [TestMethod]
    public async Task InvokeAsync_WithEntityNotFoundException_Returns404()
    {
        // Arrange
        var worldId = Guid.NewGuid();
        var entityId = Guid.NewGuid();
        var exception = new EntityNotFoundException(worldId, entityId);

        RequestDelegate next = (HttpContext hc) => throw exception;
        var middleware = new ExceptionHandlingMiddleware(next, _logger);

        // Act
        await middleware.InvokeAsync(_context);

        // Assert
        _context.Response.StatusCode.Should().Be((int)HttpStatusCode.NotFound);

        var response = await GetResponseBody<ErrorResponse>();
        response.Error.Code.Should().Be("ENTITY_NOT_FOUND");
        response.Error.Message.Should().Contain(entityId.ToString());
    }

    #endregion

    #region UnauthorizedWorldAccessException Tests

    [TestMethod]
    public async Task InvokeAsync_WithUnauthorizedWorldAccessException_Returns403()
    {
        // Arrange
        var worldId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var exception = new UnauthorizedWorldAccessException(worldId, userId);

        RequestDelegate next = (HttpContext hc) => throw exception;
        var middleware = new ExceptionHandlingMiddleware(next, _logger);

        // Act
        await middleware.InvokeAsync(_context);

        // Assert
        _context.Response.StatusCode.Should().Be((int)HttpStatusCode.Forbidden);

        var response = await GetResponseBody<ErrorResponse>();
        response.Error.Code.Should().Be("UNAUTHORIZED_ACCESS");
        response.Error.Message.Should().Contain("does not have permission to access world");
    }

    #endregion

    #region ArgumentException Tests

    [TestMethod]
    public async Task InvokeAsync_WithArgumentException_Returns400()
    {
        // Arrange
        var exception = new ArgumentException("Invalid argument");

        RequestDelegate next = (HttpContext hc) => throw exception;
        var middleware = new ExceptionHandlingMiddleware(next, _logger);

        // Act
        await middleware.InvokeAsync(_context);

        // Assert
        _context.Response.StatusCode.Should().Be((int)HttpStatusCode.BadRequest);

        var response = await GetResponseBody<ErrorResponse>();
        response.Error.Code.Should().Be("INVALID_ARGUMENT");
        response.Error.Message.Should().Be("Invalid argument");
    }

    #endregion

    #region KeyNotFoundException Tests

    [TestMethod]
    public async Task InvokeAsync_WithKeyNotFoundException_Returns404()
    {
        // Arrange
        var exception = new KeyNotFoundException("Key not found");

        RequestDelegate next = (HttpContext hc) => throw exception;
        var middleware = new ExceptionHandlingMiddleware(next, _logger);

        // Act
        await middleware.InvokeAsync(_context);

        // Assert
        _context.Response.StatusCode.Should().Be((int)HttpStatusCode.NotFound);

        var response = await GetResponseBody<ErrorResponse>();
        response.Error.Code.Should().Be("NOT_FOUND");
        response.Error.Message.Should().Be("Key not found");
    }

    #endregion

    #region UnauthorizedAccessException Tests

    [TestMethod]
    public async Task InvokeAsync_WithUnauthorizedAccessException_Returns403()
    {
        // Arrange
        var exception = new UnauthorizedAccessException("Access denied");

        RequestDelegate next = (HttpContext hc) => throw exception;
        var middleware = new ExceptionHandlingMiddleware(next, _logger);

        // Act
        await middleware.InvokeAsync(_context);

        // Assert
        _context.Response.StatusCode.Should().Be((int)HttpStatusCode.Forbidden);

        var response = await GetResponseBody<ErrorResponse>();
        response.Error.Code.Should().Be("FORBIDDEN");
        response.Error.Message.Should().Be("Access denied");
    }

    #endregion

    #region InvalidOperationException Tests

    [TestMethod]
    public async Task InvokeAsync_WithInvalidOperationException_Returns409()
    {
        // Arrange
        var exception = new InvalidOperationException("Operation conflict");

        RequestDelegate next = (HttpContext hc) => throw exception;
        var middleware = new ExceptionHandlingMiddleware(next, _logger);

        // Act
        await middleware.InvokeAsync(_context);

        // Assert
        _context.Response.StatusCode.Should().Be((int)HttpStatusCode.Conflict);

        var response = await GetResponseBody<ErrorResponse>();
        response.Error.Code.Should().Be("CONFLICT");
        response.Error.Message.Should().Be("Operation conflict");
    }

    #endregion

    #region Generic Exception Tests

    [TestMethod]
    public async Task InvokeAsync_WithUnhandledException_Returns500()
    {
        // Arrange
        var exception = new Exception("Unexpected error");

        RequestDelegate next = (HttpContext hc) => throw exception;
        var middleware = new ExceptionHandlingMiddleware(next, _logger);

        // Act
        await middleware.InvokeAsync(_context);

        // Assert
        _context.Response.StatusCode.Should().Be((int)HttpStatusCode.InternalServerError);

        var response = await GetResponseBody<ErrorResponse>();
        response.Error.Code.Should().Be("INTERNAL_ERROR");
        response.Error.Message.Should().Be("An unexpected error occurred");
    }

    #endregion

    #region No Exception Tests

    [TestMethod]
    public async Task InvokeAsync_WithNoException_CallsNextMiddleware()
    {
        // Arrange
        var nextCalled = false;
        RequestDelegate next = (HttpContext hc) =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        };
        var middleware = new ExceptionHandlingMiddleware(next, _logger);

        // Act
        await middleware.InvokeAsync(_context);

        // Assert
        nextCalled.Should().BeTrue();
        _context.Response.StatusCode.Should().Be(200); // Default status
    }

    #endregion

    #region Logging Tests

    [TestMethod]
    public async Task InvokeAsync_WithException_LogsError()
    {
        // Arrange
        var exception = new InvalidOperationException("Test error");
        RequestDelegate next = (HttpContext hc) => throw exception;
        var middleware = new ExceptionHandlingMiddleware(next, _logger);

        // Act
        await middleware.InvokeAsync(_context);

        // Assert
        _logger.Received(1).Log(
            LogLevel.Error,
            Arg.Any<EventId>(),
            Arg.Any<object>(),
            exception,
            Arg.Any<Func<object, Exception?, string>>());
    }

    #endregion

    #region Helper Methods

    private async Task<T> GetResponseBody<T>()
    {
        _context.Response.Body.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(_context.Response.Body);
        var body = await reader.ReadToEndAsync();

        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        return JsonSerializer.Deserialize<T>(body, options)!;
    }

    #endregion
}
