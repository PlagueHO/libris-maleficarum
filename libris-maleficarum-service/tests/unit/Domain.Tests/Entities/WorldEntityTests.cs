namespace LibrisMaleficarum.Domain.Tests.Entities;

using FluentAssertions;
using LibrisMaleficarum.Domain.Entities;
using LibrisMaleficarum.Domain.ValueObjects;

/// <summary>
/// Unit tests for the WorldEntity entity.
/// </summary>
[TestClass]
[TestCategory("Unit")]
public class WorldEntityTests
{
    [TestMethod]
    public void Create_WithValidParameters_ShouldCreateEntity()
    {
        // Arrange
        var worldId = Guid.NewGuid();
        var entityType = EntityType.Character;
        var name = "Aragorn";
        var description = "Ranger of the North";
        var tags = new List<string> { "hero", "ranger" };
        var attributes = new Dictionary<string, object> { ["level"] = 10, ["class"] = "ranger" };

        // Act
        var entity = WorldEntity.Create(worldId, entityType, name, description, null, tags, attributes);

        // Assert
        entity.Should().NotBeNull();
        entity.Id.Should().NotBeEmpty();
        entity.WorldId.Should().Be(worldId);
        entity.EntityType.Should().Be(entityType);
        entity.Name.Should().Be(name);
        entity.Description.Should().Be(description);
        entity.ParentId.Should().BeNull();
        entity.Tags.Should().HaveCount(2);
        entity.Tags.Should().Contain("hero");
        entity.Tags.Should().Contain("ranger");
        entity.GetAttributes().Should().ContainKey("level");
        entity.GetAttributes().Should().ContainKey("class");
        entity.CreatedDate.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        entity.ModifiedDate.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        entity.IsDeleted.Should().BeFalse();
    }

    [TestMethod]
    public void Create_WithoutOptionalParameters_ShouldCreateEntity()
    {
        // Arrange
        var worldId = Guid.NewGuid();
        var entityType = EntityType.Location;
        var name = "Rivendell";

        // Act
        var entity = WorldEntity.Create(worldId, entityType, name);

        // Assert
        entity.Should().NotBeNull();
        entity.Name.Should().Be(name);
        entity.Description.Should().BeNull();
        entity.ParentId.Should().BeNull();
        entity.Tags.Should().BeEmpty();
        entity.GetAttributes().Should().BeEmpty();
    }

    [TestMethod]
    public void Validate_WithEmptyName_ShouldThrowArgumentException()
    {
        // Arrange
        var worldId = Guid.NewGuid();
        var entityType = EntityType.Character;

        // Act
        var action = () => WorldEntity.Create(worldId, entityType, string.Empty);

        // Assert
        action.Should().Throw<ArgumentException>()
            .WithMessage("Entity name is required.");
    }

    [TestMethod]
    public void Validate_WithNameTooLong_ShouldThrowArgumentException()
    {
        // Arrange
        var worldId = Guid.NewGuid();
        var entityType = EntityType.Character;
        var nameTooLong = new string('A', 201);

        // Act
        var action = () => WorldEntity.Create(worldId, entityType, nameTooLong);

        // Assert
        action.Should().Throw<ArgumentException>()
            .WithMessage("Entity name must not exceed 200 characters.");
    }

    [TestMethod]
    public void Validate_WithDescriptionTooLong_ShouldThrowArgumentException()
    {
        // Arrange
        var worldId = Guid.NewGuid();
        var entityType = EntityType.Character;
        var name = "Frodo";
        var descriptionTooLong = new string('B', 5001);

        // Act
        var action = () => WorldEntity.Create(worldId, entityType, name, descriptionTooLong);

        // Assert
        action.Should().Throw<ArgumentException>()
            .WithMessage("Description must not exceed 5000 characters.");
    }

    [TestMethod]
    public void Validate_WithTooManyTags_ShouldThrowArgumentException()
    {
        // Arrange
        var worldId = Guid.NewGuid();
        var entityType = EntityType.Character;
        var name = "Gandalf";
        var tooManyTags = Enumerable.Range(1, 21).Select(i => $"tag{i}").ToList();

        // Act
        var action = () => WorldEntity.Create(worldId, entityType, name, tags: tooManyTags);

        // Assert
        action.Should().Throw<ArgumentException>()
            .WithMessage("Maximum 20 tags allowed.");
    }

