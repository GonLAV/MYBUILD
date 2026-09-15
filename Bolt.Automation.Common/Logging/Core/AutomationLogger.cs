using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text;
using Bolt.Automation.Common.Utils;
using MongoDB.Bson;
using NLog;

namespace Bolt.Automation.Common.Logging.Core;

/// <summary>
///     logging service
/// </summary>
public class AutomationLogger : IAutomationLogger
{
    #region Constructor

    /// <summary>
    ///     Initializes a new instance of the AutomationLogger class
    /// </summary>
    /// <param name="context">The logging context (typically class name)</param>
    public AutomationLogger(string context)
    {
        _context = context;
        _logger = _loggers.GetOrAdd(context, LogManager.GetLogger(context));
    }

    #endregion

    #region File Attachments

    /// <summary>
    ///     Attaches a file to the log
    /// </summary>
    public void AttachFile(string filePath, string? description = null)
    {
        if (File.Exists(filePath))
        {
            var message =
                $"FILE_ATTACHMENT: {filePath}{(!string.IsNullOrEmpty(description) ? " - " + description : "")}";

            var logEvent = new LogEventInfo(NLog.LogLevel.Info, _logger.Name, message);
            logEvent.Properties["Category"] = "Attachment";
            logEvent.Properties["FilePath"] = filePath;
            logEvent.Properties["Description"] = description;

            if (StepStack.Count > 0)
            {
                var currentStep = StepStack.Peek();
                logEvent.Properties["StepName"] = currentStep.Name;
                logEvent.Properties["StepLevel"] = StepStack.Count;
            }

            _logger.Log(logEvent);
        }
        else
        {
            Log(LogLevel.Warning, $"Failed to attach file - file not found: {filePath}");
        }
    }

    #endregion

    #region Private Fields and Properties

    private static readonly ConcurrentDictionary<string, Logger> _loggers = new();
    private readonly Logger _logger;
    private readonly string _context;
    private readonly AsyncLocal<Stack<StepInfo>> _stepStack = new();

    private Stack<StepInfo> StepStack => _stepStack.Value ??= new Stack<StepInfo>();

    #endregion

    #region Basic Logging

    /// <summary>
    ///     Logs a message with the specified level
    /// </summary>
    public void Log(LogLevel level, string message, params object[] args)
    {
        var formattedMessage = FormatMessage(message);

        var logEvent = new LogEventInfo(ConvertToNLogLevel(level), _logger.Name, null, formattedMessage, args);

        if (StepStack.Count > 0)
        {
            var currentStep = StepStack.Peek();
            logEvent.Properties["StepName"] = currentStep.Name;
            logEvent.Properties["StepLevel"] = StepStack.Count;
            logEvent.Properties["Category"] = "Step";
        }

        _logger.Log(logEvent);
    }

    private static NLog.LogLevel ConvertToNLogLevel(LogLevel level) => level switch
    {
        LogLevel.Trace   => NLog.LogLevel.Trace,
        LogLevel.Debug   => NLog.LogLevel.Debug,
        LogLevel.Info    => NLog.LogLevel.Info,
        LogLevel.Warning => NLog.LogLevel.Warn,
        LogLevel.Error   => NLog.LogLevel.Error,
        LogLevel.Fatal   => NLog.LogLevel.Fatal,
        _                => NLog.LogLevel.Info
    };

    /// <summary>
    ///     Formats the message to include step indentation
    /// </summary>
    private string FormatMessage(string message)
    {
        if (StepStack.Count > 0)
        {
            return $"{new string(' ', StepStack.Count * 2)}{message}";
        }

        return message;
    }

    /// <summary>
    ///     Info level logging
    /// </summary>
    public void Info(string message, params object[] args)
    {
        Log(LogLevel.Info, message, args);
    }

