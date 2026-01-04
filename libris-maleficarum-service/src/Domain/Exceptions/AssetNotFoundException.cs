namespace LibrisMaleficarum.Domain.Exceptions;

/// <summary>
/// Exception thrown when an asset is not found.
/// </summary>
public sealed class AssetNotFoundException : Exception
{
    /// <summary>
    /// Gets the asset identifier that was not found.
    /// </summary>
    public Guid AssetId { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="AssetNotFoundException"/> class.
    /// </summary>
    /// <param name="assetId">Asset identifier that was not found.</param>
    public AssetNotFoundException(Guid assetId)
        : base($"Asset with ID '{assetId}' was not found.")
    {
        AssetId = assetId;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AssetNotFoundException"/> class with a custom message.
    /// </summary>
    /// <param name="assetId">Asset identifier that was not found.</param>
    /// <param name="message">Custom error message.</param>
    public AssetNotFoundException(Guid assetId, string message)
        : base(message)
    {
        AssetId = assetId;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AssetNotFoundException"/> class with a custom message and inner exception.
    /// </summary>
    /// <param name="assetId">Asset identifier that was not found.</param>
    /// <param name="message">Custom error message.</param>
    /// <param name="innerException">Inner exception.</param>
    public AssetNotFoundException(Guid assetId, string message, Exception innerException)
        : base(message, innerException)
    {
        AssetId = assetId;
    }
}
