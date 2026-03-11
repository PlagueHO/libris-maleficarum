namespace LibrisMaleficarum.Import.Tests.Services;

using System.IO.Compression;
using System.Text.Json;
using LibrisMaleficarum.Import.Models;
using LibrisMaleficarum.Import.Services;
using LibrisMaleficarum.Import.Validation;

[TestClass]
[TestCategory("Unit")]
public sealed class ImportSourceReaderTests
{
    private string _tempFolder = null!;
    private ImportSourceReader _reader = null!;

    private static readonly JsonSerializerOptions CamelCaseOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    [TestInitialize]
    public void Setup()
    {
        _tempFolder = Path.Combine(Path.GetTempPath(), $"ImportSourceReaderTests_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempFolder);
        _reader = new ImportSourceReader();
    }

    [TestCleanup]
    public void Cleanup()
    {
        if (Directory.Exists(_tempFolder))
        {
            Directory.Delete(_tempFolder, recursive: true);
        }
    }

    [TestMethod]
    public async Task ReadAsync_ValidFolder_ReturnsWorldAndEntities()
    {
        // Arrange
        var world = new WorldImportDefinition { Name = "Forgotten Realms", Description = "A classic D&D setting" };
        await WriteJsonAsync(Path.Combine(_tempFolder, "world.json"), world);

        var entity = new EntityImportDefinition
        {
            LocalId = "continent-faerun",
            EntityType = "Continent",
            Name = "Faerûn",
            Description = "The main continent"
        };
        await WriteJsonAsync(Path.Combine(_tempFolder, "continent-faerun.json"), entity);

        // Act
        var result = await _reader.ReadAsync(_tempFolder);

        // Assert
        result.World.Should().NotBeNull();
        result.World!.Name.Should().Be("Forgotten Realms");
        result.World.Description.Should().Be("A classic D&D setting");
        result.Entities.Should().HaveCount(1);
        result.Entities[0].LocalId.Should().Be("continent-faerun");
        result.Entities[0].Name.Should().Be("Faerûn");
        result.ParseErrors.Should().BeEmpty();
    }

    [TestMethod]
    public async Task ReadAsync_FolderDetection_SetsSourceTypeToFolder()
    {
        // Arrange
        var world = new WorldImportDefinition { Name = "Test World" };
        await WriteJsonAsync(Path.Combine(_tempFolder, "world.json"), world);

        // Act
        var result = await _reader.ReadAsync(_tempFolder);

        // Assert
        result.SourceType.Should().Be(ImportSourceType.Folder);
        result.SourcePath.Should().Be(_tempFolder);
    }

    [TestMethod]
    public async Task ReadAsync_IgnoresNonJsonFiles()
    {
        // Arrange
        var world = new WorldImportDefinition { Name = "Test World" };
        await WriteJsonAsync(Path.Combine(_tempFolder, "world.json"), world);
        await File.WriteAllTextAsync(Path.Combine(_tempFolder, "readme.txt"), "This is a text file.");
        await File.WriteAllTextAsync(Path.Combine(_tempFolder, "notes.md"), "# Notes");

        var entity = new EntityImportDefinition
        {
            LocalId = "e1",
            EntityType = "Continent",
            Name = "Only Entity"
        };
        await WriteJsonAsync(Path.Combine(_tempFolder, "entity.json"), entity);

        // Act
        var result = await _reader.ReadAsync(_tempFolder);

        // Assert
        result.Entities.Should().HaveCount(1);
        result.Entities[0].LocalId.Should().Be("e1");
    }

    [TestMethod]
    public async Task ReadAsync_MissingWorldJson_ReturnsParseError()
    {
        // Arrange — folder with entity files but no world.json
        var entity = new EntityImportDefinition
        {
            LocalId = "e1",
            EntityType = "Continent",
            Name = "Orphan"
        };
        await WriteJsonAsync(Path.Combine(_tempFolder, "entity.json"), entity);

        // Act
        var result = await _reader.ReadAsync(_tempFolder);

        // Assert
        result.World.Should().BeNull();
        result.ParseErrors.Should().ContainSingle(e => e.Code == ImportValidationErrorCodes.WorldMissing);
    }

    [TestMethod]
    public async Task ReadAsync_MalformedEntityJson_ReturnsParseError()
    {
        // Arrange
        var world = new WorldImportDefinition { Name = "Test World" };
        await WriteJsonAsync(Path.Combine(_tempFolder, "world.json"), world);
        await File.WriteAllTextAsync(Path.Combine(_tempFolder, "broken.json"), "{ not valid json }}}");

        // Act
        var result = await _reader.ReadAsync(_tempFolder);

        // Assert
        result.ParseErrors.Should().Contain(e => e.Code == ImportValidationErrorCodes.EntityInvalidJson);
    }

    [TestMethod]
    public async Task ReadAsync_NestedSubfolders_ReadsAllEntities()
    {
        // Arrange
        var world = new WorldImportDefinition { Name = "Nested World" };
        await WriteJsonAsync(Path.Combine(_tempFolder, "world.json"), world);

        var rootEntity = new EntityImportDefinition
        {
            LocalId = "root",
            EntityType = "Continent",
            Name = "Root"
        };
        await WriteJsonAsync(Path.Combine(_tempFolder, "root.json"), rootEntity);

        var level1Entity = new EntityImportDefinition
        {
            LocalId = "level1",
            EntityType = "Country",
            Name = "Level 1",
            ParentLocalId = "root"
        };
        await WriteJsonAsync(Path.Combine(_tempFolder, "sub1", "level1.json"), level1Entity);

        var level2Entity = new EntityImportDefinition
        {
            LocalId = "level2",
            EntityType = "Region",
            Name = "Level 2",
            ParentLocalId = "level1"
        };
        await WriteJsonAsync(Path.Combine(_tempFolder, "sub1", "sub2", "level2.json"), level2Entity);

        // Act
        var result = await _reader.ReadAsync(_tempFolder);

        // Assert
        result.Entities.Should().HaveCount(3);
        result.Entities.Select(e => e.LocalId).Should().Contain(["root", "level1", "level2"]);
    }

    [TestMethod]
    public async Task ReadAsync_SetsSourceFilePathOnEntities()
    {
        // Arrange
        var world = new WorldImportDefinition { Name = "Test World" };
        await WriteJsonAsync(Path.Combine(_tempFolder, "world.json"), world);

        var entity = new EntityImportDefinition
        {
            LocalId = "e1",
            EntityType = "Continent",
            Name = "Entity One"
        };
        var entityPath = Path.Combine(_tempFolder, "entities", "e1.json");
        await WriteJsonAsync(entityPath, entity);

        // Act
        var result = await _reader.ReadAsync(_tempFolder);

        // Assert
        result.Entities.Should().HaveCount(1);
        result.Entities[0].SourceFilePath.Should().NotBeNullOrEmpty();
        result.Entities[0].SourceFilePath.Should().Contain("e1.json");
    }

    private static async Task WriteJsonAsync<T>(string path, T obj)
    {
        var dir = Path.GetDirectoryName(path)!;
        Directory.CreateDirectory(dir);
        var json = JsonSerializer.Serialize(obj, CamelCaseOptions);
        await File.WriteAllTextAsync(path, json);
    }

    private string CreateTestZip(Dictionary<string, string> entries)
    {
        var zipPath = Path.Combine(_tempFolder, "test.zip");
        using var archive = ZipFile.Open(zipPath, ZipArchiveMode.Create);
        foreach (var (entryName, content) in entries)
        {
            var entry = archive.CreateEntry(entryName);
            using var writer = new StreamWriter(entry.Open());
            writer.Write(content);
        }
        return zipPath;
    }

    // ── ZIP archive tests ───────────────────────────────────────────────

    [TestMethod]
    public async Task ReadAsync_ValidZip_ReturnsWorldAndEntities()
    {
        // Arrange
        var world = new WorldImportDefinition { Name = "Forgotten Realms", Description = "A classic D&D setting" };
        var entity = new EntityImportDefinition
        {
            LocalId = "continent-faerun",
            EntityType = "Continent",
            Name = "Faerûn",
            Description = "The main continent"
        };
        var zipPath = CreateTestZip(new Dictionary<string, string>
        {
            ["world.json"] = JsonSerializer.Serialize(world, CamelCaseOptions),
            ["continent-faerun.json"] = JsonSerializer.Serialize(entity, CamelCaseOptions)
        });

        // Act
        var result = await _reader.ReadAsync(zipPath);

        // Assert
        result.World.Should().NotBeNull();
        result.World!.Name.Should().Be("Forgotten Realms");
        result.World.Description.Should().Be("A classic D&D setting");
        result.Entities.Should().HaveCount(1);
        result.Entities[0].LocalId.Should().Be("continent-faerun");
        result.Entities[0].Name.Should().Be("Faerûn");
        result.ParseErrors.Should().BeEmpty();
    }

    [TestMethod]
    public async Task ReadAsync_ZipDetection_SetsSourceTypeToZipArchive()
    {
        // Arrange
        var world = new WorldImportDefinition { Name = "Test World" };
        var zipPath = CreateTestZip(new Dictionary<string, string>
        {
            ["world.json"] = JsonSerializer.Serialize(world, CamelCaseOptions)
        });

        // Act
        var result = await _reader.ReadAsync(zipPath);

        // Assert
        result.SourceType.Should().Be(ImportSourceType.ZipArchive);
        result.SourcePath.Should().Be(zipPath);
    }

    [TestMethod]
    public async Task ReadAsync_ZipIgnoresNonJsonEntries()
    {
        // Arrange
        var world = new WorldImportDefinition { Name = "Test World" };
        var entity = new EntityImportDefinition
        {
            LocalId = "e1",
            EntityType = "Continent",
            Name = "Only Entity"
        };
        var zipPath = CreateTestZip(new Dictionary<string, string>
        {
            ["world.json"] = JsonSerializer.Serialize(world, CamelCaseOptions),
            ["entity.json"] = JsonSerializer.Serialize(entity, CamelCaseOptions),
            ["readme.txt"] = "This is a text file.",
            ["notes.md"] = "# Notes"
        });

        // Act
        var result = await _reader.ReadAsync(zipPath);

        // Assert
        result.Entities.Should().HaveCount(1);
        result.Entities[0].LocalId.Should().Be("e1");
    }

    [TestMethod]
    public async Task ReadAsync_ZipMissingWorldJson_ReturnsParseError()
    {
        // Arrange — zip with entity files but no world.json
        var entity = new EntityImportDefinition
        {
            LocalId = "e1",
            EntityType = "Continent",
            Name = "Orphan"
        };
        var zipPath = CreateTestZip(new Dictionary<string, string>
        {
            ["entity.json"] = JsonSerializer.Serialize(entity, CamelCaseOptions)
        });

        // Act
        var result = await _reader.ReadAsync(zipPath);

        // Assert
        result.World.Should().BeNull();
        result.ParseErrors.Should().ContainSingle(e => e.Code == ImportValidationErrorCodes.WorldMissing);
    }

    [TestMethod]
    public async Task ReadAsync_CorruptedZip_ReturnsZipInvalidError()
    {
        // Arrange — write garbage data to a .zip file
        var zipPath = Path.Combine(_tempFolder, "corrupted.zip");
        await File.WriteAllTextAsync(zipPath, "this is not a zip file");

        // Act
        var result = await _reader.ReadAsync(zipPath);

        // Assert
        result.ParseErrors.Should().ContainSingle(e => e.Code == ImportValidationErrorCodes.ZipInvalid);
        result.World.Should().BeNull();
        result.Entities.Should().BeEmpty();
    }

    [TestMethod]
    public async Task ReadAsync_ZipSlipDetected_ReturnsError()
    {
        // Arrange — zip entry with path traversal
        var world = new WorldImportDefinition { Name = "Test World" };
        var entity = new EntityImportDefinition
        {
            LocalId = "malicious",
            EntityType = "Continent",
            Name = "Malicious"
        };
        var zipPath = CreateTestZip(new Dictionary<string, string>
        {
            ["world.json"] = JsonSerializer.Serialize(world, CamelCaseOptions),
            ["../malicious.json"] = JsonSerializer.Serialize(entity, CamelCaseOptions)
        });

        // Act
        var result = await _reader.ReadAsync(zipPath);

        // Assert
        result.ParseErrors.Should().Contain(e => e.Code == ImportValidationErrorCodes.ZipSlipDetected);
    }

    [TestMethod]
    public async Task ReadAsync_ZipNestedFolders_ReadsAllEntities()
    {
        // Arrange
        var world = new WorldImportDefinition { Name = "Nested World" };
        var rootEntity = new EntityImportDefinition { LocalId = "root", EntityType = "Continent", Name = "Root" };
        var level1Entity = new EntityImportDefinition { LocalId = "level1", EntityType = "Country", Name = "Level 1", ParentLocalId = "root" };
        var level2Entity = new EntityImportDefinition { LocalId = "level2", EntityType = "Region", Name = "Level 2", ParentLocalId = "level1" };
        var zipPath = CreateTestZip(new Dictionary<string, string>
        {
            ["world.json"] = JsonSerializer.Serialize(world, CamelCaseOptions),
            ["root.json"] = JsonSerializer.Serialize(rootEntity, CamelCaseOptions),
            ["sub1/level1.json"] = JsonSerializer.Serialize(level1Entity, CamelCaseOptions),
            ["sub1/sub2/level2.json"] = JsonSerializer.Serialize(level2Entity, CamelCaseOptions)
        });

        // Act
        var result = await _reader.ReadAsync(zipPath);

        // Assert
        result.Entities.Should().HaveCount(3);
        result.Entities.Select(e => e.LocalId).Should().Contain(["root", "level1", "level2"]);
    }
}
