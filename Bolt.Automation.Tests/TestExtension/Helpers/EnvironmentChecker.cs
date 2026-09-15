using System.Reflection;
using Bolt.Automation.Tests.TestExtension.Attributes;
using NUnit.Framework;
using BoltEnvironment = Bolt.Automation.Common.Environment;

namespace Bolt.Automation.Tests.TestExtension.Helpers
{
    public static class EnvironmentChecker
    {
        public static void ValidateTestExecution(Type testClass, BoltEnvironment currentEnvironment)
        {
            var testMethod = GetTestMethod(testClass);
            var runInAttr = testMethod?.GetCustomAttribute<RunInAttribute>() ?? testClass.GetCustomAttribute<RunInAttribute>();
            var notRunInAttrs = (testMethod?.GetCustomAttributes<NotRunInAttribute>() ?? Array.Empty<NotRunInAttribute>())
                                .Concat(testClass.GetCustomAttributes<NotRunInAttribute>())
                                .ToArray();

            if (!ShouldRunInEnvironment(runInAttr, notRunInAttrs, currentEnvironment))
            {
                Assert.Inconclusive($"Test skipped: Not configured to run in {currentEnvironment} environment");
            }
        }

        private static bool ShouldRunInEnvironment(RunInAttribute? runIn, NotRunInAttribute[] notRunIns, BoltEnvironment currentEnv)
        {
            // Check NotRunIn first (blacklist takes precedence)
            if (notRunIns.Any(a => a.Environments.Contains(currentEnv)))
            {
                return false;
            }

            // If RunIn specifies exact environments, allow only those
            if (runIn != null && runIn.Environments.Length > 0)
            {
                return runIn.Environments.Contains(currentEnv);
            }

            // Production and Staging require explicit inclusion
            if (currentEnv == BoltEnvironment.Production)
            {
                return runIn?.IncludeProduction == true;
            }

            if (currentEnv == BoltEnvironment.Staging)
            {
                return runIn?.IncludeStaging == true;
            }

            // Default: Allow QA, Dev, UAT
            return currentEnv == BoltEnvironment.Qa ||
                   currentEnv == BoltEnvironment.Dev ||
                   currentEnv == BoltEnvironment.Uat;
        }

        private static MethodInfo? GetTestMethod(Type testClass)
        {
            var methodName = TestContext.CurrentContext?.Test?.MethodName;
            if (methodName != null)
                return testClass.GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            return testClass.GetMethods()
                .FirstOrDefault(m => m.GetCustomAttribute<TestAttribute>() != null);
        }
    }
}