namespace Bolt.Automation.Common.Logging.Core;

/// <summary>
///     Interface for the automation logger
/// </summary>
public interface IAutomationLogger
{
    void Log(LogLevel level, string message, params object[] args);
    void Info(string message, params object[] args);
    void Debug(string message, params object[] args);
    void Trace(string message, params object[] args);
    void Warning(string message, params object[] args);
    void Error(string message, params object[] args);
    void Fatal(string message, params object[] args);
    void LogException(Exception exception, string? message = null, params object[] args);

    IStepScope StartStep(string stepName, string? description = null);
    void EndStep(StepStatus status = StepStatus.Passed);
    void AttachFile(string filePath, string? description = null);

    // Specialized logging methods
    void LogBusinessRule(string ruleName, bool passed, string? details = null);
    Task LogApiCallAsync(HttpRequestMessage request, HttpResponseMessage response, long durationMs);
    void LogUiAction(string actionType, string element, string? details = null);
    // Overload to allow structured BsonDocument payloads for richer UI action logging
    void LogUiAction(string actionType, string element, MongoDB.Bson.BsonDocument? payload = null);
    void LogDataValidation(string validationType, bool passed, string expected, string actual, string? details = null);

    // JSON logging methods
    void LogJson(string message, object data, LogLevel level = LogLevel.Debug);
    void LogJsonWithPreview(string message, object data, LogLevel level = LogLevel.Debug);
    void LogJsonStyled(string message, object data, LogLevel level = LogLevel.Debug);
}

/// <summary>
///     Represents a step scope that tracks pass/fail status.
///     On Dispose, defaults to Failed if neither Complete() nor Fail() was called,
///     ensuring exceptions inside a using block are correctly recorded.
/// </summary>
public interface IStepScope : IDisposable
{
    /// <summary>Marks the step as passed. Call at the end of a successful block.</summary>
    void Complete(string? details = null);

    /// <summary>Marks the step as failed with an optional reason.</summary>
    void Fail(string? reason = null);

    /// <summary>Marks the step as skipped.</summary>
    void Skip(string? reason = null);
}

/// <summary>
///     Log level enumeration
/// </summary>
public enum LogLevel
{
    Trace,
    Debug,
    Info,
    Warning,
    Error,
    Fatal
}

/// <summary>
///     Step status enumeration
/// </summary>
public enum StepStatus
{
    Passed,
    Failed,
    Skipped,
    Blocked,
    Warning
}