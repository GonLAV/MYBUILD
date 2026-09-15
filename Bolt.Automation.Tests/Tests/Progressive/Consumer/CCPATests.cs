using Bolt.Automation.Common;
using Bolt.Automation.Common.Enums;
using Bolt.Automation.Common.Logging.Extentions;
using Bolt.Automation.FrontEnds.PlaywrightBase.Helpers;
using Bolt.Automation.FrontEnds.Projects.HQXConsumer.Flows;
using Bolt.Automation.FrontEnds.Projects.HQXConsumer.FormData;
using Bolt.Automation.FrontEnds.Projects.HQXConsumer.FormData.FieldsRegistry;
using Bolt.Automation.FrontEnds.Projects.HQXConsumer.Pages;
using Bolt.Automation.TestDataProvider.Providers.PlatformAPIDataProvider;
using Bolt.Automation.TestDataProvider.TestData.PlatformAPITestData;
using Bolt.Automation.Tests.TestExtension.Attributes;
using Bolt.Automation.Tests.TestExtension.Base;
using NUnit.Framework;
using static Bolt.Automation.FrontEnds.FormData.Common.FieldNames;
using AuthorAttribute = Bolt.Automation.Tests.TestExtension.Attributes.AuthorAttribute;

namespace Bolt.Automation.Tests.Tests.Progressive
{
    [TestFixture]
    public class CCPATests : ProgressiveUITestBase
    {

        [Test]
        [RunIn(includeProduction: true)]
        [Tenant(Tenant.PROGRESSIVEPL)]
        [Author(Author.Helen)]
        [Category("Legal")]
        [Category("CCPA")]
        [Category("Regression")]
        [TestCaseId(206398)]
        [Description("Verifies CCPA cookie is saved correctly with opt-out preference when user clicks the Do Not Sell link")]
        public async Task PGR_HQX2_CCPA_Saving_Cookie_Value()
        {
            ScopeContext.Set(ctx => ctx.CurrentUser, TestContextAccessor.CurrentUserCollection.Consumer);
            var request = QuoteStartPrefillDataProvider.GetStandardPrefillData(Addresses.GetAddress(AddressKey.OH));
            var urlTestData = ScopeContext.Data.UrlDataCollection.CustomUrls;

            var (_, startUrl) = await _progressiveHelper.CallQuoteStartAsync(request, true);

            await _logger.ExecuteStepAsync("Navigate to Overview Page", async () =>
            {
                PageFactory.CreatePage<HQXConsumer_OverviewPage>();
            });

            await _logger.ExecuteStepAsync("Find and validate CCPA link", async () =>
            {
                var ccpaLinkLocator = await _pageHelper.WaitForElementAsync(_currentPage.Locator(GeneralConsumerFields.Fields[FieldsNameHQXConsumer.CCPALink].Locators[0]), 10000, true);
                Assert.That(ccpaLinkLocator, Is.Not.Null);

                var expectedHref = urlTestData.CCPAProgressive;
                var href = await ccpaLinkLocator.GetAttributeAsync("href");
                Assert.That(href, Is.EqualTo(expectedHref), $"CCPA link href: {href}, expected: {expectedHref}");
            });

            await _logger.ExecuteStepAsync("Open CCPA redirect in new tab and switch back", async () =>
            {
                await BrowserManager.OpenNewTabAsync(urlTestData.CCPARedirect);
                await BrowserManager.SwitchToFirstTabAsync();
            });

            await _logger.ExecuteStepAsync("Validate CCPA cookie value is C0004%3A0", async () =>
            {
                var context = await BrowserManager.GetContextAsync();
                var baseUrl = GetCurrentBaseUrl();
                var cookieValues = await CookieHelper.WaitForCookieValuesAsync(context, baseUrl, "PgrOptOutPref", expectedValue: "C0004%3A0", logger: _logger);
                Assert.That(cookieValues, Does.Contain("C0004%3A0"), $"Expected cookie 'C0004%3A0' not found. Actual cookies: {string.Join(",", cookieValues)}");
            });
        }