    /// <summary>
    /// Logs a UI action with simple details string
    /// </summary>
    public void LogUiAction(string actionType, string element, string? details = null)
    {
        var uiLogger = LogManager.GetLogger($"UiLog.{_context}");

        var logEvent = new LogEventInfo(NLog.LogLevel.Info, uiLogger.Name,
            $"UI Action [{actionType}] on {element}{(details != null ? ": " + details : "")}" );

        logEvent.Properties["Category"] = "UiAction";
        logEvent.Properties["ActionType"] = actionType;
        logEvent.Properties["Element"] = element;
        logEvent.Properties["Details"] = details;

        if (StepStack.Count > 0)
        {
            var currentStep = StepStack.Peek();
            logEvent.Properties["StepName"] = currentStep.Name;
            logEvent.Properties["StepLevel"] = StepStack.Count;
        }

        uiLogger.Log(logEvent);
    }

    /// <summary>
    /// Logs a UI action with a structured BsonDocument payload
    /// </summary>
    public void LogUiAction(string actionType, string element, BsonDocument? payload = null)
    {
        var uiLogger = LogManager.GetLogger($"UiLog.{_context}");

        var message = $"UI Action [{actionType}] on {element}";

        var logEvent = new LogEventInfo(NLog.LogLevel.Info, uiLogger.Name, message);
        logEvent.Properties["Category"] = "UiAction";
        logEvent.Properties["ActionType"] = actionType;
        logEvent.Properties["Element"] = element;
        logEvent.Properties["Payload"] = payload?.ToJson() ?? string.Empty;

        if (StepStack.Count > 0)
        {
            var currentStep = StepStack.Peek();
            logEvent.Properties["StepName"] = currentStep.Name;
            logEvent.Properties["StepLevel"] = StepStack.Count;
        }

        uiLogger.Log(logEvent);
    }

    /// <summary>
    ///     Debug level logging
    /// </summary>
    public void Debug(string message, params object[] args)
    {
        Log(LogLevel.Debug, message, args);
    }

    /// <summary>
    ///     Trace level logging
    /// </summary>
    public void Trace(string message, params object[] args)
    {
        Log(LogLevel.Trace, message, args);
    }

    /// <summary>
    ///     Warning level logging
    /// </summary>
    public void Warning(string message, params object[] args)
    {
        Log(LogLevel.Warning, message, args);
    }

    /// <summary>
    ///     Error level logging
    /// </summary>
    public void Error(string message, params object[] args)
    {
        Log(LogLevel.Error, message, args);
    }

    /// <summary>
    ///     Fatal level logging
    /// </summary>
    public void Fatal(string message, params object[] args)
    {
        Log(LogLevel.Fatal, message, args);
    }

    /// <summary>
    ///     Logs an exception at Error level
    /// </summary>
    public void LogException(Exception exception, string? message = null, params object[] args)
    {
        var formattedMessage = string.IsNullOrEmpty(message)
            ? $"Exception: {exception.Message}"
            : $"{string.Format(message, args)} - Exception: {exception.Message}";

        var logEvent = new LogEventInfo(NLog.LogLevel.Error, _logger.Name, formattedMessage)
        {
            Exception = exception
        };
        logEvent.Properties["Category"] = "Exception";
        logEvent.Properties["ExceptionType"] = exception.GetType().Name;

        if (StepStack.Count > 0)
        {
            var currentStep = StepStack.Peek();
            logEvent.Properties["StepName"] = currentStep.Name;
            logEvent.Properties["StepLevel"] = StepStack.Count;
        }

        _logger.Log(logEvent);
    }

    #endregion

    #region Step Management

    /// <summary>
    ///     Starts a new step with the specified name
    /// </summary>
    public IStepScope StartStep(string stepName, string? description = null)
    {
        var stepInfo = new StepInfo(stepName, Stopwatch.StartNew());
        StepStack.Push(stepInfo);

        if (string.IsNullOrEmpty(description))
            Log(LogLevel.Info, $"Starting step: {stepName}");
        else
            Log(LogLevel.Info, $"Starting step: {stepName} - {description}");

        return new StepScope(this, stepName);
    }

    /// <summary>
    ///     Ends the current step
    /// </summary>
    public void EndStep(StepStatus status = StepStatus.Passed)
    {
        if (StepStack.Count > 0)
        {
            var stepInfo = StepStack.Pop();
            stepInfo.Stopwatch.Stop();

            var logEvent = new LogEventInfo(NLog.LogLevel.Info, _logger.Name,
                $"Completed step: {stepInfo.Name} - {status} (Duration: {stepInfo.Stopwatch.ElapsedMilliseconds}ms)");

            logEvent.Properties["StepName"] = stepInfo.Name;
            logEvent.Properties["StepLevel"] = StepStack.Count;
            logEvent.Properties["Duration"] = stepInfo.Stopwatch.ElapsedMilliseconds;
            logEvent.Properties["StepStatus"] = status.ToString();
            logEvent.Properties["Category"] = "StepCompletion";

            _logger.Log(logEvent);
        }
    }

