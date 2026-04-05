using System.Security.Claims;
using FluentAssertions;
using LibrisMaleficarum.Api.Extensions;

namespace LibrisMaleficarum.Api.Tests.Extensions;

/// <summary>
/// Unit tests for <see cref="ClaimsPrincipalExtensions"/>.
/// </summary>
[TestClass]
[TestCategory("Unit")]
public class ClaimsPrincipalExtensionsTests
{
    [TestMethod]
    public void GetUserIdOrAnonymous_WithOidClaim_ReturnsOidValue()
    {
        // Arrange
        var claims = new[] { new Claim("http://schemas.microsoft.com/identity/claims/objectidentifier", "user-oid-123") };
        var identity = new ClaimsIdentity(claims, "Bearer");
        var principal = new ClaimsPrincipal(identity);

        // Act
        var result = principal.GetUserIdOrAnonymous();

        // Assert
        result.Should().Be("user-oid-123");
    }

    [TestMethod]
    public void GetUserIdOrAnonymous_WithShortOidClaim_ReturnsOidValue()
    {
        // Arrange
        var claims = new[] { new Claim("oid", "user-oid-456") };
        var identity = new ClaimsIdentity(claims, "Bearer");
        var principal = new ClaimsPrincipal(identity);

        // Act
        var result = principal.GetUserIdOrAnonymous();

        // Assert
        result.Should().Be("user-oid-456");
    }

    [TestMethod]
    public void GetUserIdOrAnonymous_WithNoClaims_ReturnsAnonymous()
    {
        // Arrange
        var principal = new ClaimsPrincipal(new ClaimsIdentity());

        // Act
        var result = principal.GetUserIdOrAnonymous();

        // Assert
        result.Should().Be("_anonymous");
    }

    [TestMethod]
    public void GetUserIdOrAnonymous_WithOtherClaimsButNoOid_ReturnsAnonymous()
    {
        // Arrange
        var claims = new[] { new Claim(ClaimTypes.Email, "user@example.com") };
        var identity = new ClaimsIdentity(claims, "Bearer");
        var principal = new ClaimsPrincipal(identity);

        // Act
        var result = principal.GetUserIdOrAnonymous();

        // Assert
        result.Should().Be("_anonymous");
    }

    [TestMethod]
    public void GetUserIdOrAnonymous_WithEmptyOidClaim_ReturnsAnonymous()
    {
        // Arrange
        var claims = new[] { new Claim("oid", "") };
        var identity = new ClaimsIdentity(claims, "Bearer");
        var principal = new ClaimsPrincipal(identity);

        // Act
        var result = principal.GetUserIdOrAnonymous();

        // Assert
        result.Should().Be("_anonymous");
    }

    [TestMethod]
    public void GetUserIdOrAnonymous_WithNullPrincipal_ReturnsAnonymous()
    {
        // Arrange
        ClaimsPrincipal? principal = null;

        // Act
        var result = principal.GetUserIdOrAnonymous();

        // Assert
        result.Should().Be("_anonymous");
    }
}
