namespace LibrisMaleficarum.Infrastructure.Search;

using Azure.Search.Documents.Indexes.Models;
using LibrisMaleficarum.Infrastructure.Configuration;

/// <summary>
/// Immutable value object that encapsulates all vector compression settings
/// for both index creation and query execution.
/// </summary>
/// <remarks>
/// Build once via <see cref="From(SearchOptions)"/> at service construction time
/// and reuse for both <c>EnsureIndexExistsAsync</c> (index-side compressions and profile wiring)
/// and <c>SearchAsync</c> (query-side oversampling).
/// </remarks>
/// <param name="Compressions">Compression configurations to register in the index.</param>
/// <param name="CompressionName">Name to set on the vector search profile. <c>null</c> when compression is disabled.</param>
/// <param name="QueryOversampling">Oversampling factor to apply to vectorized queries. <c>null</c> when compression or rescoring is disabled.</param>
public sealed record VectorIndexProfile(
    IReadOnlyList<VectorSearchCompression> Compressions,
    string? CompressionName,
    double? QueryOversampling)
{
    private const string ScalarCompressionName = "scalar-compression";
    private const string BinaryCompressionName = "binary-compression";

    /// <summary>
    /// Creates a <see cref="VectorIndexProfile"/> from the supplied <see cref="SearchOptions"/>.
    /// </summary>
    /// <param name="options">The search configuration options.</param>
    /// <returns>A fully configured <see cref="VectorIndexProfile"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="options"/> is <c>null</c>.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <see cref="SearchOptions.VectorCompression"/> has an unrecognized value.</exception>
    public static VectorIndexProfile From(SearchOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        return options.VectorCompression switch
        {
            VectorCompressionKind.None => new([], null, null),
            VectorCompressionKind.ScalarQuantization => BuildScalar(options),
            VectorCompressionKind.BinaryQuantization => BuildBinary(options),
            _ => throw new ArgumentOutOfRangeException(
                nameof(options),
                options.VectorCompression,
                $"Unknown VectorCompressionKind: {options.VectorCompression}")
        };
    }

    private static VectorIndexProfile BuildScalar(SearchOptions options)
    {
        var compression = new ScalarQuantizationCompression(ScalarCompressionName);

        if (options.EnableRescoring)
        {
            compression.RescoringOptions = new RescoringOptions
            {
                EnableRescoring = true,
                DefaultOversampling = options.DefaultOversampling,
                RescoreStorageMethod = VectorSearchCompressionRescoreStorageMethod.PreserveOriginals
            };
        }

        return new(
            [compression],
            ScalarCompressionName,
            options.EnableRescoring ? options.DefaultOversampling : null);
    }

    private static VectorIndexProfile BuildBinary(SearchOptions options)
    {
        var compression = new BinaryQuantizationCompression(BinaryCompressionName);

        if (options.EnableRescoring)
        {
            compression.RescoringOptions = new RescoringOptions
            {
                EnableRescoring = true,
                DefaultOversampling = options.DefaultOversampling,
                RescoreStorageMethod = VectorSearchCompressionRescoreStorageMethod.PreserveOriginals
            };
        }

        return new(
            [compression],
            BinaryCompressionName,
            options.EnableRescoring ? options.DefaultOversampling : null);
    }
}
