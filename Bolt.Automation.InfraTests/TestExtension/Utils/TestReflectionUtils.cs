using NUnit.Framework;

namespace Bolt.Automation.InfraTests.TestExtension.Utils
{
    public static class TestReflectionUtils
    {
        public static string? GetTestMethodName()
        {
            return TestContext.CurrentContext?.Test?.MethodName;
        }
    }
}

