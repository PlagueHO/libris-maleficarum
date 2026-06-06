namespace LibrisMaleficarum.SearchIndexWorker;

using Microsoft.Azure.Cosmos;
using System.Text.Json;

/// <summary>
/// A <see cref="CosmosSerializer"/> implementation backed by System.Text.Json.
/// The default Cosmos serializer uses Newtonsoft.Json, which cannot deserialize into
/// <see cref="System.Text.Json.JsonElement"/>: it silently produces default structs with
/// <see cref="JsonValueKind.Undefined"/>, causing every Change Feed entry to be skipped.
/// Using this serializer ensures the Change Feed Processor can correctly materialise
/// <c>IReadOnlyCollection&lt;JsonElement&gt;</c> batches.
/// </summary>
internal sealed class SystemTextJsonCosmosSerializer : CosmosSerializer
{
    private readonly JsonSerializerOptions _options;

    internal SystemTextJsonCosmosSerializer(JsonSerializerOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    /// <inheritdoc/>
    public override T FromStream<T>(Stream stream)
    {
        using (stream)
        {
            // When T is Stream the caller wants the raw stream back (e.g. for diagnostics).
            if (typeof(Stream).IsAssignableFrom(typeof(T)))
            {
                return (T)(object)stream;
            }

            return JsonSerializer.Deserialize<T>(stream, _options)!;
        }
    }

    /// <inheritdoc/>
    public override Stream ToStream<T>(T input)
    {
        var ms = new MemoryStream();
        JsonSerializer.Serialize(ms, input, _options);
        ms.Position = 0;
        return ms;
    }
}
