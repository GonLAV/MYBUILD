using System.Diagnostics;
using System.Text.Json;
using Bolt.Automation.Common.Logging.Core;
using MongoDB.Bson;

namespace Bolt.Automation.Common.Logging.Mongo;

public class MongoLoggingDecorator : IAutomationLogger
{
    private readonly IAutomationLogger _inner;
    private readonly ITestRunWriter _writer;

    // Instance fields — each test creates its own DI container
    // and thus its own MongoLoggingDecorator singleton.
    private string? _testId;

    // Open steps, innermost on top — mirrors the inner AutomationLogger's step stack.
    // A single "current step" field cannot represent nesting: parents never completed.
    private readonly AsyncLocal<Stack<OpenStep>> _openSteps = new();

    private Stack<OpenStep> OpenSteps => _openSteps.Value ??= new Stack<OpenStep>();

    private readonly record struct OpenStep(string Name, DateTime StartedAt);

    public MongoLoggingDecorator(IAutomationLogger inner, ITestRunWriter writer)
    {
        _inner = inner;
        _writer = writer;
    }

    public void SetTestId(string testId) => _testId = testId;
    public void ClearTestId() => _testId = null;

    // --- Basic logging: delegate + buffer to Mongo ---

    public void Log(LogLevel level, string message, params object[] args)
    {
        _inner.Log(level, message, args);
        BufferEntry(level, "General", Format(message, args));
    }

    public void Info(string message, params object[] args)
    {
        _inner.Info(message, args);
        BufferEntry(LogLevel.Info, "General", Format(message, args));
    }

    public void Debug(string message, params object[] args)
    {
        _inner.Debug(message, args);
        BufferEntry(LogLevel.Debug, "General", Format(message, args));
    }

    public void Trace(string message, params object[] args)
    {
        _inner.Trace(message, args);
        BufferEntry(LogLevel.Trace, "General", Format(message, args));
    }

    public void Warning(string message, params object[] args)
    {
        _inner.Warning(message, args);
        BufferEntry(LogLevel.Warning, "General", Format(message, args));
    }

    public void Error(string message, params object[] args)
    {
        _inner.Error(message, args);
        BufferEntry(LogLevel.Error, "General", Format(message, args));
    }

    public void Fatal(string message, params object[] args)
    {
        _inner.Fatal(message, args);
        BufferEntry(LogLevel.Fatal, "General", Format(message, args));
    }

    public void LogException(Exception exception, string? message = null, params object[] args)
    {
        _inner.LogException(exception, message, args);
        var formattedMessage = string.IsNullOrEmpty(message)
            ? null
            : Format(message, args);
        var msg = formattedMessage is null
            ? $"Exception: {exception.Message}"
            : $"{formattedMessage} - Exception: {exception.Message}";
        BufferEntry(LogLevel.Error, "Exception", msg);
    }

    // --- Steps ---

    public IStepScope StartStep(string stepName, string? description = null)
    {
        var startedAt = DateTime.UtcNow;
        OpenSteps.Push(new OpenStep(stepName, startedAt));

        // Skip MongoDB step recording during debugging
        if (!Debugger.IsAttached)
        {
            var testId = _testId;
            if (!string.IsNullOrEmpty(testId))
            {
                _writer.RecordStep(testId, new StepRecord
                {
                    Name = stepName,
                    Status = "Running",
                    StartedAt = startedAt
                });
            }
        }

        BufferEntry(LogLevel.Info, "Step", $"Starting step: {stepName}" +
            (description != null ? $" - {description}" : ""));

        var innerScope = _inner.StartStep(stepName, description);
        return new MongoStepScope(this, innerScope, stepName, startedAt);
    }

    public void EndStep(StepStatus status = StepStatus.Passed)
    {
        _inner.EndStep(status);

        if (_openSteps.Value is { Count: > 0 } stack)
        {
            var open = stack.Peek();
            CompleteMongoStep(open.Name, open.StartedAt, status);
        }
    }

