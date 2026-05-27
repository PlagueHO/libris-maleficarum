using System.Text;

namespace LibrisMaleficarum.ServiceDefaults.Logging;

/// <summary>
/// Provides defense-in-depth sanitization of log message strings to prevent
/// log injection attacks (CWE-117 / OWASP Log Injection).
/// </summary>
/// <remarks>
/// This sanitizer escapes control characters so that a malicious user cannot
/// forge additional log entries by embedding newlines or other control characters
/// in user-supplied values that reach a log sink.
/// Printable ASCII and Unicode text is left unchanged.
/// </remarks>
public static class LogSanitizer
{
    /// <summary>
    /// Sanitizes a log string by escaping all control characters.
    /// </summary>
    /// <param name="value">The raw string to sanitize.</param>
    /// <returns>
    /// The sanitized string with control characters replaced by their escape sequences,
    /// or <see langword="null"/> if <paramref name="value"/> is <see langword="null"/>.
    /// </returns>
    public static string? Sanitize(string? value)
    {
        if (value is null)
        {
            return null;
        }

        // Fast path: scan for any character requiring escaping before allocating.
        var needsEscape = false;
        foreach (var ch in value)
        {
            if (IsControlCharacter(ch))
            {
                needsEscape = true;
                break;
            }
        }

        if (!needsEscape)
        {
            return value;
        }

        var builder = new StringBuilder(value.Length + 16);
        foreach (var ch in value)
        {
            switch (ch)
            {
                case '\r':
                    builder.Append("\\r");
                    break;
                case '\n':
                    builder.Append("\\n");
                    break;
                case '\t':
                    builder.Append("\\t");
                    break;
                default:
                    if (IsControlCharacter(ch))
                    {
                        builder.Append($"\\u{(int)ch:X4}");
                    }
                    else
                    {
                        builder.Append(ch);
                    }

                    break;
            }
        }

        return builder.ToString();
    }

    /// <summary>
    /// Returns <see langword="true"/> if <paramref name="ch"/> is a control character
    /// that could be exploited for log injection.
    /// </summary>
    private static bool IsControlCharacter(char ch) =>
        ch < '\u0020' || ch == '\u007F';
}
