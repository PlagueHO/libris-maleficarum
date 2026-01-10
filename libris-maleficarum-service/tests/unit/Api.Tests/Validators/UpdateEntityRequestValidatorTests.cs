namespace LibrisMaleficarum.Api.Tests.Validators;

using FluentAssertions;
using LibrisMaleficarum.Api.Models.Requests;
using LibrisMaleficarum.Api.Validators;
using LibrisMaleficarum.Domain.ValueObjects;
using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Tests for <see cref="UpdateEntityRequestValidator"/>.
/// </summary>
[TestClass]
[TestCategory("Unit")]
public class UpdateEntityRequestValidatorTests
{
    private UpdateEntityRequestValidator _validator = null!;

    /// <summary>
    /// Initializes the validator before each test.
    /// </summary>
    [TestInitialize]
    public void Setup()
    {
        _validator = new UpdateEntityRequestValidator();
    }

    /// <summary>
    /// Tests that a valid minimal request passes validation.
    /// </summary>
    [TestMethod]
    public async Task ValidateAsync_WithValidMinimalRequest_ReturnsNoErrors()
    {
        // Arrange
        var request = new UpdateEntityRequest
        {
            Name = "Updated Character",
            EntityType = EntityType.Character
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    /// <summary>
    /// Tests that a valid complete request passes validation.
    /// </summary>
    [TestMethod]
    public async Task ValidateAsync_WithValidCompleteRequest_ReturnsNoErrors()
    {
        // Arrange
        var request = new UpdateEntityRequest
        {
            Name = "Aragorn II",
            Description = "King of Gondor, Ranger of the North",
            EntityType = EntityType.Character,
            Tags = ["hero", "king", "ranger", "warrior"],
            Attributes = new Dictionary<string, object>
            {
                { "level", 25 },
                { "title", "King" },
                { "kingdom", "Gondor" }
            }
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    /// <summary>
    /// Tests that an empty name fails validation.
    /// </summary>
    [TestMethod]
    public async Task ValidateAsync_WithEmptyName_ReturnsError()
    {
        // Arrange
        var request = new UpdateEntityRequest
        {
            Name = string.Empty,
            EntityType = EntityType.Character
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().HaveCount(2);
        result.Errors.Should().Contain(e => e.PropertyName == "Name" && e.ErrorMessage == "Entity name is required.");
        result.Errors.Should().Contain(e => e.PropertyName == "Name" && e.ErrorMessage == "Entity name must be between 1 and 200 characters.");
    }

    /// <summary>
    /// Tests that a name exceeding maximum length fails validation.
    /// </summary>
    [TestMethod]
    public async Task ValidateAsync_WithNameTooLong_ReturnsError()
    {
        // Arrange
        var request = new UpdateEntityRequest
        {
            Name = new string('A', 201), // 201 characters
            EntityType = EntityType.Character
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle()
            .Which.PropertyName.Should().Be("Name");
        result.Errors.First().ErrorMessage.Should().Be("Entity name must be between 1 and 200 characters.");
    }

    /// <summary>
    /// Tests that a name at maximum length passes validation.
    /// </summary>
    [TestMethod]
    public async Task ValidateAsync_WithNameAtMaxLength_ReturnsNoErrors()
    {
        // Arrange
        var request = new UpdateEntityRequest
        {
            Name = new string('A', 200), // Exactly 200 characters
            EntityType = EntityType.Character
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    /// <summary>
    /// Tests that a description exceeding maximum length fails validation.
    /// </summary>
    [TestMethod]
    public async Task ValidateAsync_WithDescriptionTooLong_ReturnsError()
    {
        // Arrange
        var request = new UpdateEntityRequest
        {
            Name = "Test Entity",
            EntityType = EntityType.Character,
            Description = new string('A', 5001) // 5001 characters
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle()
            .Which.PropertyName.Should().Be("Description");
        result.Errors.First().ErrorMessage.Should().Be("Description must not exceed 5000 characters.");
    }

    /// <summary>
    /// Tests that a description at maximum length passes validation.
    /// </summary>
    [TestMethod]
    public async Task ValidateAsync_WithDescriptionAtMaxLength_ReturnsNoErrors()
    {
        // Arrange
        var request = new UpdateEntityRequest
        {
            Name = "Test Entity",
            EntityType = EntityType.Character,
            Description = new string('A', 5000) // Exactly 5000 characters
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    /// <summary>
    /// Tests that an invalid entity type fails validation.
    /// </summary>
    [TestMethod]
    public async Task ValidateAsync_WithInvalidEntityType_ReturnsError()
    {
        // Arrange
        var request = new UpdateEntityRequest
        {
            Name = "Test Entity",
            EntityType = (EntityType)999 // Invalid enum value
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle()
            .Which.PropertyName.Should().Be("EntityType");
        result.Errors.First().ErrorMessage.Should().Be("Invalid entity type.");
    }

    /// <summary>
    /// Tests that more than 20 tags fails validation.
    /// </summary>
    [TestMethod]
    public async Task ValidateAsync_WithTooManyTags_ReturnsError()
    {
        // Arrange
        var request = new UpdateEntityRequest
        {
            Name = "Test Entity",
            EntityType = EntityType.Character,
            Tags = Enumerable.Range(1, 21).Select(i => $"tag{i}").ToList() // 21 tags
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle()
            .Which.PropertyName.Should().Be("Tags");
        result.Errors.First().ErrorMessage.Should().Be("Maximum 20 tags allowed.");
    }

    /// <summary>
    /// Tests that exactly 20 tags passes validation.
    /// </summary>
    [TestMethod]
    public async Task ValidateAsync_WithMaximumTags_ReturnsNoErrors()
    {
        // Arrange
        var request = new UpdateEntityRequest
        {
            Name = "Test Entity",
            EntityType = EntityType.Character,
            Tags = Enumerable.Range(1, 20).Select(i => $"tag{i}").ToList() // Exactly 20 tags
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    /// <summary>
    /// Tests that a tag exceeding maximum length fails validation.
    /// </summary>
    [TestMethod]
    public async Task ValidateAsync_WithTagTooLong_ReturnsError()
    {
        // Arrange
        var request = new UpdateEntityRequest
        {
            Name = "Test Entity",
            EntityType = EntityType.Character,
            Tags = [new string('A', 51)] // 51 characters
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle()
            .Which.PropertyName.Should().Be("Tags");
        result.Errors.First().ErrorMessage.Should().Be("Each tag must be 1-50 characters.");
    }

    /// <summary>
    /// Tests that a tag at maximum length passes validation.
    /// </summary>
    [TestMethod]
    public async Task ValidateAsync_WithTagAtMaxLength_ReturnsNoErrors()
    {
        // Arrange
        var request = new UpdateEntityRequest
        {
            Name = "Test Entity",
            EntityType = EntityType.Character,
            Tags = [new string('A', 50)] // Exactly 50 characters
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    /// <summary>
    /// Tests that an empty tag fails validation.
    /// </summary>
    [TestMethod]
    public async Task ValidateAsync_WithEmptyTag_ReturnsError()
    {
        // Arrange
        var request = new UpdateEntityRequest
        {
            Name = "Test Entity",
            EntityType = EntityType.Character,
            Tags = ["validTag", string.Empty]
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle()
            .Which.PropertyName.Should().Be("Tags");
        result.Errors.First().ErrorMessage.Should().Be("Each tag must be 1-50 characters.");
    }

    /// <summary>
    /// Tests that a whitespace tag fails validation.
    /// </summary>
    [TestMethod]
    public async Task ValidateAsync_WithWhitespaceTag_ReturnsError()
    {
        // Arrange
        var request = new UpdateEntityRequest
        {
            Name = "Test Entity",
            EntityType = EntityType.Character,
            Tags = ["validTag", "   "]
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle()
            .Which.PropertyName.Should().Be("Tags");
        result.Errors.First().ErrorMessage.Should().Be("Each tag must be 1-50 characters.");
    }

    /// <summary>
    /// Tests that attributes exceeding 100KB fails validation.
    /// </summary>
    [TestMethod]
    public async Task ValidateAsync_WithAttributesTooLarge_ReturnsError()
    {
        // Arrange
        // Create a dictionary that will exceed 100KB when serialized
        var largeAttributes = new Dictionary<string, object>();
        for (int i = 0; i < 5000; i++)
        {
            largeAttributes[$"key{i}"] = new string('A', 50);
        }

        var request = new UpdateEntityRequest
        {
            Name = "Test Entity",
            EntityType = EntityType.Character,
            Attributes = largeAttributes
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle()
            .Which.PropertyName.Should().Be("Attributes");
        result.Errors.First().ErrorMessage.Should().Be("Attributes must not exceed 100KB serialized.");
    }

    /// <summary>
    /// Tests that attributes within size limit passes validation.
    /// </summary>
    [TestMethod]
    public async Task ValidateAsync_WithAttributesWithinLimit_ReturnsNoErrors()
    {
        // Arrange
        var request = new UpdateEntityRequest
        {
            Name = "Test Entity",
            EntityType = EntityType.Character,
            Attributes = new Dictionary<string, object>
            {
                { "level", 25 },
                { "class", "Paladin" },
                { "hitPoints", 150 },
                { "divinePoints", 100 }
            }
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    /// <summary>
    /// Tests that null tags passes validation.
    /// </summary>
    [TestMethod]
    public async Task ValidateAsync_WithNullTags_ReturnsNoErrors()
    {
        // Arrange
        var request = new UpdateEntityRequest
        {
            Name = "Test Entity",
            EntityType = EntityType.Character,
            Tags = null
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    /// <summary>
    /// Tests that null attributes passes validation.
    /// </summary>
    [TestMethod]
    public async Task ValidateAsync_WithNullAttributes_ReturnsNoErrors()
    {
        // Arrange
        var request = new UpdateEntityRequest
        {
            Name = "Test Entity",
            EntityType = EntityType.Character,
            Attributes = null
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }
}
