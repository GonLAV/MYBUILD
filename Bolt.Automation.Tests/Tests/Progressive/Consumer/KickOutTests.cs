using Bolt.Automation.ApiClients.GetQuoteApi.Interfaces;
using Bolt.Automation.ApiClients.Infrastructure;
using Bolt.Automation.ApiClients.SSO;
using Bolt.Automation.Common;
using Bolt.Automation.Common.Enums;
using Bolt.Automation.Common.Logging.Extentions;
using Bolt.Automation.FrontEnds.Projects.HQXAgent.Pages;
using Bolt.Automation.FrontEnds.Projects.HQXConsumer.Pages;
using Bolt.Automation.TestDataProvider.Context;
using Bolt.Automation.TestDataProvider.Providers.PlatformAPIDataProvider;
using Bolt.Automation.TestDataProvider.TestData.PlatformAPITestData;
using Bolt.Automation.Tests.TestExtension.Attributes;
using Bolt.Automation.Tests.TestExtension.Base;
using Bolt.Automation.Tests.TestHelpers.Progressive;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using AuthorAttribute = Bolt.Automation.Tests.TestExtension.Attributes.AuthorAttribute;

namespace Bolt.Automation.Tests.Tests.Progressive
{
    public class KickOutTests : ProgressiveUITestBase
    {
        private IGetQuoteApi _getQuoteApi = null!;
        private ISsoApiFactory _ssoApiFactory = null!;

        private PaaTestHelper _paaHelper = null!;

        protected override void ResolveServices()
        {
            base.ResolveServices();
            var refitApiLocator = _uiTestScope.ServiceProvider.GetRequiredService<RefitApiServiceLocator>();
            _getQuoteApi = refitApiLocator.GetRequiredService<IGetQuoteApi>();
            _ssoApiFactory = _testScope.ServiceProvider.GetRequiredService<ISsoApiFactory>();
        }

        protected override void InitializeComponents()
        {
            base.InitializeComponents();
            _paaHelper = new PaaTestHelper(
                _getQuoteApi, _ssoApiFactory, Executor, PageFactory, BrowserManager, ScopeContext, TestContextAccessor);
        }

        [Test]
        [RunIn(includeProduction: true)]
        [Tenant(Tenant.PROGRESSIVEPL)]
        [Author(Author.Helen)]
        [Category("KickOut")]
        [Category("Regression")]
        [TestCaseId(126879)]
        [Description("Consumer Quote locked by agent kickout.")]
        public async Task PGR_HQX2_Locked_By_Agent_KickOut()
        {
            ScopeContext.Set(ctx => ctx.FrontEnd, FrontEndType.HQXConsumer);
            ScopeContext.Set(ctx => ctx.CurrentUser, TestContextAccessor.CurrentUserCollection.Consumer);

            var request = QuoteStartPrefillDataProvider.GetStandardPrefillData(Addresses.GetAddress(AddressKey.OR));
            string? externalId = null;
            string? friendlyId = null;
            string? lastName = null;
            string? zipCode = null;


            var (ExternalId, _) = await _progressiveHelper.CallQuoteStartAsync(request, true);
            externalId = ExternalId;

            var overviewConsumerPage = PageFactory.CreatePage<HQXConsumer_OverviewPage>();
            friendlyId = await overviewConsumerPage.GetFriendlyId();
            lastName = request.ApplicantDetails?.Surname;
            zipCode = request.PropertyAddr?.PostalCode;


            var overviewAgentPage = await _logger.ExecuteStepAsync("Open HQX Agent and lock quote", async () =>
             {
                 _paaHelper.SetAdminUserContext();
                 _paaHelper.SetRelayState(externalId);
                 ScopeContext.Set(ctx => ctx.FrontEnd, FrontEndType.HQXAgent);
                 await _paaHelper.NavigateThroughSsoToOverviewAsync(true);

                 var overviewAgentPage = PageFactory.CreatePage<HQXAgent_OverviewPage>();
                 await overviewAgentPage.ClickEditQuote();
                 var customerIntroPage = PageFactory.CreatePage<HQXAgent_CustomerIntroPage>();
                 await customerIntroPage.ClickContinue();
                 overviewAgentPage = PageFactory.CreatePage<HQXAgent_OverviewPage>();
                 return overviewAgentPage;
             });

            await _logger.ExecuteStepAsync("Verify consumer kickout screen", async () =>
            {
                await BrowserManager.SwitchToFirstTabAsync();
                ScopeContext.Set(ctx => ctx.FrontEnd, FrontEndType.HQXConsumer);

                var overviewConsumerPage = PageFactory.CreatePage<HQXConsumer_OverviewPage>();
                await overviewConsumerPage.ClickContinue();

                var kickoutPage = PageFactory.CreatePage<HQXConsumer_KoLockedPage>();
                var title = await kickoutPage.GetTitleTextAsync();
                var message = await kickoutPage.GetMessageTextAsync();

                Assert.That(title, Is.EqualTo("Thanks for coming back!"));
                Assert.That(message, Does.Contain("Please call one of our friendly licensed home insurance experts at 844-416-5371 to continue."));

                var hoursText = await kickoutPage.GetWorkHoursTextAsync();
                Assert.That(hoursText, Is.EqualTo("We are open:Mon-Sun 8 AM-11 PM ET"));

                var linkText = await kickoutPage.GetProgressiveLinkAsync();
                Assert.That(linkText, Is.EqualTo("Go to Progressive.com"));

                await BrowserManager.CloseCurrentTabAsync();
            });

            await _logger.ExecuteStepAsync("Agent stops edit", async () =>
            {
                await overviewAgentPage.ClickStopEdit();
            });


            ScopeContext.Set(ctx => ctx.CurrentUser, TestContextAccessor.CurrentUserCollection.Consumer);

            var (_, url) = await _progressiveHelper.CallQuoteRetrievalAsync(friendlyId, lastName, zipCode);

            await BrowserManager.OpenNewTabAsync(url);
            _ = PageFactory.CreatePage<HQXConsumer_KoLockedPage>();

        }
    }
}
