namespace LibrisMaleficarum.Infrastructure.Tests.Services;

using System.Security.Claims;
using FluentAssertions;
using LibrisMaleficarum.Infrastructure.Services;
using Microsoft.AspNetCore.Http;

/// <summary>
/// Unit tests for <see cref="UserContextService"/>.
/// </summary>
[TestClass]
[TestCategory("Unit")]
public class UserContextServiceTests
{
    [TestMethod]
    public async Task GetCurrentUserIdAsync_WithAuthenticatedUser_ReturnsOidClaim()
    {
        // Arrange
        var claims = new[] { new Claim("oid", "user-oid-123") };
        var identity = new ClaimsIdentity(claims, "Bearer");
        var principal = new ClaimsPrincipal(identity);

        var httpContext = new DefaultHttpContext { User = principal };
        var accessor = new HttpContextAccessor { HttpContext = httpContext };
        var service = new UserContextService(accessor);

        // Act
        var result = await service.GetCurrentUserIdAsync();

        // Assert
        result.Should().Be("user-oid-123");
    }

    [TestMethod]
    public async Task GetCurrentUserIdAsync_WithAnonymousClaims_ReturnsAnonymous()
    {
        // Arrange
        var claims = new[] { new Claim("oid", "_anonymous"), new Claim("scp", "access_as_user") };
        var identity = new ClaimsIdentity(claims, "Anonymous");
        var principal = new ClaimsPrincipal(identity);

        var httpContext = new DefaultHttpContext { User = principal };
        var accessor = new HttpContextAccessor { HttpContext = httpContext };
        var service = new UserContextService(accessor);

        // Act
        var result = await service.GetCurrentUserIdAsync();

        // Assert
        result.Should().Be("_anonymous");
    }

    [TestMethod]
    public async Task GetCurrentUserIdAsync_WithNoHttpContext_ReturnsAnonymous()
    {
        // Arrange
        var accessor = new HttpContextAccessor { HttpContext = null };
        var service = new UserContextService(accessor);

        // Act
        var result = await service.GetCurrentUserIdAsync();

        // Assert
        result.Should().Be("_anonymous");
    }

    [TestMethod]
    public async Task GetCurrentUserIdAsync_WithUnauthenticatedUser_ReturnsAnonymous()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        var accessor = new HttpContextAccessor { HttpContext = httpContext };
        var service = new UserContextService(accessor);

        // Act
        var result = await service.GetCurrentUserIdAsync();

        // Assert
        result.Should().Be("_anonymous");
    }

    [TestMethod]
    public async Task GetCurrentUserIdAsync_WithFullOidClaimUri_ReturnsOidValue()
    {
        // Arrange
        var claims = new[] { new Claim("http://schemas.microsoft.com/identity/claims/objectidentifier", "full-uri-oid") };
        var identity = new ClaimsIdentity(claims, "Bearer");
        var principal = new ClaimsPrincipal(identity);

        var httpContext = new DefaultHttpContext { User = principal };
        var accessor = new HttpContextAccessor { HttpContext = httpContext };
        var service = new UserContextService(accessor);

        // Act
        var result = await service.GetCurrentUserIdAsync();

        // Assert
        result.Should().Be("full-uri-oid");
    }

    [TestMethod]
    public async Task GetCurrentUserIdAsync_ReturnsSameIdOnMultipleCalls()
    {
        // Arrange
        var claims = new[] { new Claim("oid", "consistent-user") };
        var identity = new ClaimsIdentity(claims, "Bearer");
        var httpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) };
        var accessor = new HttpContextAccessor { HttpContext = httpContext };
        var service = new UserContextService(accessor);

        // Act
        var result1 = await service.GetCurrentUserIdAsync();
        var result2 = await service.GetCurrentUserIdAsync();

        // Assert
        result1.Should().Be(result2);
    }
}
