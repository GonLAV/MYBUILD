using System.Collections.Concurrent;

namespace Bolt.Automation.Common.Logging.Core
{
    public static class LoggerFactory
    {
        private static readonly ConcurrentDictionary<string, IAutomationLogger> _loggers = new();

        public static IAutomationLogger CreateLogger(string context)
        {
            return _loggers.GetOrAdd(context, ctx => new AutomationLogger(ctx));
        }

        public static void SetTestOutput(TextWriter? writer)
        {
            TestOutputTarget.SetTestOutput(writer);
        }

        public static void ClearTestOutput()
        {
            TestOutputTarget.ClearTestOutput();
        }
    }
}