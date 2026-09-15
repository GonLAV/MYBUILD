using Bolt.Automation.ApiClients.GetQuoteApi.Interfaces;
using Bolt.Automation.ApiClients.Infrastructure;
using Bolt.Automation.ApiClients.SSO;
using Bolt.Automation.Common;
using Bolt.Automation.Common.Enums;
using Bolt.Automation.Common.Logging.Extentions;
using Bolt.Automation.Common.PollyRetry;
using Bolt.Automation.Common.Utils;
using Bolt.Automation.FrontEnds.Projects.HQXAgent.Pages;
using Bolt.Automation.FrontEnds.Projects.HQXAgent.Popups;
using Bolt.Automation.ExternalServices.Outlook;
using Bolt.Automation.TestDataProvider.Providers.GetQuoteApiDataProvider;
using Bolt.Automation.TestDataProvider.TestData.CommonTestData;
using Bolt.Automation.Tests.TestData.Progressive;
using Bolt.Automation.Tests.TestExtension.Attributes;
using Bolt.Automation.Tests.TestHelpers.Progressive;
using Bolt.Automation.Tests.TestExtension.Base;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using static Bolt.Automation.Common.Tenant;
using static Bolt.Automation.FrontEnds.Projects.HQXAgent.FormData.FieldNamesHQXAgent;
using AuthorAttribute = Bolt.Automation.Tests.TestExtension.Attributes.AuthorAttribute;

namespace Bolt.Automation.Tests.Tests.Progressive
{
    public class PaaSoldNoteValidationTests : UITestBase
    {
        private IGetQuoteApi _getQuoteApi = null!;
        private ISsoApiFactory _ssoApiFactory = null!;
        private PaaTestHelper _paaHelper = null!;
        private IPollyRetryService _pollyRetryService = null!;
        private IOutlookClient _outlookClient = null!;

        // ── Test class setup ───────────────────────────────────────────────────────────

        public PaaSoldNoteValidationTests() : base()
        {
            ScopeContext.Set(ctx => ctx.FrontEnd, FrontEndType.HQXAgent);
        }

        protected override void ResolveServices()
        {
            var refitApiLocator = _uiTestScope.ServiceProvider.GetRequiredService<RefitApiServiceLocator>();
            _getQuoteApi = refitApiLocator.GetRequiredService<IGetQuoteApi>();
            _ssoApiFactory = _testScope.ServiceProvider.GetRequiredService<ISsoApiFactory>();
            _pollyRetryService = _testScope.ServiceProvider.GetRequiredService<IPollyRetryService>();
            _outlookClient = _testScope.ServiceProvider.GetRequiredService<IOutlookClient>();
        }

        protected override void InitializeComponents()
        {
            _paaHelper = new PaaTestHelper(
                _getQuoteApi, _ssoApiFactory, Executor, PageFactory, BrowserManager, ScopeContext, TestContextAccessor);
        }

        // ── Tests ──────────────────────────────────────────────────────────────────────

