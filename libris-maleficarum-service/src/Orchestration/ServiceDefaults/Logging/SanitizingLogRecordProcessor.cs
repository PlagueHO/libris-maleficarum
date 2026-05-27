using OpenTelemetry;
using OpenTelemetry.Logs;

namespace LibrisMaleficarum.ServiceDefaults.Logging;

/// <summary>
/// An OpenTelemetry log processor that sanitizes all string-valued fields in
/// each <see cref="LogRecord"/> before it is exported.
/// </summary>
/// <remarks>
/// This processor is a defense-in-depth control. It escapes control characters
/// (CR, LF, TAB, and other non-printable code points) in <c>FormattedMessage</c>
/// and any string-valued structured log attributes, reducing the risk of log
/// injection attacks (CWE-117 / OWASP Log Injection) if a future call site
/// inadvertently passes raw user input to a log statement.
///
/// Non-string attribute values are never mutated.
/// </remarks>
public sealed class SanitizingLogRecordProcessor : BaseProcessor<LogRecord>
{
    /// <inheritdoc/>
    public override void OnEnd(LogRecord data)
    {
        ArgumentNullException.ThrowIfNull(data);

        // Sanitize the pre-formatted log message, which is what most exporters emit.
        if (data.FormattedMessage is not null)
        {
            data.FormattedMessage = LogSanitizer.Sanitize(data.FormattedMessage);
        }

        // Sanitize individual structured attribute values in-place.
        // The underlying collection in the OpenTelemetry .NET SDK is a mutable List<>.
        // The cast will succeed at runtime; if the SDK ever changes to an immutable type
        // the cast will return null and we skip silently — the FormattedMessage is still sanitized.
        if (data.Attributes is IList<KeyValuePair<string, object?>> mutableAttributes)
        {
            for (var i = 0; i < mutableAttributes.Count; i++)
            {
                if (mutableAttributes[i].Value is string rawValue)
                {
                    var sanitized = LogSanitizer.Sanitize(rawValue);

                    // Avoid allocation when nothing changed.
                    if (!ReferenceEquals(sanitized, rawValue))
                    {
                        mutableAttributes[i] = new KeyValuePair<string, object?>(
                            mutableAttributes[i].Key,
                            sanitized);
                    }
                }
            }
        }
    }
}
