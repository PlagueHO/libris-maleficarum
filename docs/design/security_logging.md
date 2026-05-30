# Logging Security

## Purpose

This document defines the logging safety rules for the Libris Maleficarum backend service. The rules prevent log injection attacks (CWE-117) and protect sensitive user data from appearing in log sinks.

## Defense layers

The solution uses two complementary layers:

### Layer 1 — Central sanitization processor

A custom OpenTelemetry processor (`SanitizingLogRecordProcessor`) is registered in `ServiceDefaults/Extensions.cs`. It runs on every `LogRecord` before export and escapes control characters (CR, LF, TAB, and other non-printable code points) in `FormattedMessage` and string-valued attributes.

This layer prevents multi-line injection and hidden control-character payloads if a future call site accidentally logs unsanitized text.

**Location:** `src/Orchestration/ServiceDefaults/Logging/`

### Layer 2 — Call-site hardening

No log call may emit raw user-derived content. Only safe metadata is logged:

| Safe | Unsafe |
|---|---|
| Counts, lengths | Raw search query text |
| Durations, latency | Transcription text |
| Boolean flags | Access code values |
| Structured IDs (GUIDs) | User display names |
| HTTP status codes | User-authored content |
| Lifecycle event names | Locale/language strings |

**Reference:** `.github/instructions/logging-security.instructions.md`

## Example patterns

### Search service

```csharp
// GOOD
_logger.LogInformation(
    "Search completed: WorldId={WorldId}, Mode={Mode}, QueryLength={QueryLength}, ResultCount={ResultCount}, LatencyMs={LatencyMs}",
    request.WorldId, request.Mode, request.Query.Length, results.Count, stopwatch.ElapsedMilliseconds);

// BAD — removed; was: _logger.LogDebug("Search raw query: {Query}", request.Query);
```

### Access code middleware

```csharp
// GOOD — logs path and outcome only; never logs the code value
_logger.LogWarning("Access code required but not provided for {Path}", requestPath);
_logger.LogWarning("Invalid access code provided for {Path}", requestPath);
```

## Validation gate

CI runs a grep scan in `build-backend-service.yml` that fails the build if unsafe patterns are detected. See that workflow for the exact regex patterns and any documented exceptions.

## References

- [OWASP Log Injection](https://owasp.org/www-community/attacks/Log_Injection)
- [CWE-117: Improper Output Neutralization for Logs](https://cwe.mitre.org/data/definitions/117.html)
- [OpenTelemetry .NET — Log processors](https://opentelemetry.io/docs/languages/net/instrumentation/#log-processor)
