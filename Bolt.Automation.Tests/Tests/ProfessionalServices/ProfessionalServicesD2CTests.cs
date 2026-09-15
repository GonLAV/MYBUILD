using Bolt.Automation.ApiClients.GetQuoteApi.Interfaces;
using Bolt.Automation.ApiClients.Infrastructure;
using Bolt.Automation.Common;
using Bolt.Automation.Common.Configuration.InjectedConfig;
using Bolt.Automation.Common.Enums;
using Bolt.Automation.Common.Logging.Extentions;
using Bolt.Automation.FrontEnds.FormData.Common;
using Bolt.Automation.FrontEnds.Projects.D2C.Flows;
using Bolt.Automation.FrontEnds.Projects.D2C.Pages;
using Bolt.Automation.Tests.TestExtension.Attributes;
using Bolt.Automation.Tests.TestExtension.Base;
using Bolt.Automation.Tests.TestHelpers.D2C;
using Bolt.Automation.TestDataProvider.TestData.CommonTestData;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using AuthorAttribute = Bolt.Automation.Tests.TestExtension.Attributes.AuthorAttribute;

namespace Bolt.Automation.Tests.Tests.ProfessionalServices
{
    /// <summary>
    /// Smallest end-to-end Professional Services onboarding test — proves the
    /// <see cref="UIInjectionTestBase"/> foundation works against a newly onboarded tenant
    /// without depending on the static <c>(Tenant, Environment)</c>-keyed data stores.
    ///
    /// Ports the spirit of <c>BOLTAG_Agent_Sidebar_Logout_Test</c> from <c>AdbxSidebarNavigationTests</c>
    /// (smallest existing ADBX test that does login + a single UI action + assert + logging),
    /// but reads URL/credentials from <c>INJECTED_*</c> environment variables instead of
    /// <c>TestContextAccessor</c>.
    /// </summary>
    public class ProfessionalServicesD2CTests : UIInjectionTestBase
    {
        // Only the PartnerPortal section is required — Adbx/D2C env vars are not consumed
        // by this test, so the validator skips them.
        private static readonly HashSet<string> _requiredSections = new() { nameof(InjectedTestConfig.D2C) };
        protected override IReadOnlySet<string> RequiredSections => _requiredSections;
        protected IGetQuoteApi? _getQuoteApi;
        protected D2CTestHelpers _d2cHelper = null!;
        protected D2CLobNavigator _lobNavigator = null!;

        protected override void ResolveServices()
        {
            var refitApiLocator = _uiTestScope.ServiceProvider.GetRequiredService<RefitApiServiceLocator>();
            _getQuoteApi = refitApiLocator.GetService<IGetQuoteApi>();
        }

        /// <summary>
        /// Runs after the browser components exist, so <c>_pageHelper</c> and <c>Executor</c> are
        /// safe to hand over here.
        /// </summary>
        /// <remarks>
        /// <c>mainQueries</c> is left null on purpose: this suite validates a freshly onboarded
        /// tenant and must not depend on the tenant-keyed data stores. Only the members that never
        /// touch the database are called from here.
        /// </remarks>
        protected override void InitializeComponents()
        {
            _d2cHelper = new D2CTestHelpers(
                _logger, BrowserManager, PageFactory, _pageHelper!, ScopeContext, _getQuoteApi!, mainQueries: null);

            _lobNavigator = new D2CLobNavigator(Executor, PageFactory, _pageHelper!, _logger);
        }

        public ProfessionalServicesD2CTests() : base()
        {
            ScopeContext.Set(ctx => ctx.FrontEnd, FrontEndType.D2C);
            ScopeContext.Set(ctx => ctx.CurrentUrl, _injectedConfig.D2C.Url);
        }

        [Test]
        [Category("ProfessionalServices")]
        [TestCaseId(24024700)]
        [Author(Author.Sandy)]
        [TestCaseSource(nameof(StatesToVerify))]
        [InjectedParameter("INJECTED_PS_STATE")]
        [Description("Verify end-to-end Auto quote flow reaches rates page with at least one rated carrier, for the state configured via INJECTED_PS_STATE.")]
        public async Task ProfessionalServices_D2C_E2E_Auto_Reach_Rates_With_Available_Carriers_ByState(AddressKey addressKey)
        {
            var address = AddressData.GetAddress(addressKey);

            var formData = new Dictionary<string, string>
            {
                [FieldNames.OnlineAddress] = $"{address.AddressLine1}, {address.City}, {address.State} {address.ZipCode}"
            };

            var ratesPage = await _logger.ExecuteStepAsync($"Navigate through Auto flow to rates page for {address.State}", async () =>
            {
                return await Executor.Execute<D2C_YourAddressPage, D2C_RatesPage>(
                    FlowType.D2CAutoFlow,
                    formData: formData,
                    fillForms: true,
                    pagesToSkip: [typeof(D2C_CrossSellInformationPage)]
                );
            });

            await _logger.ExecuteStepAsync("Verify rated carriers are available", async () =>
            {
                var ratedCarriers = await ratesPage.GetAllRatedCarriers();

                Assert.That(ratedCarriers.Count, Is.GreaterThan(0), $"Should have rated carriers on Auto rates page for {address.State}");
            });
        }

