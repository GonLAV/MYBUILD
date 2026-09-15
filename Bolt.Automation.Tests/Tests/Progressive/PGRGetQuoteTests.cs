using Bolt.Automation.ApiClients.GetQuoteApi.Interfaces;
using Bolt.Automation.ApiClients.Infrastructure;
using Bolt.Automation.ApiClients.SSO;
using Bolt.Automation.Common;
using Bolt.Automation.Common.Enums;
using Bolt.Automation.Common.Logging.Extentions;
using Bolt.Automation.FrontEnds.Projects.HQXConsumer.Pages;
using Bolt.Automation.TestDataProvider.TestData.PlatformAPITestData;
using Bolt.Automation.Tests.TestExtension.Attributes;
using Bolt.Automation.Tests.TestExtension.Base;
using Bolt.Automation.Tests.TestHelpers.Progressive;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using static Bolt.Automation.FrontEnds.FormData.Common.FieldNames;
using AuthorAttribute = Bolt.Automation.Tests.TestExtension.Attributes.AuthorAttribute;

namespace Bolt.Automation.Tests.Tests.Progressive
{
    /// <summary>
    /// GetQuote API tests for Progressive personal lines — validates the new APPLICATION HEADER /
    /// SEARCH APPLICATIONS fields (friendlyId, preferredCarrierPremium, lockedByAgent, status).
    /// TC1–TC3 are focused regression tests; TC4 is a broader end-to-end smoke test.
    /// Consumer/agent role switching mirrors <see cref="KickOutTests"/>.
    /// </summary>
    public class PGRGetQuoteTests : ProgressiveUITestBase
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

        private void SetConsumerContext() =>
            ScopeContext.Set(ctx => ctx.CurrentUser, TestContextAccessor.CurrentUserCollection.Consumer);

        // TC1 — friendlyId is returned and stays consistent across GET APPLICATION HEADER and SEARCH APPLICATIONS.
        [Test]
        [RunIn(includeProduction: true)]
        [Tenant(Tenant.PROGRESSIVEPL)]
        [TestCaseId(249279)]
        [Category("GetQuoteApi")]
        [Category("Regression")]
        [Description("Verifies friendlyId is generated and returned consistently across GET APPLICATION HEADER (before and after submitting to Rates) and SEARCH APPLICATIONS.")]
        [Author(Author.Helen)]
        public async Task PGR_GQ_ApplicationHeader_FriendlyId_Consistent()
        {
            ScopeContext.Set(ctx => ctx.FrontEnd, FrontEndType.HQXConsumer);
            SetConsumerContext();

            var quote = await _progressiveHelper.CreateConsumerQuoteAndSettleAsync(Addresses.GetAddress(AddressKey.OR));

            var headerFriendlyId = await _logger.ExecuteStepAsync("GET APPLICATION HEADER returns a friendlyId", async () =>
            {
                var header = await _paaHelper.GetApplicationHeaderAsync(quote.ExternalId);
                Assert.That(header.QuoteFriendlyId, Is.Not.Null.And.Not.Empty, "friendlyId should be populated and non-empty");
                return header.QuoteFriendlyId!;
            });

            await _logger.ExecuteStepAsync("Consumer submits through to the Rates page", async () =>
            {
                await _progressiveHelper.SubmitToRatesPageAsync(quote.OverviewPage);
                var ratesPage = PageFactory.CreatePage<HQXConsumer_RatesPage>();
                var carrier = await ratesPage.GetSelectedCarrierNameAsync();
                Assert.That(carrier, Is.Not.Null.And.Not.Empty, "Rates page displayed but no carrier is shown");
            });

            await _logger.ExecuteStepAsync("GET APPLICATION HEADER returns the same friendlyId after submit", async () =>
            {
                var header = await _paaHelper.GetApplicationHeaderAsync(quote.ExternalId);
                Assert.That(header.QuoteFriendlyId, Is.EqualTo(headerFriendlyId), "friendlyId should remain consistent after the quote is submitted");
            });

            await _logger.ExecuteStepAsync("SEARCH APPLICATIONS returns the same friendlyId", async () =>
            {
                var request = PaaTestHelper.BuildSearchRequest(quote.LastName, quote.FriendlyId, quote.ZipCode);
                var record = await _paaHelper.SearchAndFindByExternalIdAsync(request, quote.ExternalId);
                Assert.That(record, Is.Not.Null, $"SEARCH APPLICATIONS did not return the application (externalId '{quote.ExternalId}')");
                Assert.That(record!.QuoteFriendlyId, Is.EqualTo(headerFriendlyId), "friendlyId in search results should match GET APPLICATION HEADER");
            });
        }