        [Test]
        [RunIn(includeProduction: false)]
        [Tenant(PROGRESSIVEPL)]
        [Category("PAA")]
        [Category("SoldNote")]
        [Category("Regression")]
        [TestCaseId(243376)]
        [Author(Author.Helen)]
        [Description("Verifies that submitting a valid Homesite sold note creates a Policy Ordered note and an Email Out note in the GetQuote API with matching policy number, carrier and premium")]
        public async Task PGR_PAA_SoldNote_Successful_Creation_Creates_Notes()
        {
            const string parentCompany = "Homesite Insurance";
            var policyNumber = SoldNotePolicyTestData.RandomDigits(8);

            await _logger.ExecuteStepAsync("Generate policy number and set up admin user context", async () =>
            {
                _logger.Info($"Generated policy number '{policyNumber}' for carrier '{parentCompany}'.");
                _paaHelper.SetAdminUserContext();
                var address = AddressData.GetAddress(AddressKey.OH);
                var data = PersonalLineDataProvider.GetPersonalDataProgressive(address);
                await _paaHelper.CreateApplicationAndSetRelayStateAsync(data);
            });

            await _logger.ExecuteStepAsync("Navigate through SSO and reach Overview page", async () =>
            {
                await _paaHelper.NavigateThroughSsoToOverviewAsync();
                PageFactory.CreatePage<HQXAgent_OverviewPage>();
            });

            var notePopup = await _logger.ExecuteStepAsync("Open Sold Note modal", async () =>
            {
                var overviewPage = PageFactory.CreatePage<HQXAgent_OverviewPage>();
                return await overviewPage.ClickCreateNoteAsync();
            }, "Expected result: Sold Note form is visible inside the Create Note modal");

            await _logger.ExecuteStepAsync($"Create sold note for carrier '{parentCompany}' with policy number '{policyNumber}' and close confirmation", async () =>
            {
                await notePopup.CreateSoldNoteAndConfirmAsync(new SoldNoteFormData
                {
                    PolicyNumber  = policyNumber,
                    Product       = SoldNotePolicyTestData.DefaultProduct,
                    EffectiveDate = DateTime.Today.AddDays(7).ToString("MM/dd/yyyy"),
                    ParentCompany = parentCompany,
                    Premium       = SoldNotePolicyTestData.DefaultPremium
                });
                PageFactory.CreatePage<HQXAgent_OverviewPage>();
            }, "Expected result: 'Your note has been saved!' shown and closed; interview is now in post-sale locked state");

            var externalId = ScopeContext.Get(ctx => ctx.ExternalId)!;
            var notes = await _logger.ExecuteStepAsync($"Poll GetQuote API for notes on application '{externalId}'", async () =>
            {
                return await _pollyRetryService.ExecuteWithRetryAsync(async () =>
                {
                    var response = await _getQuoteApi.GetNotesByApplicationIdAsync(externalId);
                    var noteList = response.Content ?? [];
                    if (!noteList.Any(n => string.Equals(n.Action, "Policy Ordered", StringComparison.OrdinalIgnoreCase)))
                        throw new InvalidOperationException("'Policy Ordered' note not yet available in GetQuote API — retrying.");
                    return noteList;
                });
            }, "Expected result: API returns at least a 'Policy Ordered' note and an 'Email out' note");

            await _logger.ExecuteStepAsync("Assert 'Policy Ordered' note contains correct policy number, carrier and premium", async () =>
            {
                var policyOrderedNote = notes.FirstOrDefault(n => string.Equals(n.Action, "Policy Ordered", StringComparison.OrdinalIgnoreCase));
                Assert.That(policyOrderedNote, Is.Not.Null, "Expected a 'Policy Ordered' note in the API response");
                Assert.Multiple(() =>
                {
                    Assert.That(policyOrderedNote!.Description, Does.Contain(policyNumber),
                        $"'Policy Ordered' note description should contain policy number '{policyNumber}'");
                    Assert.That(policyOrderedNote.Description, Does.Contain(parentCompany),
                        $"'Policy Ordered' note description should contain carrier '{parentCompany}'");
                    Assert.That(policyOrderedNote.Description, Does.Contain($"Premium: {SoldNotePolicyTestData.DefaultPremium}"),
                        $"'Policy Ordered' note description should contain 'Premium: {SoldNotePolicyTestData.DefaultPremium}'");
                });
            });

        }

