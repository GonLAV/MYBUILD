using LaunchDarkly.Sdk;

namespace Bolt.Automation.ExternalServices.LaunchDarkly
{
    public interface ILaunchDarklyUserContext
    {
        User GetUser();
    }
}