        // TC2 — preferredCarrierPremium always reflects the currently selected carrier/package.
        [Test]
        [RunIn(includeProduction: true)]
        [Tenant(Tenant.PROGRESSIVEPL)]
        [TestCaseId(249280)]
        [Category("GetQuoteApi")]
        [Category("Regression")]
        [Description("Verifies preferredCarrierPremium is null before submit, then tracks the selected premium through a carrier switch and package-tier changes, and matches SEARCH APPLICATIONS. Premiums captured dynamically from the UI.")]
        [Author(Author.Helen)]
        public async Task PGR_GQ_ApplicationHeader_PreferredCarrierPremium_TracksSelection()
        {
            ScopeContext.Set(ctx => ctx.FrontEnd, FrontEndType.HQXConsumer);
            SetConsumerContext();

            var quote = await _progressiveHelper.CreateConsumerQuoteAndSettleAsync(Addresses.GetAddress(AddressKey.OR));

            // Step 1 — Before the quote is submitted, no carrier is selected yet, so GET APPLICATION
            // HEADER returns preferredCarrierPremium as null ("cannot be found").
            await _logger.ExecuteStepAsync("GET APPLICATION HEADER before submit returns a null premium", async () =>
            {
                var header = await _paaHelper.GetApplicationHeaderAsync(quote.ExternalId);
                Assert.That(header.PreferredCarrierPremium, Is.Null,
                    $"preferredCarrierPremium should be null before the quote is submitted, but was '{header.PreferredCarrierPremium}'");
            });

            var ratesPage = await _logger.ExecuteStepAsync("Consumer submits through to the Rates page", async () =>
            {
                await _progressiveHelper.SubmitToRatesPageAsync(quote.OverviewPage);
                var page = PageFactory.CreatePage<HQXConsumer_RatesPage>();
                var carrier = await page.GetSelectedCarrierNameAsync();
                Assert.That(carrier, Is.Not.Null.And.Not.Empty, "Rates page displayed but no carrier is shown");
                return page;
            });

            var selectedPremium = await _logger.ExecuteStepAsync("Capture the initially selected premium", async () =>
            {
                var premium = await ratesPage.GetSelectedCarrierPremiumAsync();
                Assert.That(premium, Is.GreaterThan(0m), "Captured premium should be a positive amount");
                return premium;
            });

            var latestPremium = await _logger.ExecuteStepAsync("GET APPLICATION HEADER matches the selected premium", async () =>
            {
                var header = await _paaHelper.GetApplicationHeaderUntilAsync(quote.ExternalId, h => h.PreferredCarrierPremium == selectedPremium);
                Assert.That(header.PreferredCarrierPremium, Is.EqualTo(selectedPremium),
                    $"preferredCarrierPremium ({header.PreferredCarrierPremium}) should match the selected premium ({selectedPremium})");
                return selectedPremium;
            });

            latestPremium = await _logger.ExecuteStepAsync("Switch carrier, then GET APPLICATION HEADER matches the new premium", async () =>
            {
                var (_, carrierPremium) = await ratesPage.SwitchToAnyOtherCarrierAndGetPremiumAsync();
                Assert.That(carrierPremium, Is.Not.EqualTo(latestPremium), "Premium should change after switching carrier");

                var header = await _paaHelper.GetApplicationHeaderUntilAsync(quote.ExternalId, h => h.PreferredCarrierPremium == carrierPremium);
                Assert.That(header.PreferredCarrierPremium, Is.EqualTo(carrierPremium),
                    $"preferredCarrierPremium ({header.PreferredCarrierPremium}) should match the newly selected carrier premium ({carrierPremium})");
                return carrierPremium;
            });

            // Package-tier changes only apply when the selected carrier offers more than one tier.
            var tiers = await ratesPage.GetAvailablePackageTiersAsync();
            if (tiers.Count >= 2)
            {
                latestPremium = await _logger.ExecuteStepAsync("Change package tier, then GET APPLICATION HEADER matches the new package premium", async () =>
                {
                    var packagePremium = await ratesPage.SelectDifferentPackageTierAndGetPremiumAsync();
                    Assert.That(packagePremium, Is.Not.EqualTo(latestPremium), "Premium should change after changing package tier");

                    var header = await _paaHelper.GetApplicationHeaderUntilAsync(quote.ExternalId, h => h.PreferredCarrierPremium == packagePremium);
                    Assert.That(header.PreferredCarrierPremium, Is.EqualTo(packagePremium),
                        $"preferredCarrierPremium ({header.PreferredCarrierPremium}) should match the selected package premium ({packagePremium})");
                    return packagePremium;
                });
            }
            else
            {
                _logger.Info($"Selected carrier offers a single package tier ([{string.Join(", ", tiers)}]) — skipping package-tier change steps.");
            }

            await _logger.ExecuteStepAsync("SEARCH APPLICATIONS reflects the latest selected premium", async () =>
            {
                var request = PaaTestHelper.BuildSearchRequest(quote.LastName, quote.FriendlyId, quote.ZipCode);
                var record = await _paaHelper.SearchAndFindByExternalIdAsync(request, quote.ExternalId);
                Assert.That(record, Is.Not.Null, $"SEARCH APPLICATIONS did not return the application (externalId '{quote.ExternalId}')");
                Assert.That(record!.PreferredCarrierPremium, Is.EqualTo(latestPremium),
                    $"preferredCarrierPremium in search results ({record.PreferredCarrierPremium}) should match the latest selected premium ({latestPremium})");
            });
        }