        [Test]
        [RunIn(includeProduction: true)]
        [Tenant(Tenant.PROGRESSIVEPL)]
        [Author(Author.Helen)]
        [TestCaseId(205654)]
        [Category("Legal")]
        [Category("CCPA")]
        [Category("Regression")]
        [Description("Verifies CCPA link is present on the kick-out Do Not Quote (DNQ) page in HQX 2.0")]
        public async Task PGR_HQX2_CCPA_Presence_In_KoDNQ()
        {
            ScopeContext.Set(ctx => ctx.CurrentUser, TestContextAccessor.CurrentUserCollection.Consumer);
            var request = QuoteStartPrefillDataProvider.GetStandardPrefillData(Addresses.GetAddress(AddressKey.FL));

            var (_, startUrl) = await _progressiveHelper.CallQuoteStartAsync(request, true);
            PageFactory.CreatePage<HQXConsumer_OverviewPage>();

            await _logger.ExecuteStepAsync("Click Property Section Edit/View All", async () =>
            {
                await _pageHelper.InteractWithField(FieldsNameHQXConsumer.PropertySectionEditViewAll);
            });

            await _logger.ExecuteStepAsync("Select MFH as Type of Dwelling to trigger DNQ Kickout", async () =>
            {
                await _pageHelper.InteractWithField(PLTypeOfDwelling, "Manufactured/Mobile Home");
                await _pageHelper.InteractWithField(MHArchitectureStyle, "Double Wide");
                await _pageHelper.InteractWithField(HomeLength, "20");
                await _pageHelper.InteractWithField(HomeWidth, "20");
                await _pageHelper.InteractWithField(NextButton);
                PageFactory.CreatePage<HQXConsumer_KoDNQPage>();
            });

            await _logger.ExecuteStepAsync("Find and validate CCPA link", async () =>
            {
                var ccpaLinkLocator = await _pageHelper.WaitForElementAsync(_currentPage.Locator(GeneralConsumerFields.Fields[FieldsNameHQXConsumer.CCPALink].Locators[0]), 10000, true);
                Assert.That(ccpaLinkLocator, Is.Not.Null);
            });
        }

        [Test]
        [RunIn(includeProduction: true)]
        [Tenant(Tenant.PROGRESSIVEPL)]
        [Author(Author.Helen)]
        [Category("Legal")]
        [Category("CCPA")]
        [Category("Regression")]
        [TestCaseId(205653)]
        [Description("Verifies CCPA Do Not Sell link is displayed on all pages and navigates to the correct URL")]
        public async Task PGR_HQX2_CCPA_DoNotSell_Link_Display_and_Navigation()
        {
            ScopeContext.Set(ctx => ctx.CurrentUser, TestContextAccessor.CurrentUserCollection.Consumer);
            var request = QuoteStartPrefillDataProvider.GetStandardPrefillData(Addresses.GetAddress(AddressKey.OR));
            var formData = new Dictionary<string, string>();
            formData[YearsAtAddress] = "3";

            await _progressiveHelper.CallQuoteStartAsync(request, true);

            await _logger.ExecuteStepAsync("Execute flow to Rates Page and assert DoNotSell link", async () =>
            {
                _logger.Info($"Executing flow to Rates Page and asserting Do not sell link on each page.");
                await Executor.ExecuteToPage<HQXConsumer_RatesPage>(
                    FlowType.HQXShortFlow,
                    PageFactory.CreatePage<HQXConsumer_OverviewPage>(),
                    formData: formData,
                    fillForms: true,
                    pagesToSkip: null,
                    pageCallbacks: null,
                    perPageAction: async page => await AssertDoNotSellLinkAsync());

                await AssertDoNotSellLinkAsync();
            });
        }

        [Test]
        [RunIn(includeProduction: true)]
        [Tenant(Tenant.PROGRESSIVEPL)]
        [Author(Author.Helen)]
        [Category("Legal")]
        [Category("CCPA")]
        [Category("Regression")]
        [TestCaseId(221305)]
        [Description("Verifies California Notice link is displayed on all pages and navigates to the correct URL")]
        public async Task PGR_HQX2_California_Notice_Link_Display_and_Navigation()
        {
            ScopeContext.Set(ctx => ctx.CurrentUser, TestContextAccessor.CurrentUserCollection.Consumer);
            var request = QuoteStartPrefillDataProvider.GetStandardPrefillData(Addresses.GetAddress(AddressKey.OR));

            var formData = new Dictionary<string, string>
            {
                [YearsAtAddress] = "3"
            };

            await _progressiveHelper.CallQuoteStartAsync(request, navigate: true);

            await _logger.ExecuteStepAsync("Execute flow to Rates Page and assert CA Notice link", async () =>
            {
                _logger.Info($"Executing flow to Rates Page and asserting CA Notice link on each page.");
                await Executor.ExecuteToPage<HQXConsumer_RatesPage>(
                    FlowType.HQXShortFlow,
                    PageFactory.CreatePage<HQXConsumer_OverviewPage>(),
                    formData: formData,
                    fillForms: true,
                    pagesToSkip: null,
                    pageCallbacks: null,
                    perPageAction: async page => await AssertCANoticeLinkAsync());
                await AssertCANoticeLinkAsync();
            });
        }