        // Anchor ADO TC for the per-state scenario — inherited from the retired non-parameterized
        // sibling (same E2E flow; per-state execution is covered by the composite TestId suffix).
        // Declared TWICE on purpose, and the values must match the method-level [TestCaseId]:
        //  * [TestCaseId] on the method — what the orchestrator's discovery reads as the row id
        //    (per-case properties don't survive discovery for Ignore()d TestCaseData).
        //  * .SetProperty per case — what NUnit's runtime TestCaseId= filter and the Mongo run
        //    record key read. MUST stay a string: a non-string property makes NUnit's
        //    PropertyFilter throw at runtime for EVERY test in a TestCaseId-filtered run.
        private const string ByStateTestCaseId = "24024700";

        // Reads INJECTED_PS_STATE (a bare state code or a full AddressKey name) at discovery time
        // and resolves it via AddressData.ResolveStateKey. TestCaseSource runs before the test class
        // is constructed, so config is loaded fresh here rather than via the test instance's
        // _injectedConfig. When the env var is blank, yields exactly one Ignore()d default case so
        // the test stays discoverable (env-less orchestrator discovery cache) without ever silently
        // running against a state nobody chose. Every yielded case — including the ignored default —
        // is tagged with the same anchor TestCaseId; [InjectedParameter] + TestMetadataResolver's
        // composite TestId ("{TestCaseId}_{argsSignature}") is what keeps per-state Mongo run records
        // from colliding.
        private static IEnumerable<TestCaseData> StatesToVerify()
        {
            var config = InjectedConfigLoader.Load();
            var state = config.State.Trim();

            if (string.IsNullOrEmpty(state))
            {
                yield return new TestCaseData(AddressData.ResolveStateKey("TX"))
                    .SetProperty("TestCaseId", ByStateTestCaseId)
                    .Ignore("INJECTED_PS_STATE is not set — supply it locally or run per-state via the orchestrator");
                yield break;
            }

            yield return new TestCaseData(AddressData.ResolveStateKey(state))
                .SetProperty("TestCaseId", ByStateTestCaseId);
        }

        [Test]
        [Category("ProfessionalServices")]
        [TestCaseId(24024701)]
        [Author(Author.Sandy)]
        [TestCaseSource(nameof(StatesAndLobsToVerify))]
        [InjectedParameter("INJECTED_PS_STATE")]
        [InjectedParameter("INJECTED_PS_LOB")]
        [Description("Verify the end-to-end quote flow reaches the rates page with at least one rated carrier, for the state configured via INJECTED_PS_STATE and the line of business configured via INJECTED_PS_LOB.")]
        public async Task ProfessionalServices_D2C_E2E_Reach_Rates_With_Available_Carriers_ByStateAndLob(AddressKey addressKey, string lob)
        {
            var address = AddressData.GetAddress(addressKey);
            var lobFlow = D2CLobCatalog.Get(lob);
            var isBundle = lobFlow.ExpectedRatesLobs.Count > 1;

            // The LOB's own field values go in first so the address below always wins. Condominium
            // and Dwelling Fire are only reachable this way: the product picks the line of business
            // from what the interview is told, not from the flow alone.
            var formData = new Dictionary<string, string>(lobFlow.FormData ?? new Dictionary<string, string>())
            {
                [FieldNames.OnlineAddress] = $"{address.AddressLine1}, {address.City}, {address.State} {address.ZipCode}"
            };

            var ratesPage = await _logger.ExecuteStepAsync($"Navigate through {lob} flow to rates page for {address.State}", async () =>
                await _lobNavigator.NavigateToRatesAsync(lobFlow, formData));

            // A bundle lands on the combined "Bundle & Save" view, where the per-LOB carrier lists
            // are collapsed. Switching to Buy Separately is what makes each LOB's carriers — and
            // the LOB group titles asserted below — readable.
            if (isBundle)
            {
                await _logger.ExecuteStepAsync("Switch rates page to Buy Separately results", async () =>
                    await _d2cHelper.SwitchToBuySeparatelyIfNeeded(ratesPage));
            }

            await _logger.ExecuteStepAsync($"Verify rated carriers are available for {lob}", async () =>
            {
                // The two layouts need different readers. A single-LOB rates page renders carriers
                // as flat Call-Agent/Buy-Now buttons, which is all GetAllRatedCarriers matches. A
                // bundle nests them under a per-LOB group heading instead, so GetAllRatedCarriers
                // returns an empty list on a page that is in fact fully rated — asserting per group
                // is both the correct read AND proof the LOB split happened.
                if (isBundle)
                {
                    foreach (var expectedLob in lobFlow.ExpectedRatesLobs)
                    {
                        var lobCarriers = await ratesPage.GetRatedCallToPurchaseCarriersForLob(expectedLob);

                        Assert.That(lobCarriers.Count, Is.GreaterThan(0),
                            $"Should have rated {expectedLob} carriers in the {lob} bundle for {address.State}");
                    }

                    return;
                }

                var ratedCarriers = await ratesPage.GetAllRatedCarriers();

                Assert.That(ratedCarriers.Count, Is.GreaterThan(0), $"Should have rated carriers on the {lob} rates page for {address.State}");
            });
        }

