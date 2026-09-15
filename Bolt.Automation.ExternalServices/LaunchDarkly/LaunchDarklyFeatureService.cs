using Bolt.Automation.Common.Logging.Core;
using LaunchDarkly.Sdk;
using LaunchDarkly.Sdk.Server;

namespace Bolt.Automation.ExternalServices.LaunchDarkly
{
    public class LaunchDarklyFeatureService(
        LdClient client,
        ILaunchDarklyUserContext userContext,
        IAutomationLogger logger)
        : IFeatureFlagService
    {
        public FeatureStatus GetFeatureStatus(string featureKey)
        {
            logger.Info($"Evaluating feature flag: {featureKey}");

            var user = userContext.GetUser();
            var context = Context.FromUser(user);

            var allFlags = client.AllFlagsState(context);
            if (!allFlags.Valid)
                throw new InvalidOperationException("LaunchDarkly flag state is not valid.");

            if (!allFlags.ToValuesJsonMap().ContainsKey(featureKey))
                throw new KeyNotFoundException($"Feature flag '{featureKey}' not found.");

            var result = client.BoolVariationDetail(featureKey, context, defaultValue: false);
            if (result.IsDefaultValue)
                throw new InvalidOperationException($"Failed to evaluate flag: {featureKey}");

            logger.Info($"Feature flag '{featureKey}' evaluated to '{result.Value}' for user: {user.Key}");
            return result.Value ? FeatureStatus.On : FeatureStatus.Off;
        }

        public string GetFeatureVariation(string featureKey, string defaultValue = "")
        {
            logger.Info($"Evaluating multivariate feature flag: {featureKey}");

            var user = userContext.GetUser();
            var context = Context.FromUser(user);

            var allFlags = client.AllFlagsState(context);
            if (!allFlags.Valid)
                throw new InvalidOperationException("LaunchDarkly flag state is not valid.");

            // A flag that shipped but was never configured for this environment is simply absent here, and
            // that is a legitimate state the owning service falls back on - report the default, don't throw.
            if (!allFlags.ToValuesJsonMap().ContainsKey(featureKey))
            {
                logger.Info($"Feature flag '{featureKey}' is not defined in this environment - using '{defaultValue}'");
                return defaultValue;
            }

            var result = client.StringVariationDetail(featureKey, context, defaultValue);
            logger.Info($"Feature flag '{featureKey}' evaluated to '{result.Value}' for user: {user.Key}");
            return result.Value;
        }
    }

}
