namespace LibrisMaleficarum.Import.Tests.Models;

using System.Text.Json;
using LibrisMaleficarum.Import.Models;

[TestClass]
[TestCategory("Unit")]
public sealed class EntityImportDefinitionTests
{
    private static readonly JsonSerializerOptions CamelCaseOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    [TestMethod]
    public void Deserialize_AllFields_Success()
    {
        // Arrange
        var json = """
            {
                "localId": "city-waterdeep",
                "entityType": "City",
                "name": "Waterdeep",
                "description": "The City of Splendors",
                "parentLocalId": "region-sword-coast",
                "tags": ["city", "trade-hub"],
                "properties": {
                    "population": 1300000,
                    "government": "Open Lord"
                }
            }
            """;

        // Act
        var entity = JsonSerializer.Deserialize<EntityImportDefinition>(json, CamelCaseOptions)!;

        // Assert
        entity.LocalId.Should().Be("city-waterdeep");
        entity.EntityType.Should().Be("City");
        entity.Name.Should().Be("Waterdeep");
        entity.Description.Should().Be("The City of Splendors");
        entity.ParentLocalId.Should().Be("region-sword-coast");
        entity.Tags.Should().BeEquivalentTo(["city", "trade-hub"]);
        entity.Properties.Should().ContainKey("population");
        entity.Properties.Should().ContainKey("government");
    }

    [TestMethod]
    public void Deserialize_OptionalFieldsMissing_Success()
    {
        // Arrange
        var json = """
            {
                "localId": "continent-faerun",
                "entityType": "Continent",
                "name": "Faerûn"
            }
            """;

        // Act
        var entity = JsonSerializer.Deserialize<EntityImportDefinition>(json, CamelCaseOptions)!;

        // Assert
        entity.LocalId.Should().Be("continent-faerun");
        entity.EntityType.Should().Be("Continent");
        entity.Name.Should().Be("Faerûn");
        entity.Description.Should().BeNull();
        entity.ParentLocalId.Should().BeNull();
        entity.Tags.Should().BeNull();
        entity.Properties.Should().BeNull();
    }

    [TestMethod]
    public void SourceFilePath_NotSerialized()
    {
        // Arrange
        var entity = new EntityImportDefinition
        {
            LocalId = "e1",
            EntityType = "Continent",
            Name = "Test",
            SourceFilePath = "/some/path/entity.json"
        };

        // Act
        var json = JsonSerializer.Serialize(entity, CamelCaseOptions);

        // Assert
        json.Should().NotContain("sourceFilePath");
        json.Should().NotContain("SourceFilePath");
    }

    [TestMethod]
    public void Deserialize_CamelCaseProperties()
    {
        // Arrange — all property names are camelCase
        var json = """
            {
                "localId": "npc-elminster",
                "entityType": "Character",
                "name": "Elminster Aumar",
                "description": "The Sage of Shadowdale",
                "parentLocalId": "region-dalelands",
                "tags": ["wizard", "chosen"]
            }
            """;

        // Act
        var entity = JsonSerializer.Deserialize<EntityImportDefinition>(json, CamelCaseOptions)!;

        // Assert
        entity.LocalId.Should().Be("npc-elminster");
        entity.EntityType.Should().Be("Character");
        entity.Name.Should().Be("Elminster Aumar");
        entity.Description.Should().Be("The Sage of Shadowdale");
        entity.ParentLocalId.Should().Be("region-dalelands");
        entity.Tags.Should().HaveCount(2);
    }
}
