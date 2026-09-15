// Bolt.Automation.Common/Logging/AutomationLoggerAdapter.cs

using Bolt.Automation.Common.Logging.Core;
using Microsoft.Extensions.Logging;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;

namespace Bolt.Automation.Common.Logging.Adapters
{
    /// <summary>
    /// Adapter to use IAutomationLogger with Microsoft.Extensions.Logging.ILogger{T}
    /// </summary>
    public class AutomationLoggerAdapter<T> : ILogger<T>
    {
        private readonly IAutomationLogger _automationLogger;
        private readonly string _categoryName;

        public AutomationLoggerAdapter(IAutomationLogger automationLogger)
        {
            _automationLogger = automationLogger ?? throw new ArgumentNullException(nameof(automationLogger));
            _categoryName = typeof(T).FullName ?? typeof(T).Name;
        }

        public IDisposable BeginScope<TState>(TState state)
        {
            // If your IAutomationLogger supports scopes (like StartStep), 
            // you could implement this. For now, return a no-op scope.
            return NoOpDisposable.Instance;
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            // You could make this configurable based on your automation logger's settings
            return true;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId,
            TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            if (!IsEnabled(logLevel))
                return;

            var message = formatter?.Invoke(state, exception);
            if (string.IsNullOrEmpty(message) && exception == null)
                return;

            // Prepend category name for better context
            var formattedMessage = $"[{_categoryName}] {message}";

            switch (logLevel)
            {
                case LogLevel.Trace:
                    _automationLogger.Trace(formattedMessage);
                    break;
                case LogLevel.Debug:
                    _automationLogger.Debug(formattedMessage);
                    break;
                case LogLevel.Information:
                    _automationLogger.Info(formattedMessage);
                    break;
                case LogLevel.Warning:
                    _automationLogger.Warning(formattedMessage);
                    break;
                case LogLevel.Error:
                    if (exception != null)
                        _automationLogger.LogException(exception, formattedMessage);
                    else
                        _automationLogger.Error(formattedMessage);
                    break;
                case LogLevel.Critical:
                    if (exception != null)
                        _automationLogger.Fatal($"{formattedMessage} - Exception: {exception}");
                    else
                        _automationLogger.Fatal(formattedMessage);
                    break;
                default:
                    _automationLogger.Info(formattedMessage);
                    break;
            }
        }

        private class NoOpDisposable : IDisposable
        {
            public static readonly NoOpDisposable Instance = new NoOpDisposable();
            public void Dispose() { }
        }
    }
}