        // Anchor ADO TC for the per-state-and-LOB scenario. Declared twice for the same reason as
        // ByStateTestCaseId above — see that comment.
        private const string ByStateAndLobTestCaseId = "24024701";

        // Two-axis sibling of StatesToVerify: reads INJECTED_PS_STATE and INJECTED_PS_LOB at
        // discovery time and yields exactly one case, because the orchestrator sets one value per
        // axis per work item — the cross-product is fanned out on its side, not here.
        //
        // The two axes deliberately behave differently when unset:
        //   * INJECTED_PS_STATE unset -> Ignore. There is no sensible default state; silently
        //     picking one would report a green run for a state nobody chose.
        //   * INJECTED_PS_LOB unset   -> Auto. That is what this suite ran before LOB was
        //     selectable, so saved runs and variable collections predating the LOB picker keep
        //     working untouched.
        // An unrecognised INJECTED_PS_LOB is an Ignore, never a fallback to Auto: running a
        // different product than the one the user picked is worse than not running.
        //
        // SetArgDisplayNames keeps the NUnit test name free of the quotes a string argument would
        // otherwise render — "...ByStateAndLob(TX_Crowley, Home)" rather than
        // "...ByStateAndLob(TX_Crowley, \"Home\")" — so TestMetadataResolver's composite TestId
        // comes out as "24024701_TX_Crowley__Home" (the doubled underscore is NUnit's ", "
        // separator) instead of the quote-laden "24024701_TX_Crowley___Home_".
        private static IEnumerable<TestCaseData> StatesAndLobsToVerify()
        {
            var config = InjectedConfigLoader.Load();
            var state = config.State.Trim();
            var lobInput = config.Lob.Trim();

            var canonicalLob = string.IsNullOrEmpty(lobInput)
                ? D2CLobCatalog.DefaultLob
                : D2CLobCatalog.Canonicalize(lobInput);

            if (canonicalLob is null)
            {
                yield return new TestCaseData(AddressData.ResolveStateKey("TX"), D2CLobCatalog.DefaultLob)
                    .SetArgDisplayNames("TX", D2CLobCatalog.DefaultLob)
                    .SetProperty("TestCaseId", ByStateAndLobTestCaseId)
                    .Ignore($"INJECTED_PS_LOB='{lobInput}' is not a supported line of business — supported inputs: {string.Join(", ", D2CLobCatalog.SupportedLobInputs())}");
                yield break;
            }

            if (string.IsNullOrEmpty(state))
            {
                yield return new TestCaseData(AddressData.ResolveStateKey("TX"), canonicalLob)
                    .SetArgDisplayNames("TX", canonicalLob)
                    .SetProperty("TestCaseId", ByStateAndLobTestCaseId)
                    .Ignore("INJECTED_PS_STATE is not set — supply it locally or run per-state via the orchestrator");
                yield break;
            }

            var addressKey = AddressData.ResolveStateKey(state);

            yield return new TestCaseData(addressKey, canonicalLob)
                .SetArgDisplayNames(addressKey.ToString(), canonicalLob)
                .SetProperty("TestCaseId", ByStateAndLobTestCaseId);
        }


        //used external variables
        //        <RunSettings>
        //	<RunConfiguration>
        //		<EnvironmentVariables>
        //			<INJECTED_TENANT>BOLTAG</INJECTED_TENANT>
        //			<INJECTED_ENVIRONMENT>Qa</INJECTED_ENVIRONMENT>
        //			<INJECTED_D2C_URL>https://d2cinterview-qa.boltqa.com/D2CAutomation</INJECTED_D2C_URL>
        //			<INJECTED_PS_STATE>TX</INJECTED_PS_STATE> <!-- only needed for the _ByState tests; single state code or full AddressKey name -->
        //			<INJECTED_PS_LOB>Home</INJECTED_PS_LOB> <!-- only read by _ByStateAndLob; Auto | Home | Renters | HomeAuto (aliases: "Home + Auto", HO3, HO4...). Unset = Auto -->
        //	</EnvironmentVariables>
        //	</RunConfiguration>
        //</RunSettings>
    }
}
