using Bolt.Automation.ApiClients.Infrastructure;
using Bolt.Automation.ApiClients.PartnerPortalApi.Interfaces;
using Bolt.Automation.InfraTests.TestExtension.Attributes;
using Bolt.Automation.InfraTests.TestExtension.Base;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using static Bolt.Automation.Common.Tenant;

namespace Bolt.Automation.InfraTests.InfraTests
{
    public class PartnerPortalApiTests : TestBase
    {
        private readonly RefitApiServiceLocator _refitApiLocator;

        public PartnerPortalApiTests() : base()
        {
            _refitApiLocator = _testScope.ServiceProvider.GetRequiredService<RefitApiServiceLocator>();
        }

        [Test]
        [Tenant(BOLTAG)]
        [Property("TestCaseId", "20")]
        [Property("Category", "API")]
        public async Task PartnerPortalApiTest()
        {
            var user = TestContextAccessor.CurrentUserCollection.PartnerPortalAgent;
            ScopeContext.Set(ctx => ctx.CurrentUser, user);
            var partnerPortalApi = _refitApiLocator.GetService<IPartnerPortalApi>();
            var resp = await partnerPortalApi.GetInvitationDetailsAsync()
                .EnsureSuccessContentAsync("Failed to get invitation details");
            Assert.That(resp, Is.Not.Null);
            _logger.Info("Successfully retrieved invitation details");
        }
    }
}

