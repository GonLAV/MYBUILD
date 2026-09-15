using Bolt.Automation.ExternalServices.LaunchDarkly;
using Bolt.Automation.ExternalServices.Outlook;
using Bolt.Automation.InfraTests.TestExtension.Attributes;
using Bolt.Automation.InfraTests.TestExtension.Base;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using static Bolt.Automation.Common.Tenant;

namespace Bolt.Automation.InfraTests.InfraTests
{
    public class OutlookAndFeatureFlagTests : TestBase
    {
        public OutlookAndFeatureFlagTests() : base() { }

        [Test]
        [Tenant(UNIFY)]
        public async Task OutlookClientAndLDTest()
        {
            var outlookClient = _testScope.ServiceProvider.GetRequiredService<IOutlookClient>();
            var resp = await outlookClient.GetMessages();
            Assert.That(resp, Is.Not.Null);

            var featureService = _testScope.ServiceProvider.GetRequiredService<IFeatureFlagService>();
            var status = featureService.GetFeatureStatus("ba_BlockBind");
        }

        [Test]
        [Tenant(UNIFY)]
        public void MultivariateFlagReadTest()
        {
            var featureService = _testScope.ServiceProvider.GetRequiredService<IFeatureFlagService>();

            // An undefined key must fall back rather than throw, so a caller can treat "not configured
            // in this environment" as a value instead of an error.
            var missing = featureService.GetFeatureVariation("nexus-flag-that-does-not-exist", "fallback");
            Assert.That(missing, Is.EqualTo("fallback"));

            // A real multivariate flag returns its string variation, where GetFeatureStatus would throw.
            var mode = featureService.GetFeatureVariation("ranking-mock-mode", "(not defined)");
            Assert.That(mode, Is.Not.Empty);
        }
    }
}

