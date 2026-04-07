namespace LibrisMaleficarum.Api.Tests.Middleware;

using LibrisMaleficarum.Api.Middleware;
using LibrisMaleficarum.Domain.Configuration;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;

/// <summary>
/// Unit tests for AccessCodeMiddleware.
/// </summary>
[TestClass]
[TestCategory("Unit")]
public class AccessCodeMiddlewareTests
{
    private ILogger<AccessCodeMiddleware> _logger = null!;
    private DefaultHttpContext _context = null!;
    private bool _nextCalled;

    [TestInitialize]
    public void Setup()
    {
        _logger = Substitute.For<ILogger<AccessCodeMiddleware>>();
        _context = new DefaultHttpContext();
        _context.Response.Body = new MemoryStream();
        _nextCalled = false;
    }

    private AccessCodeMiddleware CreateMiddleware()
    {
        RequestDelegate next = (_) =>
        {
            _nextCalled = true;
            return Task.CompletedTask;
        };
        return new AccessCodeMiddleware(next, _logger);
    }

    private static IOptionsMonitor<AccessControlOptions> CreateOptions(string? accessCode)
    {
        var options = Substitute.For<IOptionsMonitor<AccessControlOptions>>();
        options.CurrentValue.Returns(new AccessControlOptions { AccessCode = accessCode });
        return options;
    }

    private async Task<T?> GetResponseBody<T>()
    {
        _context.Response.Body.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(_context.Response.Body);
        var body = await reader.ReadToEndAsync();
        return JsonSerializer.Deserialize<T>(body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
    }

    #region Pass-through when no code configured

    [TestMethod]
    public async Task InvokeAsync_WhenAccessCodeIsNull_ShouldPassThrough()
    {
        // Arrange
        var middleware = CreateMiddleware();
        var options = CreateOptions(null);
        _context.Request.Path = "/api/v1/worlds";

        // Act
        await middleware.InvokeAsync(_context, options);

        // Assert
        _nextCalled.Should().BeTrue();
    }

    [TestMethod]
    public async Task InvokeAsync_WhenAccessCodeIsEmpty_ShouldPassThrough()
    {
        // Arrange
        var middleware = CreateMiddleware();
        var options = CreateOptions(string.Empty);
        _context.Request.Path = "/api/v1/worlds";

        // Act
        await middleware.InvokeAsync(_context, options);

        // Assert
        _nextCalled.Should().BeTrue();
    }

    #endregion

    #region Valid access code

    [TestMethod]
    public async Task InvokeAsync_WhenValidAccessCodeProvided_ShouldPassThrough()
    {
        // Arrange
        var middleware = CreateMiddleware();
        var options = CreateOptions("secret-code");
        _context.Request.Path = "/api/v1/worlds";
        _context.Request.Headers["X-Access-Code"] = "secret-code";

        // Act
        await middleware.InvokeAsync(_context, options);

        // Assert
        _nextCalled.Should().BeTrue();
    }

    #endregion

    #region Missing or wrong access code

    [TestMethod]
    public async Task InvokeAsync_WhenHeaderMissing_ShouldReturn401()
    {
        // Arrange
        var middleware = CreateMiddleware();
        var options = CreateOptions("secret-code");
        _context.Request.Path = "/api/v1/worlds";

        // Act
        await middleware.InvokeAsync(_context, options);

        // Assert
        _nextCalled.Should().BeFalse();
        _context.Response.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);
    }

    [TestMethod]
    public async Task InvokeAsync_WhenWrongAccessCode_ShouldReturn401()
    {
        // Arrange
        var middleware = CreateMiddleware();
        var options = CreateOptions("secret-code");
        _context.Request.Path = "/api/v1/worlds";
        _context.Request.Headers["X-Access-Code"] = "wrong-code";

        // Act
        await middleware.InvokeAsync(_context, options);

        // Assert
        _nextCalled.Should().BeFalse();
        _context.Response.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);
    }

    [TestMethod]
    public async Task InvokeAsync_WhenUnauthorized_ShouldReturnJsonErrorBody()
    {
        // Arrange
        var middleware = CreateMiddleware();
        var options = CreateOptions("secret-code");
        _context.Request.Path = "/api/v1/worlds";

        // Act
        await middleware.InvokeAsync(_context, options);

        // Assert
        _context.Response.ContentType.Should().Be("application/json");
        var body = await GetResponseBody<JsonElement>();
        body.GetProperty("error").GetString().Should().Be("Access code required");
    }

    #endregion

    #region Allowlisted paths

    [TestMethod]
    [DataRow("/health")]
    [DataRow("/alive")]
    [DataRow("/api/config/access-status")]
    [DataRow("/api/error")]
    [DataRow("/openapi")]
    public async Task InvokeAsync_WhenAllowlistedPath_ShouldPassThrough(string path)
    {
        // Arrange
        var middleware = CreateMiddleware();
        var options = CreateOptions("secret-code");
        _context.Request.Path = path;

        // Act
        await middleware.InvokeAsync(_context, options);

        // Assert
        _nextCalled.Should().BeTrue();
    }

    [TestMethod]
    public async Task InvokeAsync_WhenAllowlistedPathSubpath_ShouldPassThrough()
    {
        // Arrange
        var middleware = CreateMiddleware();
        var options = CreateOptions("secret-code");
        _context.Request.Path = "/openapi/v1.json";

        // Act
        await middleware.InvokeAsync(_context, options);

        // Assert
        _nextCalled.Should().BeTrue();
    }

    [TestMethod]
    [DataRow("/Health")]
    [DataRow("/ALIVE")]
    [DataRow("/Api/Config/Access-Status")]
    public async Task InvokeAsync_WhenAllowlistedPathCaseInsensitive_ShouldPassThrough(string path)
    {
        // Arrange
        var middleware = CreateMiddleware();
        var options = CreateOptions("secret-code");
        _context.Request.Path = path;

        // Act
        await middleware.InvokeAsync(_context, options);

        // Assert
        _nextCalled.Should().BeTrue();
    }

    #endregion

    #region Protected paths are blocked

    [TestMethod]
    [DataRow("/api/v1/worlds")]
    [DataRow("/api/v1/worlds/123/entities")]
    [DataRow("/api/v1/config")]
    public async Task InvokeAsync_WhenProtectedPathWithoutCode_ShouldReturn401(string path)
    {
        // Arrange
        var middleware = CreateMiddleware();
        var options = CreateOptions("secret-code");
        _context.Request.Path = path;

        // Act
        await middleware.InvokeAsync(_context, options);

        // Assert
        _nextCalled.Should().BeFalse();
        _context.Response.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);
    }

    #endregion
}
