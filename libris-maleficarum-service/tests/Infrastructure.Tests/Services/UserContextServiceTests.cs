namespace LibrisMaleficarum.Infrastructure.Tests.Services;

using FluentAssertions;
using LibrisMaleficarum.Infrastructure.Services;

/// <summary>
/// Unit tests for UserContextService.
/// Tests stubbed user context behavior for local development.
/// </summary>
[TestClass]
public class UserContextServiceTests
{
    [TestMethod]
    public async Task GetCurrentUserIdAsync_ReturnsStubUserId()
    {
        // Arrange
        var service = new UserContextService();
        var expectedUserId = new Guid("00000000-0000-0000-0000-000000000001");

        // Act
        var result = await service.GetCurrentUserIdAsync();

        // Assert
        result.Should().Be(expectedUserId);
    }

    [TestMethod]
    public async Task GetCurrentUserIdAsync_ReturnsSameIdOnMultipleCalls()
    {
        // Arrange
        var service = new UserContextService();

        // Act
        var result1 = await service.GetCurrentUserIdAsync();
        var result2 = await service.GetCurrentUserIdAsync();

        // Assert
        result1.Should().Be(result2);
    }

    [TestMethod]
    public async Task GetCurrentUserIdAsync_ReturnsNonEmptyGuid()
    {
        // Arrange
        var service = new UserContextService();

        // Act
        var result = await service.GetCurrentUserIdAsync();

        // Assert
        result.Should().NotBe(Guid.Empty);
    }
}
