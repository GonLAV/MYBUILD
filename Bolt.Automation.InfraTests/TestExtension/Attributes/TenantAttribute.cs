using Bolt.Automation.Common;
using NUnit.Framework;

namespace Bolt.Automation.InfraTests.TestExtension.Attributes
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
    public class TenantAttribute(Tenant tenant) : PropertyAttribute("Tenant", tenant.ToString())
    {
        public Tenant TenantValue { get; } = tenant;
    }
}