    #endregion

    #region Specialized Logging

    /// <summary>
    ///     Logs business logic validations
    /// </summary>
    public void LogBusinessRule(string ruleName, bool passed, string? details = null)
    {
        var businessLogger = LogManager.GetLogger($"BusinessLogic.{_context}");
        var status = passed ? "PASSED" : "FAILED";

        var logEvent = new LogEventInfo(passed ? NLog.LogLevel.Info : NLog.LogLevel.Error,
            businessLogger.Name, $"Business Rule [{ruleName}] {status}: {details}");

        logEvent.Properties["Category"] = "BusinessRule";
        logEvent.Properties["RuleName"] = ruleName;
        logEvent.Properties["RuleStatus"] = status;

        if (StepStack.Count > 0)
        {
            var currentStep = StepStack.Peek();
            logEvent.Properties["StepName"] = currentStep.Name;
            logEvent.Properties["StepLevel"] = StepStack.Count;
        }

        businessLogger.Log(logEvent);
    }

    /// <summary>
    ///     Logs API request and response details
    /// </summary>
    public async Task LogApiCallAsync(HttpRequestMessage request, HttpResponseMessage response, long durationMs)
    {
        var apiLogger = LogManager.GetLogger($"ApiLog.{_context}");

        var requestContent = request.Content != null ? await request.Content.ReadAsStringAsync() : "[No Content]";

        var responseContent = response.Content != null ? await response.Content.ReadAsStringAsync() : "[No Content]";

        // Truncate content if too large
        const int maxContentLength = 10000;
        if (requestContent.Length > maxContentLength)
            requestContent = requestContent.Substring(0, maxContentLength) + "... [TRUNCATED]";

        if (responseContent.Length > maxContentLength)
            responseContent = responseContent.Substring(0, maxContentLength) + "... [TRUNCATED]";

        var logEvent = new LogEventInfo(NLog.LogLevel.Info, apiLogger.Name,
            $"API Call: {request.Method} {request.RequestUri} - Status: {response.StatusCode} - Duration: {durationMs}ms");

        logEvent.Properties["Category"] = "ApiCall";
        logEvent.Properties["HttpMethod"] = request.Method.ToString();
        logEvent.Properties["RequestUri"] = request.RequestUri.ToString();
        logEvent.Properties["StatusCode"] = (int)response.StatusCode;
        logEvent.Properties["Duration"] = durationMs;
        logEvent.Properties["RequestContent"] = requestContent;
        logEvent.Properties["ResponseContent"] = responseContent;

        if (StepStack.Count > 0)
        {
            var currentStep = StepStack.Peek();
            logEvent.Properties["StepName"] = currentStep.Name;
            logEvent.Properties["StepLevel"] = StepStack.Count;
        }

        apiLogger.Log(logEvent);
    }



    /// <summary>
    ///     Logs data validation results
    /// </summary>
    public void LogDataValidation(string validationType, bool passed, string expected, string actual,
        string? details = null)
    {
        var dataLogger = LogManager.GetLogger($"DataValidation.{_context}");
        var status = passed ? "PASSED" : "FAILED";

        var message = new StringBuilder();
        message.Append($"Data Validation [{validationType}] {status}");

        if (!string.IsNullOrEmpty(expected))
            message.Append($" - Expected: {expected}");

        if (!string.IsNullOrEmpty(actual))
            message.Append($" - Actual: {actual}");

        if (!string.IsNullOrEmpty(details))
            message.Append($" - Details: {details}");

        var logEvent = new LogEventInfo(passed ? NLog.LogLevel.Info : NLog.LogLevel.Error,
            dataLogger.Name, message.ToString());

        logEvent.Properties["Category"] = "DataValidation";
        logEvent.Properties["ValidationType"] = validationType;
        logEvent.Properties["ValidationStatus"] = status;
        logEvent.Properties["Expected"] = expected;
        logEvent.Properties["Actual"] = actual;

        if (StepStack.Count > 0)
        {
            var currentStep = StepStack.Peek();
            logEvent.Properties["StepName"] = currentStep.Name;
            logEvent.Properties["StepLevel"] = StepStack.Count;
        }

        dataLogger.Log(logEvent);
    }

