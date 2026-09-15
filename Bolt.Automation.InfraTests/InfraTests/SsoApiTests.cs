using System.Text.RegularExpressions;
using Bolt.Automation.ApiClients.SSO;
using Bolt.Automation.Common.Enums;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using static Bolt.Automation.Common.Tenant;
using Bolt.Automation.InfraTests.TestExtension.Base;
using Bolt.Automation.InfraTests.TestExtension.Attributes;
using Bolt.Automation.Tests.TestHelpers;

namespace Bolt.Automation.InfraTests.InfraTests
{
    public class SsoApiTests : TestBase
    {
        private readonly ISsoApiFactory _ssoApiFactory;
        private readonly SsoHelper _ssoHelper;

        public SsoApiTests() : base()
        {
            _ssoApiFactory = _testScope.ServiceProvider.GetRequiredService<ISsoApiFactory>();
            _ssoHelper = new SsoHelper(_logger, ScopeContext, _ssoApiFactory);
        }

        [Test]
        [Tenant(COMPARION)]
        [Property("TestCaseId", "10")]
        [Property("Category", "API")]
        public async Task ComparionSsoTest()
        {
            var quoteId = "OVVF-NLUX-F20N";//need to pass to testspecificdata
            var tenant = ScopeContext.Data.Tenant.ToString();

            var user = TestContextAccessor.CurrentUserCollection.Agent;
            ScopeContext.Set(ctx => ctx.CurrentUser, user);
            ScopeContext.Set(ctx => ctx.SamlTemplate, SamlTemplateType.Saml2ResponseTemplateWithGroupExternalId);

            var relayStateTestData = _ssoHelper.CreateQuoteRetrieveRelayState(
                quoteId, tenant, TestContextAccessor.CurrentRelayStateCollection);
            ScopeContext.Set(ctx => ctx.CurrentRelayStateType, relayStateTestData);

            var comparionSsoClient = _ssoApiFactory.CreateClient();
            var response = await comparionSsoClient.GetSsoResponse();
            var redirectUrl = response.Content?.RedirectUrl;
            Assert.That(redirectUrl, Is.Not.Null);
        }


        //[Test]
        //[Tenant(PROGRESSIVEPL)]
        //[Property("TestCaseId", "10")]
        //[Property("Category", "API")]
        ////public async Task PGRSsoTest()
        //{
        //    var requestData = new ApplicationRequestModel<PersonalHomeData>
        //    {
        //        Products = ApplicationTestData.Products.HomeownersDF,
        //        Data = PersonalHomeDataProvider
        //            .GetPersonalHomeData
        //            (ApplicationTestData.PropertyAddresses.GAThomaston,
        //                ApplicationTestData.PersonalInfo.GetRandomPersonalInfo(),
        //                ApplicationTestData.HomeDetailsTestData.PGRHomeDetails,
        //                ApplicationTestData.HomeFeaturesTestData.PGRHomeFeatureDetails,
        //                ApplicationTestData.PolicyTestData.PGRPolicyDetails,
        //                ApplicationTestData.CustomFields.PGRCustomFields)
        //    };

        //var user = CurrentUserCollection.Admin;
        //    ScopeContext.Set(ctx => ctx.CurrentUser, user);
        //    ScopeContext.Set(ctx => ctx.SamlTemplate, SamlTemplateType.Saml2ResponseTemplate);
        //    var baseRelayState = CurrentRelayStateCollection.RetrieveQuoteRelayState.Value;
        //    var relayStateTestData = new RelayStateTestData { Value = relayStateValue };
        //    ScopeContext.Set(ctx => ctx.CurrentRelayStateType, relayStateTestData);

        //    var progressiveSsoClient = _ssoApiFactory.CreateClient();
        //    var ssoResponse = await progressiveSsoClient.GetSsoResponse();
        //    var redirectUrl = ssoResponse.Content?.RedirectUrl;

            
        //    Assert.That(redirectUrl, Is.Not.Null);
        //}

        [Test]
        [Tenant(COMPARION)]
        [Property("TestCaseId", "11")]
        [Property("Category", "API")]
        public async Task ComparionSsoTestError301()
        {
            var user = TestContextAccessor.CurrentUserCollection.Agent;
            ScopeContext.Set(ctx => ctx.CurrentUser, user);

            var usersRelayState = TestContextAccessor.CurrentRelayStateCollection.UsersRelayState;
            ScopeContext.Set(ctx => ctx.CurrentRelayStateType, usersRelayState);

            ScopeContext.Set(ctx => ctx.SamlTemplate, SamlTemplateType.Saml2ResponseTemplateWithGroupExternalId);

            var comparionSsoClient = _ssoApiFactory.CreateClient();
            var response = await comparionSsoClient.GetSsoResponse();
            var redirectUrl = response.Content?.RedirectUrl;
            Assert.That(redirectUrl, Is.Not.Null);
        }

