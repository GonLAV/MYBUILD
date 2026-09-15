using Bolt.Automation.Common.Logging.Core;
using Microsoft.Extensions.DependencyInjection;
using NLog;
using NLog.Config;
using NLog.Targets;
using LogLevel = NLog.LogLevel;

namespace Bolt.Automation.Common.Logging.Extentions
{
    public static class TestLoggingExtensions
    {
        public static IServiceCollection AddNLogWithTestOutputTarget(this IServiceCollection services)
        {
            InitializeNLogWithTestOutput();
            return services;
        }

        private static readonly object _nlogLock = new object();
        private static bool _nlogInitialized;

        private static void InitializeNLogWithTestOutput()
        {
            lock (_nlogLock)
            {
                if (!_nlogInitialized)
                {
                    var configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "nlog.config");
                    if (File.Exists(configPath))
                    {
                        LogManager.LoadConfiguration(configPath);
                        var config = LogManager.Configuration;
                        if (config != null && config.AllTargets.All(t => t.Name != "testoutput"))
                        {
                            AddTestOutputTarget(config);
                            LogManager.Configuration = config;
                        }
                    }
                    else
                    {
                        var config = new LoggingConfiguration();
                        AddTestOutputTarget(config);
                        var consoleTarget = new ColoredConsoleTarget("console")
                        {
                            Layout = "${level:uppercase=true} | ${logger} | ${message} ${exception:format=tostring}"
                        };
                        config.AddTarget(consoleTarget);
                        config.AddRuleForAllLevels(consoleTarget);
                        var debugTarget = new DebuggerTarget("debug")
                        {
                            Layout = "${level:uppercase=true} | ${logger} | ${message} ${exception:format=tostring}"
                        };
                        config.AddTarget(debugTarget);
                        config.AddRuleForAllLevels(debugTarget);
                        LogManager.Configuration = config;
                    }
                    _nlogInitialized = true;
                }
            }
        }

        private static void AddTestOutputTarget(LoggingConfiguration config)
        {
            var testOutputTarget = new TestOutputTarget
            {
                Name = "testoutput",
                Layout = "${level:uppercase=true} | ${logger} | ${message} ${exception:format=tostring}"
            };
            config.AddTarget(testOutputTarget);
            config.AddRule(LogLevel.Debug, LogLevel.Fatal, testOutputTarget);
        }
    }
}

