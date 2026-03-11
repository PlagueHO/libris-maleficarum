namespace LibrisMaleficarum.Import.Tests.Services;

using System.IO.Compression;
using System.Text.Json;
using LibrisMaleficarum.Api.Client;
using LibrisMaleficarum.Import.Services;
using LibrisMaleficarum.Import.Validation;

[TestClass]
[TestCategory("Unit")]
public sealed class WorldImportServiceValidateTests
{
    private WorldImportService _service = null!;
    private ILibrisApiClient _apiClient = null!;
    private string _tempDir = null!;

    [TestInitialize]
    public void Setup()
    {
        _apiClient = Substitute.For<ILibrisApiClient>();
        var reader = new ImportSourceReader();
        var validator = new ImportValidator();
        _service = new WorldImportService(reader, validator, _apiClient);
        _tempDir = Path.Combine(Path.GetTempPath(), $"import-validate-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
    }

    [TestCleanup]
    public void Cleanup()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, true);
    }

    [TestMethod]
    public async Task ValidateAsync_ValidFolder_ReturnsIsValidWithManifest()
    {
        // Arrange
        WriteWorldJson("Test World", "A fantasy realm");
        WriteEntityJson("continent.json", "c1", "Continent", "Eldoria");
        WriteEntityJson("country.json", "co1", "Country", "Valdris", parentLocalId: "c1");
        WriteEntityJson("city.json", "ci1", "City", "Thornhaven", parentLocalId: "co1");

        // Act
        var result = await _service.ValidateAsync(_tempDir);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Manifest.Should().NotBeNull();
        result.Manifest!.TotalEntityCount.Should().Be(3);
        result.Manifest.World.Name.Should().Be("Test World");
        result.Manifest.CountsByType.Should().ContainKey("Continent")
            .WhoseValue.Should().Be(1);
        result.Manifest.CountsByType.Should().ContainKey("Country")
            .WhoseValue.Should().Be(1);
        result.Manifest.CountsByType.Should().ContainKey("City")
            .WhoseValue.Should().Be(1);
    }

    [TestMethod]
    public async Task ValidateAsync_MixedValidAndInvalid_ReportsAllErrors()
    {
        // Arrange — one valid entity, plus entities with different validation failures
        WriteWorldJson("Mixed World");
        WriteEntityJson("valid.json", "e1", "Continent", "Valid Entity");
        WriteRawJson("no-name.json",
            """{"localId": "e2", "entityType": "City", "name": ""}""");
        WriteRawJson("bad-type.json",
            """{"localId": "e3", "entityType": "InvalidType", "name": "Bad Type"}""");
        WriteEntityJson("duplicate.json", "e1", "Country", "Duplicate Id");

        // Act
        var result = await _service.ValidateAsync(_tempDir);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Code == ImportValidationErrorCodes.EntityMissingName);
        result.Errors.Should().Contain(e => e.Code == ImportValidationErrorCodes.EntityInvalidType);
        result.Errors.Should().Contain(e => e.Code == ImportValidationErrorCodes.EntityDuplicateLocalId);
    }

    [TestMethod]
    public async Task ValidateAsync_ChecksJsonStructuralValidity()
    {
        // Arrange
        WriteWorldJson("JSON Test World");
        WriteRawJson("malformed.json", "{not valid json at all!!!");

        // Act
        var result = await _service.ValidateAsync(_tempDir);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Code == ImportValidationErrorCodes.EntityInvalidJson);
    }

    [TestMethod]
    public async Task ValidateAsync_ChecksRequiredFields()
    {
        // Arrange — entity with empty name
        WriteWorldJson("Required Fields World");
        WriteRawJson("no-name.json",
            """{"localId": "e1", "entityType": "City", "name": ""}""");

        // Act
        var result = await _service.ValidateAsync(_tempDir);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Code == ImportValidationErrorCodes.EntityMissingName);
    }

    [TestMethod]
    public async Task ValidateAsync_ChecksValidEntityTypes()
    {
        // Arrange
        WriteWorldJson("Entity Type World");
        WriteRawJson("invalid-type.json",
            """{"localId": "e1", "entityType": "Dinosaur", "name": "Bad Type Entity"}""");

        // Act
        var result = await _service.ValidateAsync(_tempDir);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Code == ImportValidationErrorCodes.EntityInvalidType);
    }

    [TestMethod]
    public async Task ValidateAsync_ChecksLocalIdUniqueness()
    {
        // Arrange — two entities with the same localId
        WriteWorldJson("Unique Ids World");
        WriteEntityJson("entity1.json", "dup-id", "Continent", "First");
        WriteEntityJson("entity2.json", "dup-id", "Country", "Second");

        // Act
        var result = await _service.ValidateAsync(_tempDir);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Code == ImportValidationErrorCodes.EntityDuplicateLocalId);
    }

    [TestMethod]
    public async Task ValidateAsync_ChecksParentLocalIdReferences()
    {
        // Arrange — entity referencing a non-existent parent
        WriteWorldJson("Parent Ref World");
        WriteRawJson("orphan.json",
            """{"localId": "e1", "entityType": "City", "name": "Orphan City", "parentLocalId": "nonexistent"}""");

        // Act
        var result = await _service.ValidateAsync(_tempDir);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Code == ImportValidationErrorCodes.EntityDanglingParent);
    }

    [TestMethod]
    public async Task ValidateAsync_ChecksCycleFreeHierarchy()
    {
        // Arrange — A and B reference each other as parents, forming a cycle
        WriteWorldJson("Cycle World");
        WriteRawJson("a.json",
            """{"localId": "a", "entityType": "Continent", "name": "Node A", "parentLocalId": "b"}""");
        WriteRawJson("b.json",
            """{"localId": "b", "entityType": "Continent", "name": "Node B", "parentLocalId": "a"}""");

        // Act
        var result = await _service.ValidateAsync(_tempDir);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Code == ImportValidationErrorCodes.EntityCycleDetected);
    }

    [TestMethod]
    public async Task ValidateAsync_NeverCallsApiClient()
    {
        // Arrange
        WriteWorldJson("API Check World");
        WriteEntityJson("entity.json", "e1", "Continent", "Some Continent");

        // Act
        await _service.ValidateAsync(_tempDir);

        // Assert — ValidateAsync should never touch the API client
        await _apiClient.DidNotReceiveWithAnyArgs().CreateWorldAsync(default!, default);
        await _apiClient.DidNotReceiveWithAnyArgs().CreateEntityAsync(default, default!, default);
    }

    [TestMethod]
    public async Task ValidateAsync_ZipSource_WorksIdentically()
    {
        // Arrange — build a folder, zip it, and validate the zip
        WriteWorldJson("Zip World", "A zipped realm");
        WriteEntityJson("continent.json", "z1", "Continent", "Zip Continent");
        WriteEntityJson("city.json", "z2", "City", "Zip City", parentLocalId: "z1");

        var zipPath = Path.Combine(Path.GetTempPath(), $"import-validate-{Guid.NewGuid():N}.zip");
        try
        {
            ZipFile.CreateFromDirectory(_tempDir, zipPath);

            // Act
            var result = await _service.ValidateAsync(zipPath);

            // Assert
            result.IsValid.Should().BeTrue();
            result.Manifest.Should().NotBeNull();
            result.Manifest!.TotalEntityCount.Should().Be(2);
            result.Manifest.World.Name.Should().Be("Zip World");
        }
        finally
        {
            if (File.Exists(zipPath))
                File.Delete(zipPath);
        }
    }

    #region Helper Methods

    private void WriteWorldJson(string name, string? description = null)
    {
        var obj = new Dictionary<string, string> { ["name"] = name };
        if (description is not null)
        {
            obj["description"] = description;
        }

        File.WriteAllText(
            Path.Combine(_tempDir, "world.json"),
            JsonSerializer.Serialize(obj));
    }

    private void WriteEntityJson(
        string fileName,
        string localId,
        string entityType,
        string name,
        string? parentLocalId = null)
    {
        var obj = new Dictionary<string, string>
        {
            ["localId"] = localId,
            ["entityType"] = entityType,
            ["name"] = name
        };

        if (parentLocalId is not null)
        {
            obj["parentLocalId"] = parentLocalId;
        }

        File.WriteAllText(
            Path.Combine(_tempDir, fileName),
            JsonSerializer.Serialize(obj));
    }

    private void WriteRawJson(string fileName, string content)
    {
        File.WriteAllText(Path.Combine(_tempDir, fileName), content);
    }

    #endregion
}