        // TC3 — lockedByAgent reflects the quote lock state.
        [Test]
        [RunIn(includeProduction: true)]
        [Tenant(Tenant.PROGRESSIVEPL)]
        [TestCaseId(249281)]
        [Category("GetQuoteApi")]
        [Category("Regression")]
        [Description("Verifies lockedByAgent is false for a new application and becomes true once a PAA opens the quote and enters edit mode, consistently across GET APPLICATION HEADER and SEARCH APPLICATIONS.")]
        [Author(Author.Helen)]
        public async Task PGR_GQ_ApplicationHeader_LockedByAgent_ReflectsLockState()
        {
            ScopeContext.Set(ctx => ctx.FrontEnd, FrontEndType.HQXConsumer);
            SetConsumerContext();

            var quote = await _progressiveHelper.CreateConsumerQuoteAndSettleAsync(Addresses.GetAddress(AddressKey.OR));

            await _logger.ExecuteStepAsync("GET APPLICATION HEADER shows lockedByAgent false for a new application", async () =>
            {
                var header = await _paaHelper.GetApplicationHeaderAsync(quote.ExternalId);
                Assert.That(header.LockedByAgent, Is.False, "lockedByAgent should be false before any agent edit");
            });

            await _logger.ExecuteStepAsync("PAA opens the quote and enters edit mode", async () =>
            {
                await _paaHelper.OpenQuoteAsAgentAndEnterEditModeAsync(quote.ExternalId);
            });

            await _logger.ExecuteStepAsync("GET APPLICATION HEADER shows lockedByAgent true", async () =>
            {
                var header = await _paaHelper.GetApplicationHeaderUntilAsync(quote.ExternalId, h => h.LockedByAgent);
                Assert.That(header.LockedByAgent, Is.True, "lockedByAgent should be true while the agent has the quote in edit mode");
            });

            await _logger.ExecuteStepAsync("SEARCH APPLICATIONS shows lockedByAgent true", async () =>
            {
                var request = PaaTestHelper.BuildSearchRequest(quote.LastName, quote.FriendlyId, quote.ZipCode);
                var record = await _paaHelper.SearchAndFindByExternalIdAsync(request, quote.ExternalId);
                Assert.That(record, Is.Not.Null, $"SEARCH APPLICATIONS did not return the application (externalId '{quote.ExternalId}')");
                Assert.That(record!.LockedByAgent, Is.True, "lockedByAgent in search results should be true");
            });
        }