    private void CompleteMongoStep(string stepName, DateTime startedAt, StepStatus status)
    {
        var durationMs = (long)(DateTime.UtcNow - startedAt).TotalMilliseconds;

        // Skip MongoDB step completion during debugging
        if (!Debugger.IsAttached)
        {
            var testId = _testId;
            if (!string.IsNullOrEmpty(testId))
            {
                var statusText = status switch
                {
                    StepStatus.Passed => "Passed",
                    StepStatus.Failed => "Failed",
                    StepStatus.Skipped => "Skipped",
                    StepStatus.Blocked => "Blocked",
                    StepStatus.Warning => "Warning",
                    _ => status.ToString()
                };
                _writer.CompleteStep(testId, stepName, statusText, durationMs);
                BufferEntry(LogLevel.Info, "Step", $"Step completed: {stepName} - {statusText} ({durationMs}ms)");
            }
        }

        // Popped last so the completion entry above is still attributed to this step.
        if (_openSteps.Value is { Count: > 0 } stack)
            stack.Pop();
    }

    private sealed class MongoStepScope : IStepScope
    {
        private readonly MongoLoggingDecorator _decorator;
        private readonly IStepScope _innerScope;
        private readonly string _stepName;
        private readonly DateTime _startedAt;
        private StepStatus? _explicitStatus;
        private bool _disposed;

        public MongoStepScope(MongoLoggingDecorator decorator, IStepScope innerScope, string stepName, DateTime startedAt)
        {
            _decorator = decorator;
            _innerScope = innerScope;
            _stepName = stepName;
            _startedAt = startedAt;
        }

        public void Complete(string? details = null)
        {
            _explicitStatus = StepStatus.Passed;
            _innerScope.Complete(details);
        }

        public void Fail(string? reason = null)
        {
            _explicitStatus = StepStatus.Failed;
            _innerScope.Fail(reason);
        }

        public void Skip(string? reason = null)
        {
            _explicitStatus = StepStatus.Skipped;
            _innerScope.Skip(reason);
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            var status = _explicitStatus ?? StepStatus.Failed;
            _innerScope.Dispose();          // pops the NLog step stack
            _decorator.CompleteMongoStep(_stepName, _startedAt, status);  // updates MongoDB
        }
    }

    public void AttachFile(string filePath, string? description = null)
    {
        _inner.AttachFile(filePath, description);
        BufferEntry(LogLevel.Info, "Attachment", $"File: {filePath}" +
            (description != null ? $" - {description}" : ""));
    }

    // --- Specialized logging ---

    public void LogBusinessRule(string ruleName, bool passed, string? details = null)
    {
        _inner.LogBusinessRule(ruleName, passed, details);

        var payload = new BsonDocument
        {
            { "ruleName", ruleName },
            { "ruleStatus", passed ? "PASSED" : "FAILED" },
            { "details", details ?? BsonString.Empty }
        };
        BufferEntry(passed ? LogLevel.Info : LogLevel.Error, "BusinessRule",
            $"Business Rule [{ruleName}] {(passed ? "PASSED" : "FAILED")}: {details}", payload);
    }

    private const int PayloadAttachmentThreshold = 5000;
    private const int InlineMaxLength = 1000;

    public async Task LogApiCallAsync(HttpRequestMessage request, HttpResponseMessage response, long durationMs)
    {
        await _inner.LogApiCallAsync(request, response, durationMs);

        var requestContent = request.Content != null ? await request.Content.ReadAsStringAsync() : null;
        var responseContent = response.Content != null ? await response.Content.ReadAsStringAsync() : null;

        var totalLength = (requestContent?.Length ?? 0) + (responseContent?.Length ?? 0);
        string? artifactUrl = null;

        if (totalLength > PayloadAttachmentThreshold)
            artifactUrl = await SavePayloadAsArtifactAsync(request, requestContent, response, responseContent, durationMs);

        var inlineRequest = Truncate(requestContent, artifactUrl != null ? InlineMaxLength : PayloadAttachmentThreshold);
        var inlineResponse = Truncate(responseContent, artifactUrl != null ? InlineMaxLength : PayloadAttachmentThreshold);

        var payload = new BsonDocument
        {
            { "httpMethod", request.Method.ToString() },
            { "requestUri", request.RequestUri?.ToString() ?? "" },
            { "statusCode", (int)response.StatusCode },
            { "durationMs", durationMs },
            { "requestHeaders", CollectHeaders(request.Headers, request.Content?.Headers) },
            { "responseHeaders", CollectHeaders(response.Headers, response.Content?.Headers) },
            { "requestContent", inlineRequest ?? BsonString.Empty },
            { "responseContent", inlineResponse ?? BsonString.Empty }
        };

        if (artifactUrl != null)
            payload.Add("payloadArtifactUrl", artifactUrl);

        BufferEntry(LogLevel.Info, "ApiCall",
            $"API Call: {request.Method} {request.RequestUri} - Status: {response.StatusCode} - Duration: {durationMs}ms",
            payload);
    }

