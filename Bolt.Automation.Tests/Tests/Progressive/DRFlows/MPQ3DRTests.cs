using Bolt.Automation.ApiClients.Infrastructure;
using Bolt.Automation.ApiClients.PlatformApi.Entities.Common;
using Bolt.Automation.Common;
using Bolt.Automation.Common.Enums;
using Bolt.Automation.Common.Logging.Extentions;
using Bolt.Automation.FrontEnds.Projects.HQXConsumer.Pages;
using Bolt.Automation.TestDataProvider.Providers.PlatformAPIDataProvider;
using Bolt.Automation.TestDataProvider.TestData.PlatformAPITestData;
using Bolt.Automation.Tests.TestExtension.Attributes;
using Bolt.Automation.Tests.TestExtension.Base;
using NUnit.Framework;
using AuthorAttribute = Bolt.Automation.Tests.TestExtension.Attributes.AuthorAttribute;

namespace Bolt.Automation.Tests.Tests.Progressive.DRFlows
{
    [TestFixture]
    public class MPQ3DRTests : ProgressiveUITestBase
    {
        [Test]
        [RunIn(includeProduction: true)]
        [Tenant(Tenant.PROGRESSIVEPL)]
        [TestCaseId(89383)]
        [Category("MPQ3DR")]
        [Category("Sanity")]
        [Description("Validates that an MPQ3 Renters quote reaches Complete in Platform API and that Deeplink navigation returns to Edit Address page.")]
        [Author(Author.Helen)]
        public async Task PGR_MPQ3DR_Renters_Complete_quote_and_validate_DeepLink_returns_to_Edit_Address_page()
        {
            ScopeContext.Set(ctx => ctx.CurrentUser, TestContextAccessor.CurrentUserCollection.Consumer);
            var request = QuoteStartPrefillDataProvider.GetStandardPrefillData(Addresses.GetAddress(AddressKey.OH));
            request.SourceName = "MPQ3_Renters";
            request.LOBCd = "Renters";

            var (externalId, _) = await _progressiveHelper.CallQuoteStartAsync(request, navigate: true);

            var editAddressPage = await _logger.ExecuteStepAsync("Navigate to Edit Address Page", async () =>
            {
                return PageFactory.CreatePage<HQXConsumer_EditAddressPage>();
            });

            await _logger.ExecuteStepAsync("Click Continue on Edit Address Page and wait for navigation to Progressive domain", async () =>
            {
                await editAddressPage.ClickContinue();
                await _pageHelper.WaitForNavigationOrUrlContainsAsync("quoteType", timeout: 10000);
            });

            var platformApi = await _platformApiFactory.CreateApiClientAsync();

            var quoteStatus = await _logger.ExecuteStepAsync("Poll QuoteStatus from Platform API until Complete", async () =>
            {
                var quoteStatusRequest = QuoteStatusDataProvider.CreateQuoteStatusData(externalId!, "MPQ3_Renters");
                _logger.Info($"Posting QuoteStatus for externalId: {externalId}");
                var response = (await platformApi.QuoteStatusWithPollingAsync(quoteStatusRequest)).Content!;
                _logger.Info($"QuoteStatus: {response.QuoteStatus}, SecondaryStatus: {response.SecondaryStatus}, MsgStatusCd: {response.MsgStatusCd}");
                return response;
            });

            Assert.That(quoteStatus.QuoteStatus, Is.EqualTo(nameof(QuoteStatus.Complete)),
                $"QuoteStatus was not Complete. Actual: {quoteStatus.QuoteStatus}");
            Assert.That(quoteStatus.SecondaryStatus, Is.EqualTo(nameof(QuoteSecondaryStatus.None)),
                $"SecondaryStatus was not None. Actual: {quoteStatus.SecondaryStatus}");

            await _logger.ExecuteStepAsync("Post DeepLink to Platform API, navigate to returned URL, and verify Edit Address page", async () =>
            {
                var deepLinkRequest = QuoteDeepLinkDataProvider.CreateDeepLinkData(externalId!, "MPQ3_Renters");
                var deepLinkResponse = await platformApi.DeeplinkAsync(deepLinkRequest).EnsureSuccessContentAsync();

                await BrowserManager.NavigateAsync(deepLinkResponse.WebsiteURL);
                PageFactory.CreatePage<HQXConsumer_EditAddressPage>();
            });
        }
    }
}
