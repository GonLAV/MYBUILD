using NUnit.Framework;

namespace Bolt.Automation.Tests.TestExtension.Attributes
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
    public class TestCaseIdAttribute(int testCaseId) : PropertyAttribute("TestCaseId", testCaseId.ToString())
    {
        public int TestCaseIdValue { get; } = testCaseId;
    }
}