    private static string? Truncate(string? value, int maxLength)
    {
        if (value == null) return null;
        return value.Length <= maxLength ? value : value[..maxLength] + "... [TRUNCATED]";
    }

    /// <summary>
    /// Header names known to carry a credential. An explicit list is exact but has to be
    /// maintained, so it is only the first of three checks in <see cref="Redact"/>.
    /// </summary>
    private static readonly HashSet<string> SensitiveHeaders = new(StringComparer.OrdinalIgnoreCase)
    {
        "Authorization", "X-Api-Key", "X-Bolt-ApiKey", "X-Agent-Identity", "X-API-Source",
        "X-Twilio-Signature", "Cookie", "Set-Cookie", "Ocp-Apim-Subscription-Key"
    };

    /// <summary>
    /// Name fragments that mark a header as carrying a credential. Most headers this repo sends
    /// are bare tokens with no auth scheme to recognise, so matching the name is what actually
    /// covers a header added later; the explicit list above is then only a fast path for the
    /// names we already know. Over-redacting a header is cheap, leaking one is not.
    /// </summary>
    private static readonly string[] SensitiveNameFragments =
        { "key", "token", "secret", "auth", "identity", "signature", "cookie", "password", "credential" };

    /// <summary>
    /// Redacts a header value by name (<see cref="SensitiveHeaders"/>), by name fragment
    /// (<see cref="SensitiveNameFragments"/>), or by shape — any value carrying an HTTP auth
    /// scheme, which covers a credential sent under a name none of the above anticipated.
    /// </summary>
    private static string Redact(string key, string value) =>
        SensitiveHeaders.Contains(key)
        || SensitiveNameFragments.Any(fragment => key.Contains(fragment, StringComparison.OrdinalIgnoreCase))
        || value.StartsWith("Basic ", StringComparison.OrdinalIgnoreCase)
        || value.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
            ? "[REDACTED]"
            : value;

    private static BsonDocument CollectHeaders(params System.Net.Http.Headers.HttpHeaders?[] headerSets)
    {
        var doc = new BsonDocument();
        foreach (var headers in headerSets)
        {
            if (headers == null) continue;
            foreach (var (key, values) in headers)
            {
                var value = string.Join(", ", values);
                doc[key] = Redact(key, value);
            }
        }
        return doc;
    }

