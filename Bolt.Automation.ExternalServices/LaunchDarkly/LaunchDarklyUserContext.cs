using Bolt.Automation.Common.Context;
using Bolt.Automation.Common.Logging.Core;
using LaunchDarkly.Sdk;

namespace Bolt.Automation.ExternalServices.LaunchDarkly
{
    public class LaunchDarklyUserContext(IScopeContext scopeContext, IAutomationLogger logger) : ILaunchDarklyUserContext
    {
        private static readonly List<string> LaunchDarklyCustomAttributeKeys =
        [
            "SourceKeyword",
            "InterviewFlowType",
            "BusinessType",
            "UserRoles"
        ];

        public User GetUser()
        {
            var builder = User.Builder("QAAutomationUser");
            var tenant = scopeContext.Data.Tenant;
            builder.Custom("Tenant", tenant?.ToString());

            logger.Info($"Building LaunchDarkly user for tenant: {tenant}");

            var attributes = GetLaunchDarklyCustomAttributes();
            foreach (var (key, value) in attributes)
            {
                builder.Custom(key, value);
                logger.Info($"Added custom attribute to user: {key} = {value}");
            }

            return builder.Build();
        }

        private Dictionary<string, string> GetLaunchDarklyCustomAttributes()
        {
            var propertiesDict = new Dictionary<string, string>();

            foreach (var key in LaunchDarklyCustomAttributeKeys)
            {
                var value = scopeContext.GetTestValue(key);
                if (!string.IsNullOrEmpty(value))
                {
                    propertiesDict[key] = value;
                }
            }
            return propertiesDict;
        }
    }
}