    [TestMethod]
    public void Validate_WithTagTooLong_ShouldThrowArgumentException()
    {
        // Arrange
        var worldId = Guid.NewGuid();
        var entityType = EntityType.Character;
        var name = "Legolas";
        var tagTooLong = new List<string> { new string('C', 51) };

        // Act
        var action = () => WorldEntity.Create(worldId, entityType, name, tags: tagTooLong);

        // Assert
        action.Should().Throw<ArgumentException>()
            .WithMessage("Each tag must be 1-50 characters.");
    }

    [TestMethod]
    public void Update_WithValidData_ShouldUpdateEntity()
    {
        // Arrange
        var worldId = Guid.NewGuid();
        var entity = WorldEntity.Create(worldId, EntityType.Character, "Aragorn", "Ranger");
        var originalModifiedDate = entity.ModifiedDate;
        System.Threading.Thread.Sleep(10); // Ensure time difference

        var newName = "Aragorn II Elessar";
        var newDescription = "King of Gondor";
        var newTags = new List<string> { "king", "hero" };
        var newAttributes = new Dictionary<string, object> { ["title"] = "King" };

        // Act
        entity.Update(newName, newDescription, EntityType.Character, null, newTags, newAttributes);

        // Assert
        entity.Name.Should().Be(newName);
        entity.Description.Should().Be(newDescription);
        entity.Tags.Should().HaveCount(2);
        entity.GetAttributes().Should().ContainKey("title");
        entity.ModifiedDate.Should().BeAfter(originalModifiedDate);
    }

    [TestMethod]
    public void Update_WithInvalidName_ShouldThrowArgumentException()
    {
        // Arrange
        var worldId = Guid.NewGuid();
        var entity = WorldEntity.Create(worldId, EntityType.Character, "Frodo");

        // Act
        var action = () => entity.Update(string.Empty, null, EntityType.Character, null, null, null);

        // Assert
        action.Should().Throw<ArgumentException>()
            .WithMessage("Entity name is required.");
    }

    [TestMethod]
    public void Move_ShouldUpdateParentId()
    {
        // Arrange
        var worldId = Guid.NewGuid();
        var entity = WorldEntity.Create(worldId, EntityType.City, "Minas Tirith");
        var newParentId = Guid.NewGuid();
        var originalModifiedDate = entity.ModifiedDate;
        System.Threading.Thread.Sleep(10); // Ensure time difference

        // Act
        entity.Move(newParentId);

        // Assert
        entity.ParentId.Should().Be(newParentId);
        entity.ModifiedDate.Should().BeAfter(originalModifiedDate);
    }

    [TestMethod]
    public void SoftDelete_ShouldMarkEntityAsDeleted()
    {
        // Arrange
        var worldId = Guid.NewGuid();
        var entity = WorldEntity.Create(worldId, EntityType.Character, "Boromir");
        var originalModifiedDate = entity.ModifiedDate;
        System.Threading.Thread.Sleep(10); // Ensure time difference

        // Act
        entity.SoftDelete();

        // Assert
        entity.IsDeleted.Should().BeTrue();
        entity.ModifiedDate.Should().BeAfter(originalModifiedDate);
    }

    [TestMethod]
    public void GetAttributes_ShouldReturnDeserializedDictionary()
    {
        // Arrange
        var worldId = Guid.NewGuid();
        var attributes = new Dictionary<string, object>
        {
            ["strength"] = 18,
            ["dexterity"] = 14,
            ["constitution"] = 16
        };
        var entity = WorldEntity.Create(worldId, EntityType.Character, "Gimli", attributes: attributes);

        // Act
        var retrievedAttributes = entity.GetAttributes();

        // Assert
        retrievedAttributes.Should().HaveCount(3);
        retrievedAttributes.Should().ContainKey("strength");
        retrievedAttributes.Should().ContainKey("dexterity");
        retrievedAttributes.Should().ContainKey("constitution");
    }
}
