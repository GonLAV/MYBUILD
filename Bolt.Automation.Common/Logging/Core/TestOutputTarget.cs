using NLog;
using NLog.Targets;

namespace Bolt.Automation.Common.Logging.Core
{
    [Target("TestOutput")]
    public class TestOutputTarget : TargetWithLayout
    {
        private static readonly AsyncLocal<TextWriter?> _currentWriter = new();

        public static void SetTestOutput(TextWriter? writer)
        {
            _currentWriter.Value = writer;
        }

        public static void ClearTestOutput()
        {
            _currentWriter.Value = null;
        }

        protected override void Write(LogEventInfo logEvent)
        {
            var writer = _currentWriter.Value;
            if (writer != null)
            {
                try
                {
                    var logMessage = Layout.Render(logEvent);
                    writer.WriteLine(logMessage);
                }
                catch
                {
                    // Ignore any exceptions during test output writing
                }
            }
        }
    }
}