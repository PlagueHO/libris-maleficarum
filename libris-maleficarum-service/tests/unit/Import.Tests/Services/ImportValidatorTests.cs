namespace LibrisMaleficarum.Import.Tests.Services;

using LibrisMaleficarum.Import.Models;
using LibrisMaleficarum.Import.Services;
using LibrisMaleficarum.Import.Validation;

[TestClass]
[TestCategory("Unit")]
public sealed class ImportValidatorTests
{
    private ImportValidator _validator = null!;

    [TestInitialize]
    public void Setup()
    {
        _validator = new ImportValidator();
    }

    [TestMethod]
    public void Validate_ValidContent_ReturnsValidResult()
    {
        // Arrange
        var content = CreateValidContent();

        // Act
        var result = _validator.Validate(content);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
        result.Manifest.Should().NotBeNull();
        result.Manifest!.World.Name.Should().Be("Test World");
        result.Manifest.Entities.Should().HaveCount(2);
        result.Manifest.TotalEntityCount.Should().Be(2);
    }

    [TestMethod]
    public void Validate_MissingWorld_ReturnsWorldMissingError()
    {
        // Arrange — explicitly construct content with null world
        // (the helper's ?? operator prevents null from passing through)
        var content = new ImportSourceContent
        {
            World = null,
            Entities = new List<EntityImportDefinition>
            {
                new() { LocalId = "root", EntityType = "Continent", Name = "Root" },
                new() { LocalId = "child", EntityType = "Country", Name = "Child", ParentLocalId = "root" }
            },
            ParseErrors = [],
            SourcePath = "/test",
            SourceType = ImportSourceType.Folder
        };

        // Act
        var result = _validator.Validate(content);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.Code == ImportValidationErrorCodes.WorldMissing);
    }

    [TestMethod]
    public void Validate_NullWorldWithParseError_DoesNotAddDuplicateWorldMissingError()
    {
        // Arrange
        var content = new ImportSourceContent
        {
            World = null,
            Entities = [],
            ParseErrors =
            [
                new ImportValidationError
                {
                    FilePath = "world.json",
                    Code = ImportValidationErrorCodes.WorldInvalidJson,
                    Message = "The world.json file contains invalid JSON."
                }
            ],
            SourcePath = "/test",
            SourceType = ImportSourceType.Folder
        };

        // Act
        var result = _validator.Validate(content);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.Code == ImportValidationErrorCodes.WorldInvalidJson);
        result.Errors.Should().NotContain(e => e.Code == ImportValidationErrorCodes.WorldMissing);
    }

    [TestMethod]
    public void Validate_EntityMissingLocalId_ReturnsError()
    {
        // Arrange
        var entities = new List<EntityImportDefinition>
        {
            new() { LocalId = "", EntityType = "Continent", Name = "No Id" }
        };
        var content = CreateValidContent(entities: entities);

        // Act
        var result = _validator.Validate(content);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Code == ImportValidationErrorCodes.EntityMissingLocalId);
    }

    [TestMethod]
    public void Validate_DuplicateLocalId_ReturnsError()
    {
        // Arrange
        var entities = new List<EntityImportDefinition>
        {
            new() { LocalId = "dup", EntityType = "Continent", Name = "First" },
            new() { LocalId = "dup", EntityType = "Country", Name = "Second" }
        };
        var content = CreateValidContent(entities: entities);

        // Act
        var result = _validator.Validate(content);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Code == ImportValidationErrorCodes.EntityDuplicateLocalId);
    }

    [TestMethod]
    public void Validate_EntityMissingName_ReturnsError()
    {
        // Arrange
        var entities = new List<EntityImportDefinition>
        {
            new() { LocalId = "e1", EntityType = "Continent", Name = "" }
        };
        var content = CreateValidContent(entities: entities);

        // Act
        var result = _validator.Validate(content);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Code == ImportValidationErrorCodes.EntityMissingName);
    }

    [TestMethod]
    public void Validate_EntityMissingType_ReturnsError()
    {
        // Arrange
        var entities = new List<EntityImportDefinition>
        {
            new() { LocalId = "e1", EntityType = "", Name = "Valid Name" }
        };
        var content = CreateValidContent(entities: entities);

        // Act
        var result = _validator.Validate(content);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Code == ImportValidationErrorCodes.EntityMissingType);
    }

    [TestMethod]
    public void Validate_EntityInvalidType_ReturnsError()
    {
        // Arrange
        var entities = new List<EntityImportDefinition>
        {
            new() { LocalId = "e1", EntityType = "InvalidType", Name = "Bad Type Entity" }
        };
        var content = CreateValidContent(entities: entities);

        // Act
        var result = _validator.Validate(content);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Code == ImportValidationErrorCodes.EntityInvalidType);
    }

    [TestMethod]
    public void Validate_DanglingParentLocalId_ReturnsError()
    {
        // Arrange
        var entities = new List<EntityImportDefinition>
        {
            new() { LocalId = "orphan", EntityType = "Country", Name = "Orphan", ParentLocalId = "nonexistent" }
        };
        var content = CreateValidContent(entities: entities);

        // Act
        var result = _validator.Validate(content);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Code == ImportValidationErrorCodes.EntityDanglingParent);
    }

    [TestMethod]
    public void Validate_CycleDetected_ReturnsError()
    {
        // Arrange — A→B→A cycle
        var entities = new List<EntityImportDefinition>
        {
            new() { LocalId = "a", EntityType = "Continent", Name = "A", ParentLocalId = "b" },
            new() { LocalId = "b", EntityType = "Country", Name = "B", ParentLocalId = "a" }
        };
        var content = CreateValidContent(entities: entities);

        // Act
        var result = _validator.Validate(content);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Code == ImportValidationErrorCodes.EntityCycleDetected);
    }

    [TestMethod]
    public void Validate_NameTooLong_ReturnsError()
    {
        // Arrange — 201 characters exceeds the 200-char limit
        var entities = new List<EntityImportDefinition>
        {
            new() { LocalId = "e1", EntityType = "Continent", Name = new string('A', 201) }
        };
        var content = CreateValidContent(entities: entities);

        // Act
        var result = _validator.Validate(content);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Code == ImportValidationErrorCodes.EntityNameTooLong);
    }

    [TestMethod]
    public void Validate_DescriptionTooLong_ReturnsError()
    {
        // Arrange — 5001 characters exceeds the 5000-char limit
        var entities = new List<EntityImportDefinition>
        {
            new()
            {
                LocalId = "e1",
                EntityType = "Continent",
                Name = "Valid",
                Description = new string('D', 5001)
            }
        };
        var content = CreateValidContent(entities: entities);

        // Act
        var result = _validator.Validate(content);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Code == ImportValidationErrorCodes.EntityDescTooLong);
    }

    [TestMethod]
    public void Validate_TooManyTags_ReturnsError()
    {
        // Arrange — 21 tags exceeds the 20-tag limit
        var tags = Enumerable.Range(1, 21).Select(i => $"tag{i}").ToList();
        var entities = new List<EntityImportDefinition>
        {
            new() { LocalId = "e1", EntityType = "Continent", Name = "Tagged", Tags = tags }
        };
        var content = CreateValidContent(entities: entities);

        // Act
        var result = _validator.Validate(content);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Code == ImportValidationErrorCodes.EntityTooManyTags);
    }

    [TestMethod]
    public void Validate_TagTooLong_ReturnsError()
    {
        // Arrange — 51-char tag exceeds the 50-char limit
        var entities = new List<EntityImportDefinition>
        {
            new()
            {
                LocalId = "e1",
                EntityType = "Continent",
                Name = "Tagged",
                Tags = [new string('T', 51)]
            }
        };
        var content = CreateValidContent(entities: entities);

        // Act
        var result = _validator.Validate(content);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Code == ImportValidationErrorCodes.EntityTagTooLong);
    }

    [TestMethod]
    public void Validate_PropertiesTooLarge_ReturnsError()
    {
        // Arrange — properties exceeding 100KB
        var largeValue = new string('X', 102_400);
        var properties = new Dictionary<string, object> { ["bigField"] = largeValue };
        var entities = new List<EntityImportDefinition>
        {
            new()
            {
                LocalId = "e1",
                EntityType = "Continent",
                Name = "Heavy",
                Properties = properties
            }
        };
        var content = CreateValidContent(entities: entities);

        // Act
        var result = _validator.Validate(content);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Code == ImportValidationErrorCodes.EntityPropsTooLarge);
    }

    [TestMethod]
    public void Validate_BuildsCorrectEntitiesByDepth()
    {
        // Arrange — root → child → grandchild hierarchy
        var entities = new List<EntityImportDefinition>
        {
            new() { LocalId = "root", EntityType = "Continent", Name = "Root" },
            new() { LocalId = "child", EntityType = "Country", Name = "Child", ParentLocalId = "root" },
            new() { LocalId = "grandchild", EntityType = "Region", Name = "Grandchild", ParentLocalId = "child" }
        };
        var content = CreateValidContent(entities: entities);

        // Act
        var result = _validator.Validate(content);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Manifest.Should().NotBeNull();
        result.Manifest!.EntitiesByDepth.Should().ContainKey(0);
        result.Manifest.EntitiesByDepth.Should().ContainKey(1);
        result.Manifest.EntitiesByDepth.Should().ContainKey(2);
        result.Manifest.EntitiesByDepth[0].Should().ContainSingle(e => e.Definition.LocalId == "root");
        result.Manifest.EntitiesByDepth[1].Should().ContainSingle(e => e.Definition.LocalId == "child");
        result.Manifest.EntitiesByDepth[2].Should().ContainSingle(e => e.Definition.LocalId == "grandchild");
        result.Manifest.MaxDepth.Should().Be(2);
    }

    [TestMethod]
    public void Validate_AssignsCorrectDepthAndPath()
    {
        // Arrange
        var entities = new List<EntityImportDefinition>
        {
            new() { LocalId = "root", EntityType = "Continent", Name = "Root" },
            new() { LocalId = "child", EntityType = "Country", Name = "Child", ParentLocalId = "root" },
            new() { LocalId = "grandchild", EntityType = "Region", Name = "Grandchild", ParentLocalId = "child" }
        };
        var content = CreateValidContent(entities: entities);

        // Act
        var result = _validator.Validate(content);

        // Assert
        result.IsValid.Should().BeTrue();
        var manifest = result.Manifest!;

        var rootResolved = manifest.Entities.Single(e => e.Definition.LocalId == "root");
        rootResolved.Depth.Should().Be(0);
        rootResolved.Path.Should().BeEmpty();
        rootResolved.ResolvedParentId.Should().BeNull();

        var childResolved = manifest.Entities.Single(e => e.Definition.LocalId == "child");
        childResolved.Depth.Should().Be(1);
        childResolved.Path.Should().HaveCount(1);
        childResolved.Path[0].Should().Be(rootResolved.AssignedId);
        childResolved.ResolvedParentId.Should().Be(rootResolved.AssignedId);

        var grandchildResolved = manifest.Entities.Single(e => e.Definition.LocalId == "grandchild");
        grandchildResolved.Depth.Should().Be(2);
        grandchildResolved.Path.Should().HaveCount(2);
        grandchildResolved.Path[0].Should().Be(rootResolved.AssignedId);
        grandchildResolved.Path[1].Should().Be(childResolved.AssignedId);
    }

    private static ImportSourceContent CreateValidContent(
        WorldImportDefinition? world = null,
        IReadOnlyList<EntityImportDefinition>? entities = null)
    {
        return new ImportSourceContent
        {
            World = world ?? new WorldImportDefinition { Name = "Test World" },
            Entities = entities ?? new List<EntityImportDefinition>
            {
                new() { LocalId = "root", EntityType = "Continent", Name = "Root" },
                new() { LocalId = "child", EntityType = "Country", Name = "Child", ParentLocalId = "root" }
            },
            ParseErrors = [],
            SourcePath = "/test",
            SourceType = ImportSourceType.Folder
        };
    }
}
