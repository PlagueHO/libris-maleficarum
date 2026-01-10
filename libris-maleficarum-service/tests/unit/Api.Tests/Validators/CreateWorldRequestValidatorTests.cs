namespace LibrisMaleficarum.Api.Tests.Validators;

using FluentAssertions;
using LibrisMaleficarum.Api.Models.Requests;
using LibrisMaleficarum.Api.Validators;
using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Tests for <see cref="CreateWorldRequestValidator"/>.
/// </summary>
[TestClass]
[TestCategory("Unit")]
public class CreateWorldRequestValidatorTests
{
    private CreateWorldRequestValidator _validator = null!;

    /// <summary>
    /// Initializes the validator before each test.
    /// </summary>
    [TestInitialize]
    public void Setup()
    {
        _validator = new CreateWorldRequestValidator();
    }

    /// <summary>
    /// Tests that a valid request passes validation.
    /// </summary>
    [TestMethod]
    public async Task ValidateAsync_WithValidRequest_ReturnsNoErrors()
    {
        // Arrange
        var request = new CreateWorldRequest
        {
            Name = "Test World",
            Description = "A test world description"
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
        var request = new CreateWorldRequest
        {
            Name = string.Empty,
            Description = "Description"
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().HaveCount(2);
        result.Errors.Should().Contain(e => e.PropertyName == "Name" && e.ErrorMessage == "Name is required.");
        result.Errors.Should().Contain(e => e.PropertyName == "Name" && e.ErrorMessage == "Name must be between 1 and 100 characters.");
    }

    /// <summary>
    /// Tests that a null name fails validation.
    /// </summary>
    [TestMethod]
    public async Task ValidateAsync_WithNullName_ReturnsError()
    {
        // Arrange
        var request = new CreateWorldRequest
        {
            Name = null!,
            Description = "Description"
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle()
            .Which.PropertyName.Should().Be("Name");
        result.Errors.First().ErrorMessage.Should().Be("Name is required.");
    }

    /// <summary>
    /// Tests that a name exceeding maximum length fails validation.
    /// </summary>
    [TestMethod]
    public async Task ValidateAsync_WithNameTooLong_ReturnsError()
    {
        // Arrange
        var request = new CreateWorldRequest
        {
            Name = new string('A', 101), // 101 characters
            Description = "Description"
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle()
            .Which.PropertyName.Should().Be("Name");
        result.Errors.First().ErrorMessage.Should().Be("Name must be between 1 and 100 characters.");
    }

    /// <summary>
    /// Tests that a name at maximum length passes validation.
    /// </summary>
    [TestMethod]
    public async Task ValidateAsync_WithNameAtMaxLength_ReturnsNoErrors()
    {
        // Arrange
        var request = new CreateWorldRequest
        {
            Name = new string('A', 100), // Exactly 100 characters
            Description = "Description"
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    /// <summary>
    /// Tests that a single character name passes validation.
    /// </summary>
    [TestMethod]
    public async Task ValidateAsync_WithSingleCharacterName_ReturnsNoErrors()
    {
        // Arrange
        var request = new CreateWorldRequest
        {
            Name = "A",
            Description = "Description"
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
        var request = new CreateWorldRequest
        {
            Name = "Test World",
            Description = new string('A', 2001) // 2001 characters
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle()
            .Which.PropertyName.Should().Be("Description");
        result.Errors.First().ErrorMessage.Should().Be("Description must not exceed 2000 characters.");
    }

    /// <summary>
    /// Tests that a description at maximum length passes validation.
    /// </summary>
    [TestMethod]
    public async Task ValidateAsync_WithDescriptionAtMaxLength_ReturnsNoErrors()
    {
        // Arrange
        var request = new CreateWorldRequest
        {
            Name = "Test World",
            Description = new string('A', 2000) // Exactly 2000 characters
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    /// <summary>
    /// Tests that a null description passes validation.
    /// </summary>
    [TestMethod]
    public async Task ValidateAsync_WithNullDescription_ReturnsNoErrors()
    {
        // Arrange
        var request = new CreateWorldRequest
        {
            Name = "Test World",
            Description = null
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    /// <summary>
    /// Tests that an empty description passes validation.
    /// </summary>
    [TestMethod]
    public async Task ValidateAsync_WithEmptyDescription_ReturnsNoErrors()
    {
        // Arrange
        var request = new CreateWorldRequest
        {
            Name = "Test World",
            Description = string.Empty
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }
}
