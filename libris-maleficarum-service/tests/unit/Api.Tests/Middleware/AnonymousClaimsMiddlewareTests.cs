using System.Security.Claims;
using FluentAssertions;
using LibrisMaleficarum.Api.Middleware;
using Microsoft.AspNetCore.Http;

namespace LibrisMaleficarum.Api.Tests.Middleware;

/// <summary>
/// Unit tests for <see cref="AnonymousClaimsMiddleware"/>.
/// </summary>
[TestClass]
[TestCategory("Unit")]
public class AnonymousClaimsMiddlewareTests
{
    [TestMethod]
    public async Task InvokeAsync_WithNoIdentity_InjectsAnonymousClaims()
    {
        // Arrange
        var context = new DefaultHttpContext();
        var nextCalled = false;

        var middleware = new AnonymousClaimsMiddleware((_) =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        nextCalled.Should().BeTrue();
        context.User.Identity.Should().NotBeNull();
        context.User.Identity!.IsAuthenticated.Should().BeTrue();

        var oidClaim = context.User.FindFirst("oid");
        oidClaim.Should().NotBeNull();
        oidClaim!.Value.Should().Be("_anonymous");

        var scopeClaim = context.User.FindFirst("scp");
        scopeClaim.Should().NotBeNull();
        scopeClaim!.Value.Should().Be("access_as_user");
    }

    [TestMethod]
    public async Task InvokeAsync_WithUnauthenticatedIdentity_InjectsAnonymousClaims()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.User = new ClaimsPrincipal(new ClaimsIdentity()); // unauthenticated

        var nextCalled = false;
        var middleware = new AnonymousClaimsMiddleware((_) =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        nextCalled.Should().BeTrue();
        context.User.Identity!.IsAuthenticated.Should().BeTrue();

        var oidClaim = context.User.FindFirst("oid");
        oidClaim.Should().NotBeNull();
        oidClaim!.Value.Should().Be("_anonymous");
    }

    [TestMethod]
    public async Task InvokeAsync_WithAuthenticatedIdentity_DoesNotOverride()
    {
        // Arrange
        var claims = new[]
        {
            new Claim("oid", "real-user-id"),
            new Claim("scp", "access_as_user"),
        };
        var identity = new ClaimsIdentity(claims, "Bearer");
        var context = new DefaultHttpContext { User = new ClaimsPrincipal(identity) };

        var middleware = new AnonymousClaimsMiddleware((_) => Task.CompletedTask);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.User.FindFirst("oid")!.Value.Should().Be("real-user-id");
    }

    [TestMethod]
    public async Task InvokeAsync_AnonymousIdentity_HasCorrectAuthenticationType()
    {
        // Arrange
        var context = new DefaultHttpContext();
        var middleware = new AnonymousClaimsMiddleware((_) => Task.CompletedTask);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.User.Identity!.AuthenticationType.Should().Be("Anonymous");
    }
}
