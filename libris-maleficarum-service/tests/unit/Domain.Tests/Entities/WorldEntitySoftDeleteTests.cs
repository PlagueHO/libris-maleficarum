namespace LibrisMaleficarum.Domain.Tests.Entities;

using FluentAssertions;
using LibrisMaleficarum.Domain.Entities;
using LibrisMaleficarum.Domain.ValueObjects;

/// <summary>
/// Unit tests for WorldEntity.SoftDelete() method.
/// Tests soft delete functionality with audit metadata.
/// </summary>
[TestClass]
[TestCategory("Unit")]
public class WorldEntitySoftDeleteTests
{
    private const string TestOwnerId = "test-owner-id";
    private const string TestDeletedBy = "test-user-id";

    [TestMethod]
    public void SoftDelete_WithValidDeletedBy_ShouldMarkAsDeleted()
    {
        // Arrange
        var worldId = Guid.NewGuid();
        var entity = WorldEntity.Create(worldId, EntityType.Character, "Test Entity", TestOwnerId);

        // Act
        entity.SoftDelete(TestDeletedBy);

        // Assert
        entity.IsDeleted.Should().BeTrue();
        entity.DeletedDate.Should().NotBeNull();
        entity.DeletedDate.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        entity.DeletedBy.Should().Be(TestDeletedBy);
        entity.Ttl.Should().Be(7776000); // 90 days in seconds
    }

    [TestMethod]
    public void SoftDelete_WithValidDeletedBy_ShouldUpdateModifiedDate()
    {
        // Arrange
        var worldId = Guid.NewGuid();
        var entity = WorldEntity.Create(worldId, EntityType.Location, "Test Location", TestOwnerId);
        var originalModifiedDate = entity.ModifiedDate;

        // Wait to ensure time difference
        Thread.Sleep(10);

        // Act
        entity.SoftDelete(TestDeletedBy);

        // Assert
        entity.ModifiedDate.Should().BeAfter(originalModifiedDate);
        entity.ModifiedDate.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [TestMethod]
    public void SoftDelete_WithNullDeletedBy_ShouldThrowArgumentException()
    {
        // Arrange
        var worldId = Guid.NewGuid();
        var entity = WorldEntity.Create(worldId, EntityType.Character, "Test Entity", TestOwnerId);

        // Act
        var action = () => entity.SoftDelete(null!);

        // Assert
        action.Should().Throw<ArgumentException>()
            .WithMessage("*DeletedBy is required*")
            .And.ParamName.Should().Be("deletedBy");
    }

    [TestMethod]
    public void SoftDelete_WithEmptyDeletedBy_ShouldThrowArgumentException()
    {
        // Arrange
        var worldId = Guid.NewGuid();
        var entity = WorldEntity.Create(worldId, EntityType.Location, "Test Location", TestOwnerId);

        // Act
        var action = () => entity.SoftDelete(string.Empty);

        // Assert
        action.Should().Throw<ArgumentException>()
            .WithMessage("*DeletedBy is required*")
            .And.ParamName.Should().Be("deletedBy");
    }

    [TestMethod]
    public void SoftDelete_WithWhitespaceDeletedBy_ShouldThrowArgumentException()
    {
        // Arrange
        var worldId = Guid.NewGuid();
        var entity = WorldEntity.Create(worldId, EntityType.Campaign, "Test Campaign", TestOwnerId);

        // Act
        var action = () => entity.SoftDelete("   ");

        // Assert
        action.Should().Throw<ArgumentException>()
            .WithMessage("*DeletedBy is required*")
            .And.ParamName.Should().Be("deletedBy");
    }

    [TestMethod]
    public void SoftDelete_CalledMultipleTimes_ShouldUpdateDeletedMetadata()
    {
        // Arrange
        var worldId = Guid.NewGuid();
        var entity = WorldEntity.Create(worldId, EntityType.Character, "Test Entity", TestOwnerId);
        entity.SoftDelete("first-user");
        var firstDeletedDate = entity.DeletedDate;

        // Wait to ensure time difference
        Thread.Sleep(10);

        // Act
        entity.SoftDelete("second-user");

        // Assert
        entity.IsDeleted.Should().BeTrue();
        entity.DeletedBy.Should().Be("second-user");
        entity.DeletedDate.Should().BeAfter(firstDeletedDate!.Value);
    }

    [TestMethod]
    public void SoftDelete_ShouldNotModifyOtherProperties()
    {
        // Arrange
        var worldId = Guid.NewGuid();
        var name = "Test Character";
        var description = "Test Description";
        var tags = new List<string> { "tag1", "tag2" };
        var entity = WorldEntity.Create(worldId, EntityType.Character, name, TestOwnerId, description, null, tags);

        // Act
        entity.SoftDelete(TestDeletedBy);

        // Assert
        entity.Name.Should().Be(name);
        entity.Description.Should().Be(description);
        entity.Tags.Should().BeEquivalentTo(tags);
        entity.EntityType.Should().Be(EntityType.Character);
        entity.WorldId.Should().Be(worldId);
    }

    [TestMethod]
    public void Restore_ShouldClearDeletionMetadata()
    {
        // Arrange
        var worldId = Guid.NewGuid();
        var entity = WorldEntity.Create(worldId, EntityType.Character, "Test Entity", TestOwnerId);
        entity.SoftDelete(TestDeletedBy);

        // Wait to ensure time difference
        Thread.Sleep(10);

        // Act
        entity.Restore();

        // Assert
        entity.IsDeleted.Should().BeFalse();
        entity.DeletedDate.Should().BeNull();
        entity.DeletedBy.Should().BeNull();
        entity.Ttl.Should().BeNull();
    }

    [TestMethod]
    public void Restore_ShouldUpdateModifiedDate()
    {
        // Arrange
        var worldId = Guid.NewGuid();
        var entity = WorldEntity.Create(worldId, EntityType.Location, "Test Location", TestOwnerId);
        entity.SoftDelete(TestDeletedBy);
        var deletedModifiedDate = entity.ModifiedDate;

        // Wait to ensure time difference
        Thread.Sleep(10);

        // Act
        entity.Restore();

        // Assert
        entity.ModifiedDate.Should().BeAfter(deletedModifiedDate);
        entity.ModifiedDate.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [TestMethod]
    public void Restore_ShouldNotModifyOtherProperties()
    {
        // Arrange
        var worldId = Guid.NewGuid();
        var name = "Test Campaign";
        var description = "Test Description";
        var tags = new List<string> { "epic", "fantasy" };
        var entity = WorldEntity.Create(worldId, EntityType.Campaign, name, TestOwnerId, description, null, tags);
        entity.SoftDelete(TestDeletedBy);

        // Act
        entity.Restore();

        // Assert
        entity.Name.Should().Be(name);
        entity.Description.Should().Be(description);
        entity.Tags.Should().BeEquivalentTo(tags);
        entity.EntityType.Should().Be(EntityType.Campaign);
        entity.WorldId.Should().Be(worldId);
    }

    [TestMethod]
    public void Ttl_ShouldBeNullForNewEntity()
    {
        // Arrange
        var worldId = Guid.NewGuid();

        // Act
        var entity = WorldEntity.Create(worldId, EntityType.Character, "Test Entity", TestOwnerId);

        // Assert
        entity.Ttl.Should().BeNull();
    }
}