    #endregion

    #region JSON Logging

    /// <summary>
    ///     Logs JSON data with collapsible HTML details
    /// </summary>
    public void LogJson(string message, object data, LogLevel level = LogLevel.Debug)
    {
        if (data == null)
        {
            Log(level, $"{message}: [null]");
            return;
        }

        try
        {
            // Use compact JSON formatting
            var json = CompactJsonFormatter.Format(data);

            // Truncate if too large
            if (json.Length > JsonLoggingSettings.MaxJsonLength)
            {
                json = json.Substring(0, JsonLoggingSettings.MaxJsonLength) + "... [TRUNCATED]";
            }

            var htmlJson = HtmlEncoder.Encode(json);
            var logMessage = $"{message}: <details><summary>📋 Click to expand JSON</summary><pre style='white-space: pre-wrap; word-break: break-all;'>{htmlJson}</pre></details>";

            var logEvent = new LogEventInfo(ConvertToNLogLevel(level), _logger.Name, logMessage);
            logEvent.Properties["Category"] = "JsonData";
            logEvent.Properties["DataType"] = data.GetType().Name;

            if (StepStack.Count > 0)
            {
                var currentStep = StepStack.Peek();
                logEvent.Properties["StepName"] = currentStep.Name;
                logEvent.Properties["StepLevel"] = StepStack.Count;
            }

            _logger.Log(logEvent);
        }
        catch (Exception ex)
        {
            LogException(ex, "Failed to serialize object for JSON logging");
            Log(level, $"{message}: [Serialization failed: {data.GetType().Name}]");
        }
    }

    /// <summary>
    ///     Logs JSON data with preview in summary
    /// </summary>
    public void LogJsonWithPreview(string message, object data, LogLevel level = LogLevel.Debug)
    {
        if (data == null)
        {
            Log(level, $"{message}: [null]");
            return;
        }

        try
        {
            // Use compact JSON formatting
            var json = CompactJsonFormatter.Format(data);
            var preview = CompactJsonFormatter.CreatePreview(data, JsonLoggingSettings.MaxPreviewLength);

            // Truncate JSON if too large
            if (json.Length > JsonLoggingSettings.MaxJsonLength)
            {
                json = json.Substring(0, JsonLoggingSettings.MaxJsonLength) + "... [TRUNCATED]";
            }

            var htmlJson = HtmlEncoder.Encode(json);
            var htmlPreview = HtmlEncoder.Encode(preview);
            var logMessage = $"{message}: <details><summary>{htmlPreview}</summary><pre style='white-space: pre-wrap; word-break: break-all;'>{htmlJson}</pre></details>";

            var logEvent = new LogEventInfo(ConvertToNLogLevel(level), _logger.Name, logMessage);
            logEvent.Properties["Category"] = "JsonData";
            logEvent.Properties["DataType"] = data.GetType().Name;
            logEvent.Properties["Preview"] = preview;

            if (StepStack.Count > 0)
            {
                var currentStep = StepStack.Peek();
                logEvent.Properties["StepName"] = currentStep.Name;
                logEvent.Properties["StepLevel"] = StepStack.Count;
            }

            _logger.Log(logEvent);
        }
        catch (Exception ex)
        {
            LogException(ex, "Failed to serialize object for JSON logging with preview");
            Log(level, $"{message}: [Serialization failed: {data.GetType().Name}]");
        }
    }