        [Test]
        [Tenant(USAA)]
        [Property("TestCaseId", "12")]
        [Property("Category", "API")]
        public async Task UsaaSsoTest()
        {
            var user = TestContextAccessor.CurrentUserCollection.Agent;
            ScopeContext.Set(ctx => ctx.CurrentUser, user);

            var usaaSsoClient = _ssoApiFactory.CreateClient();
            var response = await usaaSsoClient.GetSsoResponse();
            var redirectUrl = response.Content?.RedirectUrl;
            Assert.That(redirectUrl, Is.Not.Null);
        }

        [Test]
        [Tenant(USAA)]
        [Property("TestCaseId", "13")]
        [Property("Category", "API")]
        public async Task UsaaSsoTestError110()
        {
            var user = TestContextAccessor.CurrentUserCollection.Agent;
            ScopeContext.Set(ctx => ctx.CurrentUser, user);

            var dummyD2C = TestContextAccessor.CurrentRelayStateCollection.DummyD2CRelayState;
            ScopeContext.Set(ctx => ctx.CurrentRelayStateType, dummyD2C);

            var usaaSsoClient = _ssoApiFactory.CreateClient();
            var response = await usaaSsoClient.GetSsoResponse();
            var redirectUrl = response.Content?.RedirectUrl;
            Assert.That(redirectUrl, Is.Not.Null);
        }

        [Test]
        [Tenant(USAA)]
        [Property("TestCaseId", "14")]
        [Property("Category", "API")]
        public async Task UsaaSsoTestError202()
        {
            string regexPattern = @"Error: We're sorry, we were unable to log you in due to a permission error. Please call our support \(Error Code: 202 Ticket Number: ([a-f0-9\-]+)\)";

            var user = TestContextAccessor.CurrentUserCollection.D2CAgent2;
            ScopeContext.Set(ctx => ctx.CurrentUser, user);

            var D2C = TestContextAccessor.CurrentRelayStateCollection.D2CRelayState;
            ScopeContext.Set(ctx => ctx.CurrentRelayStateType, D2C);

            var usaaSsoClient = _ssoApiFactory.CreateClient();
            var isExceptionThrown = false;
            string errorMessage = string.Empty;
            try
            {
                var response = await usaaSsoClient.GetSsoResponse();
            }
            catch (Exception e)
            {
                errorMessage = e.Message;
                isExceptionThrown = true;
            }
            var isMatch = Regex.IsMatch(errorMessage, regexPattern);
            Assert.Multiple(() =>
            {
                Assert.That(isExceptionThrown, Is.True, "Failed. due to implementation Exception Unable to Login should be thrown");
                Assert.That(isMatch, Is.True, $"Failed. Error message doesn't match pattern {regexPattern}. Actual message: {errorMessage}");
            });
        }

        [Test]
        [Tenant(USAA)]
        [Property("TestCaseId", "15")]
        [Property("Category", "API")]
        public async Task UsaaSsoD2CTest()
        {
            var user = TestContextAccessor.CurrentUserCollection.OnlineQuote;
            ScopeContext.Set(ctx => ctx.CurrentUser, user);

            var D2C = TestContextAccessor.CurrentRelayStateCollection.D2CRelayState;
            ScopeContext.Set(ctx => ctx.CurrentRelayStateType, D2C);

            var usaaSsoClient = _ssoApiFactory.CreateClient();
            var response = await usaaSsoClient.GetSsoResponse();
            var redirectUrl = response.Content?.RedirectUrl;
            Assert.That(redirectUrl, Is.Not.Null);
        }

        [Test]
        [Tenant(LIBERTYX)]
        [Property("Category", "API")]
        [Property("TestCaseId", "16")]
        public async Task LibertyXSsoMccTest()
        {
            var user = TestContextAccessor.CurrentUserCollection.Agent;
            ScopeContext.Set(ctx => ctx.CurrentUser, user);

            var mccRelay = TestContextAccessor.CurrentRelayStateCollection.AgentMccRelayState;
            ScopeContext.Set(ctx => ctx.CurrentRelayStateType, mccRelay);

            var libertyXSsoClient = _ssoApiFactory.CreateClient();
            var response = await libertyXSsoClient.GetSsoResponse();

            var redirectUrl = response.Content?.RedirectUrl;
            Assert.That(redirectUrl, Is.Not.Null);
        }

        [Test]
        [Tenant(BOLTAG)]
        [Property("TestCaseId", "17")]
        [Property("Category", "API")]
        public async Task PartnerPortalSsoTest()
        {   
            var user = TestContextAccessor.CurrentUserCollection.WFGAgent;
            ScopeContext.Set(ctx => ctx.CurrentUser, user);

            ScopeContext.Set(ctx => ctx.SamlTemplate, SamlTemplateType.SamlPartnerPortal);

            var boltagSsoClient = _ssoApiFactory.CreateClient();

            var response = await boltagSsoClient.GetSsoResponse();

            var redirectUrl = response.Content?.RedirectUrl;
            Assert.That(redirectUrl, Is.Not.Null);
        }
    }
}

