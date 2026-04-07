namespace LibrisMaleficarum.Api.Tests.Controllers;

using LibrisMaleficarum.Api.Controllers;
using LibrisMaleficarum.Domain.Configuration;
using LibrisMaleficarum.Domain.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

/// <summary>
/// Unit tests for ConfigController.
/// </summary>
[TestClass]
[TestCategory("Unit")]
public class ConfigControllerTests
{
    private IOptionsMonitor<AccessControlOptions> _accessControlOptions = null!;
    private ConfigController _controller = null!;

    [TestInitialize]
    public void Setup()
    {
        _accessControlOptions = Substitute.For<IOptionsMonitor<AccessControlOptions>>();
        _controller = new ConfigController(_accessControlOptions);
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext(),
        };
    }

    [TestMethod]
    public void GetAccessStatus_WhenAccessCodeConfigured_ShouldReturnRequired()
    {
        // Arrange
        _accessControlOptions.CurrentValue.Returns(new AccessControlOptions { AccessCode = "secret" });

        // Act
        var result = _controller.GetAccessStatus();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<AccessControlStatusResponse>().Subject;
        response.AccessCodeRequired.Should().BeTrue();
    }

    [TestMethod]
    public void GetAccessStatus_WhenAccessCodeEmpty_ShouldReturnNotRequired()
    {
        // Arrange
        _accessControlOptions.CurrentValue.Returns(new AccessControlOptions { AccessCode = "" });

        // Act
        var result = _controller.GetAccessStatus();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<AccessControlStatusResponse>().Subject;
        response.AccessCodeRequired.Should().BeFalse();
    }

    [TestMethod]
    public void GetAccessStatus_WhenAccessCodeNull_ShouldReturnNotRequired()
    {
        // Arrange
        _accessControlOptions.CurrentValue.Returns(new AccessControlOptions { AccessCode = null });

        // Act
        var result = _controller.GetAccessStatus();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<AccessControlStatusResponse>().Subject;
        response.AccessCodeRequired.Should().BeFalse();
    }

    [TestMethod]
    public void Constructor_WithNullOptions_ShouldThrowArgumentNullException()
    {
        // Act
        var act = () => new ConfigController(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }
}
