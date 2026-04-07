namespace LibrisMaleficarum.Domain.Tests.Models;

using LibrisMaleficarum.Domain.Models;
using System.Text.Json;

/// <summary>
/// Unit tests for AccessControlStatusResponse.
/// </summary>
[TestClass]
[TestCategory("Unit")]
public class AccessControlStatusResponseTests
{
    [TestMethod]
    public void Serialization_ShouldUseCamelCasePropertyName()
    {
        // Arrange
        var response = new AccessControlStatusResponse { AccessCodeRequired = true };

        // Act
        var json = JsonSerializer.Serialize(response);

        // Assert
        json.Should().Contain("\"accessCodeRequired\"");
        json.Should().NotContain("\"AccessCodeRequired\"");
    }

    [TestMethod]
    public void Deserialization_ShouldWorkWithCamelCase()
    {
        // Arrange
        var json = """{"accessCodeRequired":true}""";

        // Act
        var result = JsonSerializer.Deserialize<AccessControlStatusResponse>(json);

        // Assert
        result.Should().NotBeNull();
        result!.AccessCodeRequired.Should().BeTrue();
    }

    [TestMethod]
    public void Serialization_WhenFalse_ShouldSerializeCorrectly()
    {
        // Arrange
        var response = new AccessControlStatusResponse { AccessCodeRequired = false };

        // Act
        var json = JsonSerializer.Serialize(response);

        // Assert
        json.Should().Be("""{"accessCodeRequired":false}""");
    }

    [TestMethod]
    public void RecordWith_ShouldCreateModifiedCopy()
    {
        // Arrange
        var original = new AccessControlStatusResponse { AccessCodeRequired = false };

        // Act
        var modified = original with { AccessCodeRequired = true };

        // Assert
        original.AccessCodeRequired.Should().BeFalse();
        modified.AccessCodeRequired.Should().BeTrue();
    }
}