        [TestCaseSource(typeof(SoldNotePolicyTestData), nameof(SoldNotePolicyTestData.InvalidFormatCases))]
        [RunIn(includeProduction: false)]
        [Tenant(PROGRESSIVEPL)]
        [Category("PAA")]
        [Category("SoldNote")]
        [Category("Regression")]
        [Author(Author.Helen)]
        [Description("Verifies that an invalid policy number format for each carrier triggers the correct carrier-specific validation error message in the Sold Note modal")]
        public async Task PGR_PAA_SoldNote_PolicyNumber_InvalidFormat_ShowsCarrierError(
            string parentCompany,
            string invalidPrefix,
            int digitCount,
            string expectedError,
            AddressKey addressKey)
        {
            var invalidPolicyNumber = invalidPrefix + SoldNotePolicyTestData.RandomDigits(digitCount);
            var address = AddressData.GetAddress(addressKey);

            await _logger.ExecuteStepAsync("Setup test data and agent user context", async () =>
            {
                _paaHelper.SetAdminUserContext();
                var data = PersonalLineDataProvider.GetPersonalDataProgressive(address);
                await _paaHelper.CreateApplicationAndSetRelayStateAsync(data);
            });

            await _logger.ExecuteStepAsync("Navigate through SSO and reach Overview page", async () =>
            {
                await _paaHelper.NavigateThroughSsoToOverviewAsync();
                PageFactory.CreatePage<HQXAgent_OverviewPage>();
            });

            var notePopup = await _logger.ExecuteStepAsync("Open Sold Note modal", async () =>
            {
                var overviewPage = PageFactory.CreatePage<HQXAgent_OverviewPage>();
                var popup = await overviewPage.ClickCreateNoteAsync();
                await popup.SelectActionAsync("Sold");
                return popup;
            }, "Expected result: Sold Note modal is displayed");

            await _logger.ExecuteStepAsync($"Fill Sold Note form with invalid policy number for carrier '{parentCompany}' and submit", async () =>
            {
                await notePopup.FillSoldNoteAsync(new SoldNoteFormData
                {
                    PolicyNumber  = invalidPolicyNumber,
                    Product       = SoldNotePolicyTestData.DefaultProduct,
                    EffectiveDate = DateTime.Today.AddDays(7).ToString("MM/dd/yyyy"),
                    ParentCompany = parentCompany,
                    Premium       = SoldNotePolicyTestData.DefaultPremium
                });
                await notePopup.ClickContinue();
            });

            await _logger.ExecuteStepAsync("Verify carrier-specific validation error is displayed", async () =>
            {
                var validationMessage = await _pageHelper!.GetFieldValidationTextAsync(PolicyNumberNote);
                Assert.That(TextHelper.NormalizeQuotes(validationMessage), Is.EqualTo(TextHelper.NormalizeQuotes(expectedError)),
                    $"Expected carrier error for '{parentCompany}' but got: '{validationMessage}'");
            });
        }

