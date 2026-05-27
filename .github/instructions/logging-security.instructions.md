---
description: "Logging security rules: prevent log injection and sensitive data exposure in all C# log statements"
applyTo: "**/*.cs"
---

# Logging Security — Backend C# Code

## Rule: Never log raw user-derived values

Do not pass raw user input, user-supplied content, or values derived from user input directly to any log call. This prevents log injection attacks (CWE-117 / OWASP Log Injection) and protects sensitive user data.

**Prohibited patterns — do not write code like these:**

```csharp
// BAD — raw user query in a log call
_logger.LogDebug("Search query: {Query}", request.Query);

// BAD — user display name in a log call
_logger.LogInformation("User {DisplayName} signed in", user.DisplayName);

// BAD — access code value in a log call
_logger.LogWarning("Bad access code: {Code}", providedCode);

// BAD — transcription text in a log call
_logger.LogInformation("Transcribed: {Text}", transcriptionResult.Text);
```

**Approved alternatives — use metadata instead of content:**

```csharp
// GOOD — log a count, not the content
_logger.LogDebug("Search completed: QueryLength={QueryLength}", request.Query.Length);

// GOOD — log a lifecycle event, not the identity content
_logger.LogInformation("User sign-in completed");

// GOOD — log an outcome, not the secret value
_logger.LogWarning("Access code validation failed for protected endpoint");

// GOOD — log metrics only
_logger.LogInformation("Speech recognized: TextLength={TextLength}, DurationMs={DurationMs}",
    text.Length, duration.TotalMilliseconds);
```

## Sensitive source categories

Treat the following as sensitive and never log their raw string values:

| Category | Examples |
|---|---|
| User query text | `request.Query`, `searchRequest.Text` |
| Transcription text | `result.Text`, `transcription.Content` |
| Access code values | `providedCode`, `request.Headers["X-Access-Code"]` |
| User display names | `user.DisplayName`, `profile.Name` |
| User-authored content | template content, prompt text, world entity names or descriptions |
| Locale / language strings | only log if required for diagnosis |

## Safe log metadata

These are always safe to log:

- Counts and lengths (`int`, `long`)
- Durations (`TimeSpan`, milliseconds)
- Boolean flags (`bool`)
- GUIDs and structured IDs (e.g., `WorldId`, `EntityId`, `UserId`)
- HTTP status codes and HTTP methods
- Exception types (not exception messages that may embed user input)
- Lifecycle event names (e.g., "started", "completed", "failed")

## Defense-in-depth processor

A `SanitizingLogRecordProcessor` is registered centrally in `ServiceDefaults/Extensions.cs`. This processor escapes control characters (CR, LF, TAB) from all string values in every `LogRecord` before export.

This is a safety net only. It does **not** replace the call-site rule above. Raw content will still appear sanitized — but that content should never have been logged in the first place.

## Validation

CI runs a grep scan over `src/**/*.cs` that fails the build if any of the following forbidden patterns are detected:

- `LogDebug.*\.Query[^L]` — raw query string in a debug log
- `Log.*[Aa]ccessCode\|Log.*providedCode` — access code values in logs
- `Log.*[Tt]ranscri` — transcription text in logs

If you believe a specific exception is warranted, document the justification in a comment and add the line to the allowlist in `.github/workflows/build-backend-service.yml`.