        // TC — preferredCarrierPremium is null in the Application Header after a DNQ (Did Not Qualify) decline.
        [Test]
        [RunIn(includeProduction: true)]
        [Tenant(Tenant.PROGRESSIVEPL)]
        [TestCaseId(249877)]
        [Category("GetQuoteApi")]
        [Category("Regression")]
        [Description("Verifies GET APPLICATION HEADER returns preferredCarrierPremium as null after the application is declined (DNQ) — driven by selecting the exotic-pets eligibility knockout on the Details page for an address outside Stillwater's appetite.")]
        [Author(Author.Helen)]
        public async Task PGR_GQ_ApplicationHeader_PreferredCarrierPremium_NullAfterDnq()
        {
            ScopeContext.Set(ctx => ctx.FrontEnd, FrontEndType.HQXConsumer);
            SetConsumerContext();

            // Step 1 — Create a new consumer application at an address outside Stillwater's appetite.
            var quote = await _progressiveHelper.CreateConsumerQuoteAndSettleAsync(Addresses.GetAddress(AddressKey.GA_Athens));

            // Steps 2 & 3 — Open the interview, proceed to the Details page, mark the exotic-pets
            // eligibility knockout True, and continue through to the DNQ (Did Not Qualify) page.
            // The parent "farm animals or exotic pets" checkbox (DOM id AnimalsOnThePremises_None)
            // reveals its exotic child; the child's DependsOn(parent) in the registry makes FillForm
            // fill the parent first, then the revealed child.
            await _logger.ExecuteStepAsync("Decline the quote via the exotic-pets knockout and land on the DNQ page", async () =>
            {
                var exoticPetsKnockoutAnswers = new Dictionary<string, string>
                {
                    [AnimalsOnThePremises_None] = "true",
                    [AnimalsOnThePremises_Exotic] = "true",
                };

                var dnqPage = await _progressiveHelper.SubmitToDnqPageAsync(quote.OverviewPage, exoticPetsKnockoutAnswers);
                Assert.That(dnqPage, Is.Not.Null, "Expected the DNQ (Did Not Qualify) page after selecting the exotic-pets knockout");
            });

            // Step 4 — GET APPLICATION HEADER returns preferredCarrierPremium as null for the declined application.
            await _logger.ExecuteStepAsync("GET APPLICATION HEADER returns preferredCarrierPremium as null", async () =>
            {
                var header = await _paaHelper.GetApplicationHeaderAsync(quote.ExternalId);
                Assert.That(header.PreferredCarrierPremium, Is.Null,
                    $"preferredCarrierPremium should be null after a DNQ decline, but was '{header.PreferredCarrierPremium}'");
            });
        }

