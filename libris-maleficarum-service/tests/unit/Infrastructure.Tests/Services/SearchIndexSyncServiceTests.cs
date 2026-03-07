namespace LibrisMaleficarum.Infrastructure.Tests.Services;

using FluentAssertions;
using LibrisMaleficarum.Domain.Entities;
using LibrisMaleficarum.Domain.ValueObjects;
using LibrisMaleficarum.Infrastructure.Services;

/// <summary>
/// Unit tests for SearchIndexSyncService internal static helper methods:
/// BuildEmbeddingContent and MapToSearchDocument.
/// </summary>
[TestClass]
[TestCategory("Unit")]
public class SearchIndexSyncServiceTests
{
    private static readonly Guid TestWorldId = Guid.NewGuid();
    private static readonly string TestOwnerId = "test-user-1";

    #region BuildEmbeddingContent

    [TestMethod]
    public void BuildEmbeddingContent_AllFields_ConcatenatesCorrectly()
    {
        // Arrange
        var entity = WorldEntity.Create(
            worldId: TestWorldId,
            entityType: EntityType.Character,
            name: "Gandalf",
            ownerId: TestOwnerId,
            description: "A wise wizard",
            tags: new List<string> { "wizard", "istari" },
            attributes: new Dictionary<string, object> { ["power"] = "fire" });

        // Act
        var result = SearchIndexSyncService.BuildEmbeddingContent(entity);

        // Assert
        result.Should().Contain("Gandalf");
        result.Should().Contain("A wise wizard");
        result.Should().Contain("wizard");
        result.Should().Contain("istari");
        result.Should().Contain("power");
    }

    [TestMethod]
    public void BuildEmbeddingContent_OnlyName_StartsWithName()
    {
        // Arrange — entity with no description and no tags still has default Attributes "{}"
        var entity = WorldEntity.Create(
            worldId: TestWorldId,
            entityType: EntityType.Location,
            name: "Rivendell",
            ownerId: TestOwnerId);

        // Act
        var result = SearchIndexSyncService.BuildEmbeddingContent(entity);

        // Assert
        result.Should().StartWith("Rivendell");
        result.Should().NotContain("null");
    }

    [TestMethod]
    public void BuildEmbeddingContent_WithTags_IncludesTags()
    {
        // Arrange
        var entity = WorldEntity.Create(
            worldId: TestWorldId,
            entityType: EntityType.Item,
            name: "Sting",
            ownerId: TestOwnerId,
            tags: new List<string> { "weapon", "elvish", "glowing" });

        // Act
        var result = SearchIndexSyncService.BuildEmbeddingContent(entity);

        // Assert — tags are space-joined within the overall concatenation
        result.Should().Contain("weapon elvish glowing");
    }

    [TestMethod]
    public void BuildEmbeddingContent_WithDescription_IncludesDescription()
    {
        // Arrange
        var entity = WorldEntity.Create(
            worldId: TestWorldId,
            entityType: EntityType.Character,
            name: "Frodo",
            ownerId: TestOwnerId,
            description: "The ring bearer");

        // Act
        var result = SearchIndexSyncService.BuildEmbeddingContent(entity);

        // Assert — default Attributes is "{}" which is non-empty, so it gets appended
        result.Should().StartWith("Frodo The ring bearer");
        result.Should().Contain("Frodo");
        result.Should().Contain("The ring bearer");
    }

    #endregion

    #region MapToSearchDocument

    [TestMethod]
    public void MapToSearchDocument_ValidEntity_MapsAllFields()
    {
        // Arrange
        var parentId = Guid.NewGuid();
        var parentPath = new List<Guid> { Guid.NewGuid() };
        var entity = WorldEntity.Create(
            worldId: TestWorldId,
            entityType: EntityType.Character,
            name: "Aragorn",
            ownerId: TestOwnerId,
            description: "King of Gondor",
            parentId: parentId,
            tags: new List<string> { "king", "ranger" },
            attributes: new Dictionary<string, object> { ["title"] = "Elessar" },
            parentPath: parentPath,
            parentDepth: 1,
            schemaVersion: 2);

        var vector = new ReadOnlyMemory<float>(new float[] { 0.1f, 0.2f, 0.3f });

        // Act
        var document = SearchIndexSyncService.MapToSearchDocument(entity, vector);

        // Assert
        document.Id.Should().Be(entity.Id.ToString());
        document.WorldId.Should().Be(entity.WorldId.ToString());
        document.EntityType.Should().Be("Character");
        document.Name.Should().Be("Aragorn");
        document.Description.Should().Be("King of Gondor");
        document.Tags.Should().BeEquivalentTo(new List<string> { "king", "ranger" });
        document.ParentId.Should().Be(parentId.ToString());
        document.OwnerId.Should().Be(TestOwnerId);
        document.CreatedDate.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
        document.ModifiedDate.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
        document.Path.Should().HaveCount(2); // parentPath (1) + parentId (1)
        document.Depth.Should().Be(2); // parentDepth(1) + 1
        document.Attributes.Should().Contain("Elessar");
        document.SchemaVersion.Should().Be(2);
        document.ContentVector.Length.Should().Be(3);
    }

    [TestMethod]
    public void MapToSearchDocument_NullParentId_MapsAsNull()
    {
        // Arrange
        var entity = WorldEntity.Create(
            worldId: TestWorldId,
            entityType: EntityType.Location,
            name: "The Shire",
            ownerId: TestOwnerId);

        var vector = new ReadOnlyMemory<float>(new float[] { 0.5f });

        // Act
        var document = SearchIndexSyncService.MapToSearchDocument(entity, vector);

        // Assert
        document.ParentId.Should().BeNull();
    }

    [TestMethod]
    public void MapToSearchDocument_NullPath_MapsAsEmptyList()
    {
        // Arrange — root-level entity has empty Path
        var entity = WorldEntity.Create(
            worldId: TestWorldId,
            entityType: EntityType.Continent,
            name: "Middle-earth",
            ownerId: TestOwnerId);

        var vector = new ReadOnlyMemory<float>(new float[] { 0.0f });

        // Act
        var document = SearchIndexSyncService.MapToSearchDocument(entity, vector);

        // Assert
        document.Path.Should().BeEmpty();
    }

    [TestMethod]
    public void MapToSearchDocument_SetsContentVector()
    {
        // Arrange
        var entity = WorldEntity.Create(
            worldId: TestWorldId,
            entityType: EntityType.Item,
            name: "One Ring",
            ownerId: TestOwnerId);

        var expectedVector = new float[] { 1.0f, 2.0f, 3.0f, 4.0f };
        var vector = new ReadOnlyMemory<float>(expectedVector);

        // Act
        var document = SearchIndexSyncService.MapToSearchDocument(entity, vector);

        // Assert
        document.ContentVector.ToArray().Should().BeEquivalentTo(expectedVector);
    }

    [TestMethod]
    public void MapToSearchDocument_EntityTypeString_MatchesEnumName()
    {
        // Arrange
        var entity = WorldEntity.Create(
            worldId: TestWorldId,
            entityType: EntityType.Campaign,
            name: "War of the Ring",
            ownerId: TestOwnerId);

        var vector = new ReadOnlyMemory<float>(new float[] { 0.0f });

        // Act
        var document = SearchIndexSyncService.MapToSearchDocument(entity, vector);

        // Assert
        document.EntityType.Should().Be(nameof(EntityType.Campaign));
    }

    #endregion
}