        [Test]
        [RunIn(includeProduction: true)]
        [Tenant(Tenant.PROGRESSIVEPL)]
        [Author(Author.Helen)]
        [Category("Legal")]
        [Category("CCPA")]
        [Category("Regression")]
        [TestCaseId(223714)]
        [Description("Verify that the PgrOptOutPref cookie is set and appended to the redirect URL during the MPQ3 Renters flow")]
        public async Task PGR_HQX2_CCPA_MPQ3_Renters_Cookie_URL_Params()
        {
            ScopeContext.Set(ctx => ctx.CurrentUser, TestContextAccessor.CurrentUserCollection.Consumer);
            var request = QuoteStartPrefillDataProvider.GetStandardPrefillData(Addresses.GetAddress(AddressKey.OH));
            request.SourceName = "MPQ3_Renters";
            request.LOBCd = "Renters";

            await _progressiveHelper.CallQuoteStartAsync(request, navigate: true);

            var editAddressPage = await _logger.ExecuteStepAsync("Navigate to Edit Address Page", async () =>
            {
                return PageFactory.CreatePage<HQXConsumer_EditAddressPage>();
            });

            await _logger.ExecuteStepAsync("Validate PgrOptOutPref cookie is set to C0004%3A1", async () =>
            {
                var context = await BrowserManager.GetContextAsync();
                var baseUrl = GetCurrentBaseUrl();
                var cookieValues = await CookieHelper.WaitForCookieValuesAsync(context, baseUrl, "PgrOptOutPref", expectedValue: "C0004%3A1", timeout: 15000, logger: _logger);
                Assert.That(cookieValues, Does.Contain("C0004%3A1"), $"Expected cookie 'C0004%3A1' not found. Actual cookies: {string.Join(",", cookieValues)}");
            });

            await _logger.ExecuteStepAsync("Click Continue button and wait for navigation", async () =>
            {
                await editAddressPage.ClickContinue();
                await _pageHelper.WaitForNavigationOrUrlContainsAsync("quoteType", timeout: 10000);
            });

            await _logger.ExecuteStepAsync("Validate submission and URL parameters", async () =>
            {
                var currentUrl = BrowserManager.GetCurrentTab()?.Url;
                _logger.Info($"Current URL after submission: {currentUrl}");
                Assert.That(currentUrl, Does.Contain("PgrOptOutPref="), $"Expected URL to contain 'PgrOptOutPref=' parameter. Actual URL: {currentUrl}");
                Assert.That(currentUrl, Does.Contain("quoteType=mpq_dr"), $"Expected URL to contain 'quoteType=mpq_dr'. Actual URL: {currentUrl}");
            });
        }

        private async Task AssertCANoticeLinkAsync()
        {
            await AssertLinkNavigationAsync(
                FieldsNameHQXConsumer.CANoticeLink,
                TestContextAccessor.CurrentUrlCollection.CustomUrls.CANotice
            );
        }

        private async Task AssertDoNotSellLinkAsync()
        {
            await AssertLinkNavigationAsync(
                FieldsNameHQXConsumer.CCPALink,
                TestContextAccessor.CurrentUrlCollection.CustomUrls.CCPAProgressive
            );
        }

        private async Task AssertLinkNavigationAsync(string fieldName, string expectedUrl)
        {
            _logger.Info($"Validating link field '{fieldName}' href navigates to '{expectedUrl}'.");
            var href = await _pageHelper!.GetFieldAttribute(fieldName, "href");
            _logger.Info($"Link href: {href}");
            Assert.That(href, Is.EqualTo(expectedUrl), $"Expected link to navigate to '{expectedUrl}' but href was '{href}'.");
        }
    }
}