        // TC4 — end-to-end validation of all three application-header fields across the full lifecycle.
        [Test]
        [RunIn(includeProduction: true)]
        [Tenant(Tenant.PROGRESSIVEPL)]
        [TestCaseId(249260)]
        [Category("GetQuoteApi")]
        [Category("Regression")]
        [Category("Smoke")]
        [Description("End-to-end: create → header (Incomplete/null premium/unlocked) → submit to Rates → premium set → switch selection → premium updates → PAA edit → lockedByAgent → SEARCH APPLICATIONS matches latest state.")]
        [Author(Author.Helen)]
        public async Task PGR_GQ_ApplicationHeader_Lifecycle_E2E()
        {
            ScopeContext.Set(ctx => ctx.FrontEnd, FrontEndType.HQXConsumer);
            SetConsumerContext();

            // Step 1 — Create a new consumer application (settled on Overview).
            var quote = await _progressiveHelper.CreateConsumerQuoteAndSettleAsync(Addresses.GetAddress(AddressKey.OR));

            // Step 2 — GET APPLICATION HEADER on the freshly created application.
            await _logger.ExecuteStepAsync("GET APPLICATION HEADER after create", async () =>
            {
                var header = await _paaHelper.GetApplicationHeaderAsync(quote.ExternalId);
                Assert.Multiple(() =>
                {
                    Assert.That(header.QuoteFriendlyId, Is.Not.Null.And.Not.Empty, "friendlyId should be populated");
                    Assert.That(header.PreferredCarrierPremium, Is.Null, "preferredCarrierPremium should be null before a quote is completed");
                    Assert.That(header.LockedByAgent, Is.False, "lockedByAgent should be false before any agent edit");
                    Assert.That(header.Status, Is.EqualTo("Incomplete"), "status should be Incomplete for a new application");
                });
            });

            // Step 3 — As Consumer, submit through to the Rates page.
            var ratesPage = await _logger.ExecuteStepAsync("Consumer submits through to the Rates page", async () =>
            {
                await _progressiveHelper.SubmitToRatesPageAsync(quote.OverviewPage);
                var page = PageFactory.CreatePage<HQXConsumer_RatesPage>();
                var carrier = await page.GetSelectedCarrierNameAsync();
                Assert.That(carrier, Is.Not.Null.And.Not.Empty, "Rates page displayed but no carrier is shown");
                return page;
            });

            // Step 4 — Capture the selected premium; GET APPLICATION HEADER reflects it.
            var selectedPremium = await _logger.ExecuteStepAsync("Capture selected premium and verify GET APPLICATION HEADER matches", async () =>
            {
                var premium = await ratesPage.GetSelectedCarrierPremiumAsync();
                Assert.That(premium, Is.GreaterThan(0m), "Captured premium should be a positive amount");

                var header = await _paaHelper.GetApplicationHeaderUntilAsync(quote.ExternalId, h => h.PreferredCarrierPremium == premium);
                Assert.That(header.PreferredCarrierPremium, Is.EqualTo(premium),
                    $"preferredCarrierPremium ({header.PreferredCarrierPremium}) should equal the selected premium ({premium})");
                return premium;
            });

            // Step 5/6 — Change the selection (switch carrier); GET APPLICATION HEADER reflects the update.
            var updatedPremium = await _logger.ExecuteStepAsync("Switch carrier and verify GET APPLICATION HEADER reflects the updated premium", async () =>
            {
                var (_, premium) = await ratesPage.SwitchToAnyOtherCarrierAndGetPremiumAsync();
                Assert.That(premium, Is.Not.EqualTo(selectedPremium), "Updated premium should differ after switching carrier");

                var header = await _paaHelper.GetApplicationHeaderUntilAsync(quote.ExternalId, h => h.PreferredCarrierPremium == premium);
                Assert.That(header.PreferredCarrierPremium, Is.EqualTo(premium),
                    $"preferredCarrierPremium ({header.PreferredCarrierPremium}) should reflect the updated selection ({premium})");
                return premium;
            });

            // Step 7 — As PAA, open the quote and click Edit.
            await _logger.ExecuteStepAsync("PAA opens the quote and enters edit mode", async () =>
            {
                await _paaHelper.OpenQuoteAsAgentAndEnterEditModeAsync(quote.ExternalId);
            });

            // Step 8 — GET APPLICATION HEADER shows the quote locked by the agent.
            await _logger.ExecuteStepAsync("GET APPLICATION HEADER shows lockedByAgent", async () =>
            {
                var header = await _paaHelper.GetApplicationHeaderUntilAsync(quote.ExternalId, h => h.LockedByAgent);
                Assert.That(header.LockedByAgent, Is.True, "lockedByAgent should be true while the agent has the quote in edit mode");
            });

            // Steps 9 & 10 — SEARCH APPLICATIONS returns the record with the latest state.
            await _logger.ExecuteStepAsync("SEARCH APPLICATIONS returns the record with the latest field state", async () =>
            {
                var request = PaaTestHelper.BuildSearchRequest(quote.LastName, quote.FriendlyId, quote.ZipCode);
                var record = await _paaHelper.SearchAndFindByExternalIdAsync(request, quote.ExternalId);
                Assert.That(record, Is.Not.Null, $"SEARCH APPLICATIONS did not return the application (externalId '{quote.ExternalId}')");

                Assert.Multiple(() =>
                {
                    Assert.That(record!.QuoteFriendlyId, Is.EqualTo(quote.FriendlyId), "friendlyId on the search record should match the application");
                    Assert.That(record.PreferredCarrierPremium, Is.EqualTo(updatedPremium), "preferredCarrierPremium on the search record should equal the updated premium");
                    Assert.That(record.LockedByAgent, Is.True, "lockedByAgent on the search record should be true");
                });
            });
        }
    }
}
