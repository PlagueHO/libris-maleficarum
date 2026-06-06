namespace LibrisMaleficarum.Infrastructure.Tests.Search;

using Azure.Search.Documents.Indexes.Models;
using FluentAssertions;
using LibrisMaleficarum.Infrastructure.Configuration;
using LibrisMaleficarum.Infrastructure.Search;

/// <summary>
/// Unit tests for <see cref="VectorIndexProfile"/>.
/// Covers the static factory for all <see cref="VectorCompressionKind"/> values,
/// rescoring on/off, custom oversampling, null guard, and invalid enum.
/// </summary>
[TestClass]
[TestCategory("Unit")]
public class VectorIndexProfileTests
{
    #region None

    [TestMethod]
    public void From_None_ReturnsEmptyCompressionsNullNameNullOversampling()
    {
        // Arrange
        var options = new SearchOptions { VectorCompression = VectorCompressionKind.None };

        // Act
        var profile = VectorIndexProfile.From(options);

        // Assert
        profile.Compressions.Should().BeEmpty();
        profile.CompressionName.Should().BeNull();
        profile.QueryOversampling.Should().BeNull();
    }

    #endregion

    #region ScalarQuantization

    [TestMethod]
    public void From_ScalarQuantization_Default_ReturnsSingleScalarCompression()
    {
        // Arrange
        var options = new SearchOptions { VectorCompression = VectorCompressionKind.ScalarQuantization };

        // Act
        var profile = VectorIndexProfile.From(options);

        // Assert
        profile.Compressions.Should().HaveCount(1);
        profile.Compressions[0].Should().BeOfType<ScalarQuantizationCompression>();
        profile.CompressionName.Should().Be("scalar-compression");
    }

    [TestMethod]
    public void From_ScalarQuantization_RescoringEnabled_SetsOversamplingAndPreserveOriginals()
    {
        // Arrange
        var options = new SearchOptions
        {
            VectorCompression = VectorCompressionKind.ScalarQuantization,
            EnableRescoring = true,
            DefaultOversampling = 10.0
        };

        // Act
        var profile = VectorIndexProfile.From(options);

        // Assert
        profile.QueryOversampling.Should().Be(10.0);

        var compression = (ScalarQuantizationCompression)profile.Compressions[0];
        compression.RescoringOptions.Should().NotBeNull();
        compression.RescoringOptions!.EnableRescoring.Should().BeTrue();
        compression.RescoringOptions!.DefaultOversampling.Should().Be(10.0);
        compression.RescoringOptions!.RescoreStorageMethod
            .Should().Be(VectorSearchCompressionRescoreStorageMethod.PreserveOriginals);
    }

    [TestMethod]
    public void From_ScalarQuantization_RescoringDisabled_OversamplingIsNull()
    {
        // Arrange
        var options = new SearchOptions
        {
            VectorCompression = VectorCompressionKind.ScalarQuantization,
            EnableRescoring = false
        };

        // Act
        var profile = VectorIndexProfile.From(options);

        // Assert
        profile.QueryOversampling.Should().BeNull();

        var compression = (ScalarQuantizationCompression)profile.Compressions[0];
        compression.RescoringOptions.Should().BeNull();
    }

    [TestMethod]
    public void From_ScalarQuantization_CustomOversampling_UsesConfiguredValue()
    {
        // Arrange
        var options = new SearchOptions
        {
            VectorCompression = VectorCompressionKind.ScalarQuantization,
            EnableRescoring = true,
            DefaultOversampling = 20.0
        };

        // Act
        var profile = VectorIndexProfile.From(options);

        // Assert
        profile.QueryOversampling.Should().Be(20.0);

        var compression = (ScalarQuantizationCompression)profile.Compressions[0];
        compression.RescoringOptions!.DefaultOversampling.Should().Be(20.0);
    }

    #endregion

    #region BinaryQuantization

    [TestMethod]
    public void From_BinaryQuantization_Default_ReturnsSingleBinaryCompression()
    {
        // Arrange
        var options = new SearchOptions { VectorCompression = VectorCompressionKind.BinaryQuantization };

        // Act
        var profile = VectorIndexProfile.From(options);

        // Assert
        profile.Compressions.Should().HaveCount(1);
        profile.Compressions[0].Should().BeOfType<BinaryQuantizationCompression>();
        profile.CompressionName.Should().Be("binary-compression");
    }

    [TestMethod]
    public void From_BinaryQuantization_RescoringEnabled_SetsOversamplingAndPreserveOriginals()
    {
        // Arrange
        var options = new SearchOptions
        {
            VectorCompression = VectorCompressionKind.BinaryQuantization,
            EnableRescoring = true,
            DefaultOversampling = 10.0
        };

        // Act
        var profile = VectorIndexProfile.From(options);

        // Assert
        profile.QueryOversampling.Should().Be(10.0);

        var compression = (BinaryQuantizationCompression)profile.Compressions[0];
        compression.RescoringOptions.Should().NotBeNull();
        compression.RescoringOptions!.EnableRescoring.Should().BeTrue();
        compression.RescoringOptions!.DefaultOversampling.Should().Be(10.0);
        compression.RescoringOptions!.RescoreStorageMethod
            .Should().Be(VectorSearchCompressionRescoreStorageMethod.PreserveOriginals);
    }

    [TestMethod]
    public void From_BinaryQuantization_RescoringDisabled_OversamplingIsNull()
    {
        // Arrange
        var options = new SearchOptions
        {
            VectorCompression = VectorCompressionKind.BinaryQuantization,
            EnableRescoring = false
        };

        // Act
        var profile = VectorIndexProfile.From(options);

        // Assert
        profile.QueryOversampling.Should().BeNull();

        var compression = (BinaryQuantizationCompression)profile.Compressions[0];
        compression.RescoringOptions.Should().BeNull();
    }

    #endregion

    #region Null and invalid input

    [TestMethod]
    public void From_NullOptions_ThrowsArgumentNullException()
    {
        // Arrange & Act
        var act = () => VectorIndexProfile.From(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("options");
    }

    [TestMethod]
    public void From_InvalidVectorCompressionKind_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var options = new SearchOptions { VectorCompression = (VectorCompressionKind)99 };

        // Act
        var act = () => VectorIndexProfile.From(options);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("options");
    }

    #endregion
}