    private static Dictionary<string, string> CollectHeadersDictionary(params System.Net.Http.Headers.HttpHeaders?[] headerSets)
    {
        var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var headers in headerSets)
        {
            if (headers == null) continue;
            foreach (var (key, values) in headers)
            {
                var value = string.Join(", ", values);
                dict[key] = Redact(key, value);
            }
        }
        return dict;
    }

    private async Task<string?> SavePayloadAsArtifactAsync(
        HttpRequestMessage request, string? requestContent,
        HttpResponseMessage response, string? responseContent,
        long durationMs)
    {
        // Skip artifact uploads during debugging
        if (Debugger.IsAttached)
        {
            return null;
        }

        var testId = _testId;
        if (string.IsNullOrEmpty(testId))
            return null;

        string? tempPath = null;
        try
        {
            var artifact = new
            {
                httpMethod = request.Method.ToString(),
                requestUri = request.RequestUri?.ToString(),
                statusCode = (int)response.StatusCode,
                durationMs,
                requestHeaders = CollectHeadersDictionary(request.Headers, request.Content?.Headers),
                responseHeaders = CollectHeadersDictionary(response.Headers, response.Content?.Headers),
                requestContent,
                responseContent
            };

            var json = JsonSerializer.Serialize(artifact, new JsonSerializerOptions { WriteIndented = true });
            var fileName = $"api_{request.Method}_{DateTime.UtcNow:yyyyMMdd_HHmmss_fff}.json";
            tempPath = Path.Combine(Path.GetTempPath(), fileName);
            await File.WriteAllTextAsync(tempPath, json);

            return await _writer.UploadAndAddArtifactAsync(testId, tempPath, fileName, ArtifactType.ApiPayload);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[MongoReporting] WARNING: Failed to save API payload artifact: {ex.Message}");
            return null;
        }
        finally
        {
            if (tempPath != null && File.Exists(tempPath))
            {
                try { File.Delete(tempPath); } catch { /* best-effort cleanup */ }
            }
        }
    }

    public void LogUiAction(string actionType, string element, string? details = null)
    {
        _inner.LogUiAction(actionType, element, details);

        var payload = new BsonDocument
        {
            { "actionType", actionType },
            { "element", element },
            { "details", details ?? BsonString.Empty },
            { "capturedAt", DateTime.UtcNow }
        };
        BufferEntry(LogLevel.Info, "UiAction",
            $"UI Action [{actionType}] on {element}" + (details != null ? $": {details}" : ""),
            payload);
    }

    public void LogUiAction(string actionType, string element, MongoDB.Bson.BsonDocument? payload = null)
    {
        if (payload == null)
            payload = new BsonDocument { { "capturedAt", DateTime.UtcNow } };
        else if (!payload.Contains("capturedAt"))
            payload.Add("capturedAt", DateTime.UtcNow);

        _inner.LogUiAction(actionType, element, payload);
        BufferEntry(LogLevel.Info, "UiAction",
            $"UI Action [{actionType}] on {element}",
            payload);
    }

    public void LogDataValidation(string validationType, bool passed, string expected, string actual, string? details = null)
    {
        _inner.LogDataValidation(validationType, passed, expected, actual, details);

        var payload = new BsonDocument
        {
            { "validationType", validationType },
            { "passed", passed },
            { "expected", expected },
            { "actual", actual },
            { "details", details ?? BsonString.Empty }
        };
        BufferEntry(passed ? LogLevel.Info : LogLevel.Error, "DataValidation",
            $"Validation [{validationType}] {(passed ? "PASSED" : "FAILED")} - Expected: {expected}, Actual: {actual}",
            payload);
    }

    // --- JSON logging ---

    public void LogJson(string message, object data, LogLevel level = LogLevel.Debug)
    {
        _inner.LogJson(message, data, level);
        var json = data != null ? JsonSerializer.Serialize(data) : "null";
        var payload = new BsonDocument { { "data", json } };
        BufferEntry(level, "JsonData", message, payload);
    }

    public void LogJsonWithPreview(string message, object data, LogLevel level = LogLevel.Debug)
    {
        _inner.LogJsonWithPreview(message, data, level);
        var json = data != null ? JsonSerializer.Serialize(data) : "null";
        var payload = new BsonDocument { { "data", json } };
        BufferEntry(level, "JsonData", message, payload);
    }

    public void LogJsonStyled(string message, object data, LogLevel level = LogLevel.Debug)
    {
        _inner.LogJsonStyled(message, data, level);
        var json = data != null ? JsonSerializer.Serialize(data) : "null";
        var payload = new BsonDocument { { "data", json } };
        BufferEntry(level, "JsonData", message, payload);
    }

    // --- Helpers ---

    /// <summary>
    ///     Applies the args the inner logger formats with, so the persisted copy does not show a raw
    ///     <c>{0}</c>. Falls back to the template rather than throwing from inside a log call.
    /// </summary>
    private static string Format(string message, object[]? args)
    {
        if (args is null || args.Length == 0 || string.IsNullOrEmpty(message))
        {
            return message;
        }

        try
        {
            return string.Format(message, args);
        }
        catch (FormatException)
        {
            return message;
        }
    }

    private int _nullTestIdWarningCount;

    private void BufferEntry(LogLevel level, string category, string message, BsonDocument? payload = null)
    {
        // Skip MongoDB logging during debugging to avoid polluting the database
        if (Debugger.IsAttached)
        {
            return;
        }

        var testId = _testId;
        if (string.IsNullOrEmpty(testId))
        {
            if (Interlocked.Increment(ref _nullTestIdWarningCount) == 1)
                Console.WriteLine($"[MongoReporting] WARNING: _testId is null — MongoDB log entries will not be buffered. First dropped message: {message}");
            return;
        }

        var depth = _openSteps.Value?.Count ?? 0;

        _writer.BufferLog(testId, new LogEntryDocument
        {
            Timestamp = DateTime.UtcNow,
            Level = level.ToString(),
            Category = category,
            Message = message,
            StepName = depth > 0 ? _openSteps.Value!.Peek().Name : null,
            StepLevel = depth > 0 ? depth - 1 : null,
            Payload = payload
        });
    }
}
