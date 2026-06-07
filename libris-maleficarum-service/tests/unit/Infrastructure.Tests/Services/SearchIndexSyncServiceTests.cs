namespace LibrisMaleficarum.Infrastructure.Tests.Services;

using FluentAssertions;
using LibrisMaleficarum.Domain.Entities;
using LibrisMaleficarum.Domain.ValueObjects;
using LibrisMaleficarum.Infrastructure.Configuration;
using LibrisMaleficarum.Infrastructure.Services;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Diagnostics.Metrics;
using System.Text.Json;

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
            properties: new Dictionary<string, object> { ["power"] = "fire" });

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
        // Arrange — entity with no description and no tags still has default Properties "{}"
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

        // Assert — default Properties is "{}" which is non-empty, so it gets appended
        result.Should().StartWith("Frodo The ring bearer");
        result.Should().Contain("Frodo");
        result.Should().Contain("The ring bearer");
    }

    #endregion

    #region ConstructorGuards

    [TestMethod]
    public void Constructor_NullMeter_ThrowsArgumentNullException()
    {
        // Arrange
        var scopeFactory = Substitute.For<IServiceScopeFactory>();
        var cosmosClient = (CosmosClient)RuntimeHelpers.GetUninitializedObject(typeof(CosmosClient));
        var options = Options.Create(new SearchOptions());
        var logger = Substitute.For<ILogger<SearchIndexSyncService>>();

        // Act
        Action act = () => new SearchIndexSyncService(
            scopeFactory,
            cosmosClient,
            options,
            null!,
            logger);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("meter");
    }

    [TestMethod]
    public void Constructor_ValidDependencies_DoesNotThrow()
    {
        // Arrange
        var scopeFactory = Substitute.For<IServiceScopeFactory>();
        var cosmosClient = (CosmosClient)RuntimeHelpers.GetUninitializedObject(typeof(CosmosClient));
        var options = Options.Create(new SearchOptions());
        var logger = Substitute.For<ILogger<SearchIndexSyncService>>();
        using var meter = new Meter("LibrisMaleficarum.SearchIndexWorker.Tests");

        // Act
        Action act = () => _ = new SearchIndexSyncService(
            scopeFactory,
            cosmosClient,
            options,
            meter,
            logger);

        // Assert
        act.Should().NotThrow();
    }

    #endregion

    #region TryParseUnixTimestamp

    [TestMethod]
    public void TryParseUnixTimestamp_ValidNumber_ReturnsTrueAndValue()
    {
        // Arrange
        using var document = JsonDocument.Parse("""
        {
            "_ts": 1717660800
        }
        """);

        // Act
        var result = InvokeTryParseUnixTimestamp(document.RootElement, "_ts", out var value);

        // Assert
        result.Should().BeTrue();
        value.Should().Be(1717660800);
    }

    [TestMethod]
    public void TryParseUnixTimestamp_MissingProperty_ReturnsFalseAndZero()
    {
        // Arrange
        using var document = JsonDocument.Parse("""
        {
            "id": "abc"
        }
        """);

        // Act
        var result = InvokeTryParseUnixTimestamp(document.RootElement, "_ts", out var value);

        // Assert
        result.Should().BeFalse();
        value.Should().Be(0);
    }

    [TestMethod]
    public void TryParseUnixTimestamp_NonNumericProperty_ReturnsFalseAndZero()
    {
        // Arrange
        using var document = JsonDocument.Parse("""
        {
            "_ts": "not-a-number"
        }
        """);

        // Act
        var result = InvokeTryParseUnixTimestamp(document.RootElement, "_ts", out var value);

        // Assert
        result.Should().BeFalse();
        value.Should().Be(0);
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
            properties: new Dictionary<string, object> { ["title"] = "Elessar" },
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
        document.CreatedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
        document.UpdatedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
        document.Path.Should().HaveCount(2); // parentPath (1) + parentId (1)
        document.Depth.Should().Be(2); // parentDepth(1) + 1
        document.Properties.Should().Contain("Elessar");
        document.SchemaVersion.Should().Be(2);
        Assert.IsNotNull(document.ContentVector);
        document.ContentVector.Should().HaveCount(3);
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
        document.ContentVector.Should().BeEquivalentTo(expectedVector);
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

    #region TryMapToWorldEntity

    [TestMethod]
    public void TryMapToWorldEntity_ValidDocument_ReturnsMappedEntity()
    {
        // Arrange
        var entityId = Guid.NewGuid();
        var parentId = Guid.NewGuid();
        var createdAt = DateTime.UtcNow.AddMinutes(-10);
        var updatedAt = DateTime.UtcNow;

        var json = $$"""
                {
                    "id": "{{entityId}}",
                    "worldId": "{{TestWorldId}}",
                    "parentId": "{{parentId}}",
                    "entityType": "Character",
                    "name": "Drizzt",
                    "ownerId": "{{TestOwnerId}}",
                    "description": "Drow ranger",
                    "tags": ["ranger", "drow"],
                    "path": ["{{TestWorldId}}", "{{parentId}}"],
                    "depth": 2,
                    "schemaId": "character-v1",
                    "schemaVersion": 3,
                    "properties": { "class": "Ranger", "level": 12 },
                    "systemProperties": { "source": "import" },
                    "hasChildren": true,
                    "createdAt": "{{createdAt:O}}",
                    "updatedAt": "{{updatedAt:O}}",
                    "isDeleted": false,
                    "_type": "WorldEntity"
                }
                """;

        using var document = JsonDocument.Parse(json);

        // Act
        var result = InvokeTryMapToWorldEntity(document.RootElement, out var entity);

        // Assert
        result.Should().BeTrue();
        entity.Should().NotBeNull();
        entity.Id.Should().Be(entityId);
        entity.WorldId.Should().Be(TestWorldId);
        entity.ParentId.Should().Be(parentId);
        entity.EntityType.Should().Be(EntityType.Character);
        entity.Name.Should().Be("Drizzt");
        entity.OwnerId.Should().Be(TestOwnerId);
        entity.Description.Should().Be("Drow ranger");
        entity.Tags.Should().BeEquivalentTo(["ranger", "drow"]);
        entity.Path.Should().BeEquivalentTo([TestWorldId, parentId]);
        entity.Depth.Should().Be(2);
        entity.SchemaId.Should().Be("character-v1");
        entity.SchemaVersion.Should().Be(3);
        entity.Properties.Should().NotBeNull();
        entity.Properties!.Should().ContainKey("class");
        entity.SystemProperties.Should().NotBeNull();
        entity.SystemProperties!.Should().ContainKey("source");
        entity.HasChildren.Should().BeTrue();
        entity.IsDeleted.Should().BeFalse();
    }

    [TestMethod]
    public void TryMapToWorldEntity_MissingDiscriminator_ReturnsFalse()
    {
        // Arrange
        var json = $$"""
                {
                    "id": "{{Guid.NewGuid()}}",
                    "worldId": "{{TestWorldId}}",
                    "entityType": "Character",
                    "name": "Bruenor",
                    "ownerId": "{{TestOwnerId}}"
                }
                """;

        using var document = JsonDocument.Parse(json);

        // Act
        var result = InvokeTryMapToWorldEntity(document.RootElement, out var entity);

        // Assert
        result.Should().BeFalse();
        entity.Should().BeNull();
    }

    [TestMethod]
    public void TryMapToWorldEntity_NonWorldEntityDiscriminator_ReturnsFalse()
    {
        // Arrange
        var json = $$"""
                {
                    "id": "{{Guid.NewGuid()}}",
                    "worldId": "{{TestWorldId}}",
                    "entityType": "Character",
                    "name": "Wulfgar",
                    "ownerId": "{{TestOwnerId}}",
                    "_type": "DeleteOperation"
                }
                """;

        using var document = JsonDocument.Parse(json);

        // Act
        var result = InvokeTryMapToWorldEntity(document.RootElement, out var entity);

        // Assert
        result.Should().BeFalse();
        entity.Should().BeNull();
    }

    [TestMethod]
    public void TryMapToWorldEntity_NumericEntityType_MapsEnumValue()
    {
        // Arrange
        var numericEntityType = (int)EntityType.Location;
        var json = $$"""
                {
                    "id": "{{Guid.NewGuid()}}",
                    "worldId": "{{TestWorldId}}",
                    "entityType": {{numericEntityType}},
                    "name": "Icewind Dale",
                    "ownerId": "{{TestOwnerId}}",
                    "_type": "WorldEntity"
                }
                """;

        using var document = JsonDocument.Parse(json);

        // Act
        var result = InvokeTryMapToWorldEntity(document.RootElement, out var entity);

        // Assert
        result.Should().BeTrue();
        entity.Should().NotBeNull();
        entity.EntityType.Should().Be(EntityType.Location);
    }

    [TestMethod]
    public void TryMapToWorldEntity_InvalidEntityType_Throws()
    {
        // Arrange
        var json = $$"""
                {
                    "id": "{{Guid.NewGuid()}}",
                    "worldId": "{{TestWorldId}}",
                    "entityType": "NotARealType",
                    "name": "Catti-brie",
                    "ownerId": "{{TestOwnerId}}",
                    "_type": "WorldEntity"
                }
                """;

        using var document = JsonDocument.Parse(json);

        // Act
        Action act = () => InvokeTryMapToWorldEntity(document.RootElement, out _);

        // Assert
        var exception = act.Should().Throw<TargetInvocationException>().Which;
        exception.InnerException.Should().BeOfType<InvalidOperationException>();
        exception.InnerException!.Message.Should().Contain("Invalid EntityType value");
    }

    [TestMethod]
    public void TryMapToWorldEntity_SoftDeletedDocument_MapsDeletionFields()
    {
        // Arrange
        var deletedDate = DateTime.UtcNow.AddDays(-1);
        var json = $$"""
                {
                    "id": "{{Guid.NewGuid()}}",
                    "worldId": "{{TestWorldId}}",
                    "entityType": "Character",
                    "name": "Regis",
                    "ownerId": "{{TestOwnerId}}",
                    "isDeleted": true,
                    "deletedDate": "{{deletedDate:O}}",
                    "deletedBy": "deleter-user",
                    "ttl": 7776000,
                    "_type": "WorldEntity"
                }
                """;

        using var document = JsonDocument.Parse(json);

        // Act
        var result = InvokeTryMapToWorldEntity(document.RootElement, out var entity);

        // Assert
        result.Should().BeTrue();
        entity.Should().NotBeNull();
        entity.IsDeleted.Should().BeTrue();
        entity.DeletedDate.Should().NotBeNull();
        entity.DeletedBy.Should().Be("deleter-user");
        entity.Ttl.Should().Be(7776000);
    }

    #endregion

    private static bool InvokeTryMapToWorldEntity(JsonElement change, out WorldEntity? entity)
    {
        var method = typeof(SearchIndexSyncService).GetMethod(
                "TryMapToWorldEntity",
                BindingFlags.NonPublic | BindingFlags.Static);

        method.Should().NotBeNull("TryMapToWorldEntity should exist for mapping change-feed documents");

        var args = new object?[] { change, null };

        var result = method!.Invoke(null, args);

        result.Should().NotBeNull();
        entity = args[1] as WorldEntity;
        return (bool)result!;
    }

    private static bool InvokeTryParseUnixTimestamp(JsonElement document, string propertyName, out long value)
    {
        var method = typeof(SearchIndexSyncService).GetMethod(
            "TryParseUnixTimestamp",
            BindingFlags.NonPublic | BindingFlags.Static);

        method.Should().NotBeNull("TryParseUnixTimestamp should exist for parsing Cosmos _ts values");

        var args = new object?[] { document, propertyName, 0L };

        var result = method!.Invoke(null, args);

        result.Should().NotBeNull();
        value = (long)args[2]!;
        return (bool)result!;
    }
}
