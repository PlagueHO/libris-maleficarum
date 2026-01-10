using FluentAssertions;
using LibrisMaleficarum.Domain.Entities;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LibrisMaleficarum.Domain.Tests.Entities;

/// <summary>
/// Unit tests for the <see cref="World"/> entity.
/// </summary>
[TestClass]
[TestCategory("Unit")]
public class WorldTests
{
    [TestMethod]
    public void Create_WithValidParameters_ShouldCreateWorld()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var name = "Test World";
        var description = "A test world description";

        // Act
        var world = World.Create(ownerId, name, description);

        // Assert
        world.Should().NotBeNull();
        world.Id.Should().NotBeEmpty();
        world.OwnerId.Should().Be(ownerId);
        world.Name.Should().Be(name);
        world.Description.Should().Be(description);
        world.CreatedDate.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        world.ModifiedDate.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        world.IsDeleted.Should().BeFalse();
    }

    [TestMethod]
    public void Create_WithoutDescription_ShouldCreateWorld()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var name = "Test World";

        // Act
        var world = World.Create(ownerId, name);

        // Assert
        world.Should().NotBeNull();
        world.Description.Should().BeNull();
    }

    [TestMethod]
    public void Validate_WithValidData_ShouldNotThrow()
    {
        // Arrange
        var world = World.Create(Guid.NewGuid(), "Valid Name", "Valid description");

        // Act
        Action act = () => world.Validate();

        // Assert
        act.Should().NotThrow();
    }

    [TestMethod]
    public void Validate_WithEmptyName_ShouldThrowArgumentException()
    {
        // Arrange
        var ownerId = Guid.NewGuid();

        // Act
        Action act = () => World.Create(ownerId, string.Empty, null);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("Name is required.*");
    }

    [TestMethod]
    public void Validate_WithNameTooLong_ShouldThrowArgumentException()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var longName = new string('A', 101);

        // Act
        Action act = () => World.Create(ownerId, longName, null);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("Name must be between 1 and 100 characters.*");
    }

    [TestMethod]
    public void Validate_WithDescriptionTooLong_ShouldThrowArgumentException()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var longDescription = new string('A', 2001);

        // Act
        Action act = () => World.Create(ownerId, "Valid Name", longDescription);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("Description must not exceed 2000 characters.*");
    }

    [TestMethod]
    public void Update_WithValidData_ShouldUpdateWorld()
    {
        // Arrange
        var world = World.Create(Guid.NewGuid(), "Original Name", "Original Description");
        var originalModifiedDate = world.ModifiedDate;
        Thread.Sleep(10); // Ensure time difference

        var newName = "Updated Name";
        var newDescription = "Updated Description";

        // Act
        world.Update(newName, newDescription);

        // Assert
        world.Name.Should().Be(newName);
        world.Description.Should().Be(newDescription);
        world.ModifiedDate.Should().BeAfter(originalModifiedDate);
    }

    [TestMethod]
    public void Update_WithInvalidName_ShouldThrowArgumentException()
    {
        // Arrange
        var world = World.Create(Guid.NewGuid(), "Valid Name", null);
        var longName = new string('A', 101);

        // Act
        Action act = () => world.Update(longName, null);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("Name must be between 1 and 100 characters.*");
    }

    [TestMethod]
    public void SoftDelete_ShouldMarkWorldAsDeleted()
    {
        // Arrange
        var world = World.Create(Guid.NewGuid(), "Test World", null);
        var originalModifiedDate = world.ModifiedDate;
        Thread.Sleep(10); // Ensure time difference

        // Act
        world.SoftDelete();

        // Assert
        world.IsDeleted.Should().BeTrue();
        world.ModifiedDate.Should().BeAfter(originalModifiedDate);
    }
}
