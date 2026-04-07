namespace LibrisMaleficarum.Domain.Tests.Configuration;

using LibrisMaleficarum.Domain.Configuration;

/// <summary>
/// Unit tests for AccessControlOptions.
/// </summary>
[TestClass]
[TestCategory("Unit")]
public class AccessControlOptionsTests
{
    [TestMethod]
    public void SectionName_ShouldBeAccessControl()
    {
        // Assert
        AccessControlOptions.SectionName.Should().Be("AccessControl");
    }

    [TestMethod]
    public void AccessCode_ShouldDefaultToNull()
    {
        // Act
        var options = new AccessControlOptions();

        // Assert
        options.AccessCode.Should().BeNull();
    }

    [TestMethod]
    public void AccessCode_ShouldBeSettable()
    {
        // Act
        var options = new AccessControlOptions { AccessCode = "my-secret-code" };

        // Assert
        options.AccessCode.Should().Be("my-secret-code");
    }

    [TestMethod]
    public void RecordWith_ShouldCreateModifiedCopy()
    {
        // Arrange
        var original = new AccessControlOptions { AccessCode = "original" };

        // Act
        var modified = original with { AccessCode = "modified" };

        // Assert
        original.AccessCode.Should().Be("original");
        modified.AccessCode.Should().Be("modified");
    }
}
