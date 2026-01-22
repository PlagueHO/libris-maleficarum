namespace LibrisMaleficarum.Api.Tests.Validators;

using FluentAssertions;
using LibrisMaleficarum.Api.Validators;
using LibrisMaleficarum.Domain.Configuration;
using LibrisMaleficarum.Domain.Exceptions;
using LibrisMaleficarum.Domain.ValueObjects;
using Microsoft.Extensions.Options;

/// <summary>
/// Unit tests for SchemaVersionValidator.
/// Tests all 4 validation error codes:
/// - SCHEMA_VERSION_INVALID
/// - SCHEMA_VERSION_TOO_LOW
/// - SCHEMA_VERSION_TOO_HIGH
/// - SCHEMA_DOWNGRADE_NOT_ALLOWED
/// </summary>
[TestClass]
[TestCategory("Unit")]
public class SchemaVersionValidatorTests
{
    private SchemaVersionValidator _validator = null!;
    private EntitySchemaVersionConfig _config = null!;

    [TestInitialize]
    public void Setup()
    {
        // Configure test schema version ranges: all entity types support version 1-3
        _config = new EntitySchemaVersionConfig
        {
            EntityTypes = new Dictionary<string, SchemaVersionRange>
            {
                ["Character"] = new SchemaVersionRange { MinVersion = 1, MaxVersion = 3 },
                ["Location"] = new SchemaVersionRange { MinVersion = 1, MaxVersion = 3 },
                ["Item"] = new SchemaVersionRange { MinVersion = 1, MaxVersion = 3 },
                ["Faction"] = new SchemaVersionRange { MinVersion = 1, MaxVersion = 3 }
            }
        };

        var options = Options.Create(_config);
        _validator = new SchemaVersionValidator(options);
    }

    #region ValidateCreate Tests

    // T028 [US1] TEST: ValidateCreate() - reject SchemaVersion < 1 (SCHEMA_VERSION_INVALID)
    [TestMethod]
    public void ValidateCreate_WithSchemaVersionLessThanOne_ThrowsSchemaVersionException()
    {
        // Arrange
        var entityType = EntityType.Character.ToString();
        var invalidSchemaVersion = 0;

        // Act
        var act = () => _validator.ValidateCreate(entityType, invalidSchemaVersion);

        // Assert
        act.Should().Throw<SchemaVersionException>()
            .WithMessage("*positive integer*")
            .Where(ex => ex.ErrorCode == "SCHEMA_VERSION_INVALID");
    }

    // T028 [US1] TEST: ValidateCreate() - reject negative SchemaVersion (SCHEMA_VERSION_INVALID)
    [TestMethod]
    public void ValidateCreate_WithNegativeSchemaVersion_ThrowsSchemaVersionException()
    {
        // Arrange
        var entityType = EntityType.Location.ToString();
        var invalidSchemaVersion = -1;

        // Act
        var act = () => _validator.ValidateCreate(entityType, invalidSchemaVersion);

        // Assert
        act.Should().Throw<SchemaVersionException>()
            .WithMessage("*positive integer*")
            .Where(ex => ex.ErrorCode == "SCHEMA_VERSION_INVALID");
    }

    // T029 [US1] TEST: ValidateCreate() - reject SchemaVersion < min supported (SCHEMA_VERSION_TOO_LOW)
    [TestMethod]
    public void ValidateCreate_WithSchemaVersionBelowMinimum_ThrowsSchemaVersionException()
    {
        // Arrange
        var entityType = EntityType.Item.ToString();
        var tooLowSchemaVersion = 0; // Min is 1

        // Act
        var act = () => _validator.ValidateCreate(entityType, tooLowSchemaVersion);

        // Assert
        act.Should().Throw<SchemaVersionException>()
            .Where(ex => ex.ErrorCode == "SCHEMA_VERSION_TOO_LOW" ||
                        ex.ErrorCode == "SCHEMA_VERSION_INVALID");
    }

    // T030 [US1] TEST: ValidateCreate() - reject SchemaVersion > max supported (SCHEMA_VERSION_TOO_HIGH)
    [TestMethod]
    public void ValidateCreate_WithSchemaVersionAboveMaximum_ThrowsSchemaVersionException()
    {
        // Arrange
        var entityType = EntityType.Faction.ToString();
        var tooHighSchemaVersion = 99; // Max is 3

        // Act
        var act = () => _validator.ValidateCreate(entityType, tooHighSchemaVersion);

        // Assert
        act.Should().Throw<SchemaVersionException>()
            .WithMessage("*exceeds maximum*")
            .Where(ex => ex.ErrorCode == "SCHEMA_VERSION_TOO_HIGH");
    }