    /// <summary>
    ///     Logs JSON data with enhanced HTML styling
    /// </summary>
    public void LogJsonStyled(string message, object data, LogLevel level = LogLevel.Debug)
    {
        if (data == null)
        {
            Log(level, $"{message}: [null]");
            return;
        }

        try
        {
            // Use compact JSON formatting
            var json = CompactJsonFormatter.Format(data);

            // Truncate if too large
            if (json.Length > JsonLoggingSettings.MaxJsonLength)
            {
                json = json.Substring(0, JsonLoggingSettings.MaxJsonLength) + "... [TRUNCATED]";
            }

            var htmlJson = HtmlEncoder.Encode(json);
            var dataSummary = GetDataSummary(data);

            string logMessage;
            if (JsonLoggingSettings.EnableStyling)
            {
                logMessage = $"{message}: <details style='margin: 5px 0; border: 1px solid #ddd; border-radius: 4px;'><summary style='cursor: pointer; padding: 8px; background: #f5f5f5; font-weight: bold; border-radius: 4px 4px 0 0;'>📋 JSON Data ({dataSummary})</summary><pre style='margin: 0; padding: 10px; background: #fafafa; overflow-x: auto; font-family: monospace; border-top: 1px solid #ddd; white-space: pre-wrap; word-break: break-all;'>{htmlJson}</pre></details>";
            }
            else
            {
                logMessage = $"{message}: <details><summary>📋 JSON Data ({dataSummary})</summary><pre style='white-space: pre-wrap; word-break: break-all;'>{htmlJson}</pre></details>";
            }

            var logEvent = new LogEventInfo(ConvertToNLogLevel(level), _logger.Name, logMessage);
            logEvent.Properties["Category"] = "JsonData";
            logEvent.Properties["DataType"] = data.GetType().Name;
            logEvent.Properties["Summary"] = dataSummary;

            if (StepStack.Count > 0)
            {
                var currentStep = StepStack.Peek();
                logEvent.Properties["StepName"] = currentStep.Name;
                logEvent.Properties["StepLevel"] = StepStack.Count;
            }

            _logger.Log(logEvent);
        }
        catch (Exception ex)
        {
            LogException(ex, "Failed to serialize object for styled JSON logging");
            Log(level, $"{message}: [Serialization failed: {data.GetType().Name}]");
        }
    }

    /// <summary>
    ///     Gets a summary description of the data object
    /// </summary>
    private string GetDataSummary(object data)
    {
        if (data == null) return "null";

        var type = data.GetType();
        
        if (type.IsArray)
        {
            var array = (Array)data;
            return $"{type.GetElementType()?.Name}[{array.Length}]";
        }
        
        if (data is System.Collections.ICollection collection)
        {
            return $"{type.Name}[{collection.Count}]";
        }
        
        return type.Name;
    }

    #endregion
}

#region Supporting Classes

/// <summary>
///     Information about a step
/// </summary>
internal class StepInfo
{
    public StepInfo(string name, Stopwatch stopwatch)
    {
        Name = name;
        Stopwatch = stopwatch;
    }

    public string Name { get; }
    public Stopwatch Stopwatch { get; }
}

/// <summary>
///     Disposable scope for steps that tracks pass/fail status.
///     Defaults to Failed on Dispose if neither Complete() nor Fail() was called,
///     ensuring exceptions inside a using block are correctly recorded.
/// </summary>
internal class StepScope : IStepScope
{
    private readonly AutomationLogger _logger;
    private readonly string _stepName;
    private bool _disposed;
    private StepStatus? _explicitStatus;

    public StepScope(AutomationLogger logger, string stepName)
    {
        _logger = logger;
        _stepName = stepName;
    }

    public void Complete(string? details = null)
    {
        _explicitStatus = StepStatus.Passed;
        if (!string.IsNullOrEmpty(details))
            _logger.Debug($"Step '{_stepName}' completed: {details}");
    }

    public void Fail(string? reason = null)
    {
        _explicitStatus = StepStatus.Failed;
        if (!string.IsNullOrEmpty(reason))
            _logger.Error($"Step '{_stepName}' failed: {reason}");
    }

    public void Skip(string? reason = null)
    {
        _explicitStatus = StepStatus.Skipped;
        if (!string.IsNullOrEmpty(reason))
            _logger.Warning($"Step '{_stepName}' skipped: {reason}");
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            // Default to Failed if no explicit status — safe for exception scenarios
            _logger.EndStep(_explicitStatus ?? StepStatus.Failed);
            _disposed = true;
        }
    }
}

#endregion