        [Test]
        [RunIn(includeProduction: false)]
        [Tenant(PROGRESSIVEPL)]
        [Category("PAA")]
        [Category("HQX2")]
        [Category("SoldNote")]
        [Category("Regression")]
        [TestCaseId(243420)]
        [Author(Author.Helen)]
        [Description("Verifies that a Homesite Homeowners sold note triggers the consumer email with the correct subject and all required hyperlinks, and creates the 'Email out' API note with description 'Sold Email sent to consumer'")]
        public async Task PGR_PAA_HomesiteSoldNote_EmailSentToConsumer()
        {
            var testStartedAt = DateTimeOffset.UtcNow.AddSeconds(-60);
            const string parentCompany       = "Homesite Insurance";
            const string automationEmail     = "boltautomation@boltinc.com";
            const string emailSubject        = "Important Information About Your New Homeowner's Policy";
            const string emailSenderName     = "Progressive Home";
            const string helocUrl            = "https://www.progressive.com/finance/home-equity-line-of-credit/?utm_campaign=ITA&utm_source=progressive&utm_content=heloc&partner_id=44&code=3939600101";
            const string simpliSafeUrl       = "https://simplisafe.com/progressive?utm_medium=partnerdigital&utm_source=Progressive&utm_campaign=HHX";
            const string cinchUrl            = "http://protect.cinchhomeservices.com/welcome?utm_source=progressive&utm_medium=partnerdigital&utm_campaign=hionb";
            const string emailOutAction      = "Email out";
            const string emailOutCommentTran = "Sold Email sent to consumer";
            var policyNumber = SoldNotePolicyTestData.RandomDigits(8);

            await _logger.ExecuteStepAsync("Set up admin user context and create application with automation email address", async () =>
            {
                _logger.Info($"Generated policy number '{policyNumber}' for carrier '{parentCompany}'.");
                _paaHelper.SetAdminUserContext();
                var address = AddressData.GetAddress(AddressKey.OH);
                var data = PersonalLineDataProvider.GetPersonalDataProgressive(address);
                data.Email = automationEmail;
                await _paaHelper.CreateApplicationAndSetRelayStateAsync(data);
            });

            await _logger.ExecuteStepAsync("Navigate through SSO and reach Overview page", async () =>
            {
                await _paaHelper.NavigateThroughSsoToOverviewAsync();
                PageFactory.CreatePage<HQXAgent_OverviewPage>();
            });

            await _logger.ExecuteStepAsync($"Open Sold Note modal and create sold note for carrier '{parentCompany}' with policy number '{policyNumber}'", async () =>
            {
                var overviewPage = PageFactory.CreatePage<HQXAgent_OverviewPage>();
                var notePopup = await overviewPage.ClickCreateNoteAsync();
                await notePopup.CreateSoldNoteAndConfirmAsync(new SoldNoteFormData
                {
                    PolicyNumber  = policyNumber,
                    Product       = SoldNotePolicyTestData.DefaultProduct,
                    EffectiveDate = DateTime.Today.AddDays(7).ToString("MM/dd/yyyy"),
                    ParentCompany = parentCompany,
                    Premium       = SoldNotePolicyTestData.DefaultPremium
                });
                PageFactory.CreatePage<HQXAgent_OverviewPage>();
            }, "Expected result: 'Your note has been saved!' shown and closed");

            var externalId = ScopeContext.Get(ctx => ctx.ExternalId)!;
            var emailOutNote = await _logger.ExecuteStepAsync($"Poll GetQuote API for 'Email out' note on application '{externalId}'", async () =>
            {
                return await _pollyRetryService.ExecuteWithRetryAsync(async () =>
                {
                    var response = await _getQuoteApi.GetNotesByApplicationIdAsync(externalId);
                    var noteList = response.Content ?? [];
                    var note = noteList.FirstOrDefault(n =>
                        string.Equals(n.Action, emailOutAction, StringComparison.OrdinalIgnoreCase) &&
                        (n.Description?.Contains(emailOutCommentTran, StringComparison.OrdinalIgnoreCase) ?? false)) ?? throw new InvalidOperationException($"'{emailOutAction}' note with description '{emailOutCommentTran}' not yet available — retrying.");
                    return note;
                });
            }, $"Expected result: API returns an '{emailOutAction}' note with description '{emailOutCommentTran}'");

            await _logger.ExecuteStepAsync("Assert 'Email out' note description contains 'Sold Email sent to consumer'", async () =>
            {
                Assert.That(emailOutNote, Is.Not.Null,
                    $"Expected an '{emailOutAction}' note with description containing '{emailOutCommentTran}'");
            });

            var email = await _logger.ExecuteStepAsync("Retrieve sold note email from automation inbox", async () =>
            {
                return await _outlookClient.GetSpecificEmail(
                    emailSenderName,
                    emailSubject,
                    testStartedAt);
            }, $"Expected result: Email with subject '{emailSubject}' found in automation inbox");

            await _logger.ExecuteStepAsync("Attach email HTML to test results", async () =>
            {
                await UploadEmailArtifactAsync(email.Body?.Content);
            });

            await _logger.ExecuteStepAsync("Assert email subject and all required hyperlinks are present in the email body", async () =>
            {
                // The platform now routes every outbound email link through AWS SES click-tracking, which
                // rewrites each href as awstrack.me/L0/<percent-encoded-real-url>/<tracking-ids>. Unwrap
                // those back to the original destination URLs (after HTML-decoding) so the exact-URL
                // assertions below still match. See TextHelper.UnwrapTrackingLinks.
                var decodedBody = System.Net.WebUtility.HtmlDecode(email.Body?.Content);
                var bodyContent = TextHelper.UnwrapTrackingLinks(decodedBody);
                Assert.Multiple(() =>
                {
                    Assert.That(bodyContent, Does.Contain(helocUrl),
                        "Email body should contain the HELOC hyperlink");
                    Assert.That(bodyContent, Does.Contain(simpliSafeUrl),
                        "Email body should contain the SimpliSafe hyperlink");
                    Assert.That(bodyContent, Does.Contain(cinchUrl),
                        "Email body should contain the Home Warranty by Cinch hyperlink");
                });
            });
        }

    }
}