    [TestMethod]
    public void ValidateCreate_WithValidSchemaVersion_DoesNotThrow()
    {
        // Arrange
        var entityType = EntityType.Character.ToString();
        var validSchemaVersion = 2;

        // Act
        var act = () => _validator.ValidateCreate(entityType, validSchemaVersion);

        // Assert
        act.Should().NotThrow();
    }

    [TestMethod]
    public void ValidateCreate_WithSchemaVersionAtMinimumBoundary_DoesNotThrow()
    {
        // Arrange
        var entityType = EntityType.Location.ToString();
        var minSchemaVersion = 1;

        // Act
        var act = () => _validator.ValidateCreate(entityType, minSchemaVersion);

        // Assert
        act.Should().NotThrow();
    }

    [TestMethod]
    public void ValidateCreate_WithSchemaVersionAtMaximumBoundary_DoesNotThrow()
    {
        // Arrange
        var entityType = EntityType.Item.ToString();
        var maxSchemaVersion = 3;

        // Act
        var act = () => _validator.ValidateCreate(entityType, maxSchemaVersion);

        // Assert
        act.Should().NotThrow();
    }

    #endregion

    #region ValidateUpdate Tests

    // T031 [US1] TEST: ValidateUpdate() - reject downgrade attempt (SCHEMA_DOWNGRADE_NOT_ALLOWED)
    [TestMethod]
    public void ValidateUpdate_WithSchemaDowngrade_ThrowsSchemaVersionException()
    {
        // Arrange
        var entityType = EntityType.Character.ToString();
        var currentSchemaVersion = 3;
        var newSchemaVersion = 2; // Downgrade from 3 to 2

        // Act
        var act = () => _validator.ValidateUpdate(entityType, currentSchemaVersion, newSchemaVersion);

        // Assert
        act.Should().Throw<SchemaVersionException>()
            .WithMessage("*downgrade*")
            .Where(ex => ex.ErrorCode == "SCHEMA_DOWNGRADE_NOT_ALLOWED");
    }

    // T031 [US1] TEST: ValidateUpdate() - reject downgrade to version 1 (SCHEMA_DOWNGRADE_NOT_ALLOWED)
    [TestMethod]
    public void ValidateUpdate_WithDowngradeToVersionOne_ThrowsSchemaVersionException()
    {
        // Arrange
        var entityType = EntityType.Location.ToString();
        var currentSchemaVersion = 2;
        var newSchemaVersion = 1; // Downgrade from 2 to 1

        // Act
        var act = () => _validator.ValidateUpdate(entityType, currentSchemaVersion, newSchemaVersion);

        // Assert
        act.Should().Throw<SchemaVersionException>()
            .WithMessage("*downgrade*")
            .Where(ex => ex.ErrorCode == "SCHEMA_DOWNGRADE_NOT_ALLOWED");
    }

    [TestMethod]
    public void ValidateUpdate_WithSchemaVersionAboveMaximum_ThrowsSchemaVersionException()
    {
        // Arrange
        var entityType = EntityType.Item.ToString();
        var currentSchemaVersion = 2;
        var tooHighSchemaVersion = 99; // Max is 3

        // Act
        var act = () => _validator.ValidateUpdate(entityType, currentSchemaVersion, tooHighSchemaVersion);

        // Assert
        act.Should().Throw<SchemaVersionException>()
            .WithMessage("*exceeds maximum*")
            .Where(ex => ex.ErrorCode == "SCHEMA_VERSION_TOO_HIGH");
    }

    [TestMethod]
    public void ValidateUpdate_WithValidUpgrade_DoesNotThrow()
    {
        // Arrange
        var entityType = EntityType.Faction.ToString();
        var currentSchemaVersion = 1;
        var newSchemaVersion = 2; // Upgrade from 1 to 2

        // Act
        var act = () => _validator.ValidateUpdate(entityType, currentSchemaVersion, newSchemaVersion);

        // Assert
        act.Should().NotThrow();
    }

    [TestMethod]
    public void ValidateUpdate_WithSameSchemaVersion_DoesNotThrow()
    {
        // Arrange
        var entityType = EntityType.Character.ToString();
        var schemaVersion = 2;

        // Act
        var act = () => _validator.ValidateUpdate(entityType, schemaVersion, schemaVersion);

        // Assert
        act.Should().NotThrow();
    }

    #endregion
}
