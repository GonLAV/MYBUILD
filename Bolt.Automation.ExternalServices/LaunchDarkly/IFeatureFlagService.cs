namespace Bolt.Automation.ExternalServices.LaunchDarkly
{
    public interface IFeatureFlagService
    {
        FeatureStatus GetFeatureStatus(string featureKey);

        /// <summary>
        /// Reads a multivariate (string) flag, which <see cref="GetFeatureStatus"/> cannot - it evaluates
        /// booleans and throws on anything else. Returns <paramref name="defaultValue"/> when the flag is
        /// not defined in this environment, which is how a shipped-but-unconfigured flag reads.
        /// </summary>
        string GetFeatureVariation(string featureKey, string defaultValue = "");
    }
}
