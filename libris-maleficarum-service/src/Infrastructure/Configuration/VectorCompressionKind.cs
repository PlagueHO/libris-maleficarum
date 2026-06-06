namespace LibrisMaleficarum.Infrastructure.Configuration;

/// <summary>
/// Specifies the type of vector compression applied to the Azure AI Search index.
/// </summary>
/// <remarks>
/// Compression type is a deploy-time decision. Changing compression on an existing index
/// requires dropping and recreating the index. The Change Feed worker re-indexes all documents
/// automatically from <see cref="System.DateTime.MinValue"/> on startup, so a recreate
/// fully repopulates the index without manual intervention.
/// </remarks>
public enum VectorCompressionKind
{
    /// <summary>
    /// No vector compression. Stores full float32 embeddings (~6 KB per 1536-dimension vector).
    /// Highest recall quality, highest storage and memory cost.
    /// </summary>
    None,

    /// <summary>
    /// Scalar (int8) quantization. Reduces storage and memory by ~4× with near-zero recall loss.
    /// Recommended default. Supports rescoring with <c>PreserveOriginals</c> for quality recovery.
    /// </summary>
    ScalarQuantization,

    /// <summary>
    /// Binary quantization. Reduces storage and memory by ~32× with moderate recall loss.
    /// Requires rescoring enabled to remain competitive with full-precision recall.
    /// </summary>
    BinaryQuantization,
}
