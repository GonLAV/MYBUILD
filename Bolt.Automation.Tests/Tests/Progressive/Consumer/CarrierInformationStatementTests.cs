using Bolt.Automation.Common;
using Bolt.Automation.Common.Enums;
using Bolt.Automation.Common.Logging.Extentions;
using Bolt.Automation.FrontEnds.Projects.HQXConsumer.Flows;
using Bolt.Automation.FrontEnds.Projects.HQXConsumer.Pages;
using Bolt.Automation.TestDataProvider.Providers.PlatformAPIDataProvider;
using Bolt.Automation.TestDataProvider.TestData.CoverageModificationTestData;
using Bolt.Automation.TestDataProvider.TestData.PlatformAPITestData;
using Bolt.Automation.Tests.TestExtension.Attributes;
using Bolt.Automation.Tests.TestExtension.Base;
using NUnit.Framework;
using AuthorAttribute = Bolt.Automation.Tests.TestExtension.Attributes.AuthorAttribute;

namespace Bolt.Automation.Tests.Tests.Progressive.Consumer
{
    public class CarrierInformationStatementTests : ProgressiveUITestBase
    {
        [Test]
        [RunIn(includeProduction: true)]
        [Tenant(Tenant.PROGRESSIVEPL)]
        [TestCaseId(248414)]
        [Category("Sanity")]
        [Category("HQX2")]
        [Author(Author.Helen)]
        [Description("Assurant carrier information statement: an MFH consumer quote (Anchorage, AK) returns Assurant as the selected carrier on Rates, and its information statement matches the American Bankers / Assurant manufactured-home copy.")]
        public async Task PGR_HQX2_MFH_Assurant_Carrier_Information_Statement()
        {
            ScopeContext.Set(ctx => ctx.CurrentUser, TestContextAccessor.CurrentUserCollection.Consumer);

            // Step 1 — Create a new consumer MFH quote for the Alaska manufactured-home address.
            // MFH prefill carries the manufactured-home answers so the short flow reaches Rates
            // without walking the manufactured-home interview by hand (the 3PQ page is fully
            // prefilled and skipped, and the Details/Discounts questions arrive answered).
            var request = QuoteStartPrefillDataProvider.GetMFHPrefillData(Addresses.GetAddress(AddressKey.AK_Anchorage));
            await _progressiveHelper.CallQuoteStartAsync(request, navigate: true);

            var overviewPage = await _progressiveHelper.HandleThreePQIfPresentAsync();

            // Step 2 — Submit the quote through to the Rates page.
            var ratesPage = await _logger.ExecuteStepAsync("Submit the quote through to the Rates page", async () =>
            {
                await Executor.ExecuteToPage<HQXConsumer_RatesPage>(FlowType.HQXShortFlow, overviewPage, fillForms: false);
                return PageFactory.CreatePage<HQXConsumer_RatesPage>();
            });

            // Step 3 — Assurant returned a rate: select it (via the "See other rates" comparison
            // table) and confirm it becomes the selected carrier on the main rate card. Assurant is
            // offered for this MFH quote but is not the default selection (American Modern is);
            // EnsureCarrierSelectedAsync throws if Assurant did not return a rate at all.
            await _logger.ExecuteStepAsync("Select Assurant and verify it is the selected carrier", async () =>
            {
                await ratesPage.EnsureCarrierSelectedAsync(CarrierEnums.Assurant.ToString());
                var selectedCarrier = await ratesPage.GetSelectedCarrierNameAsync();
                Assert.That(selectedCarrier, Is.EqualTo(CarrierEnums.Assurant.ToString()).IgnoreCase,
                    $"Assurant should be the selected carrier on the Rates page. Actual: '{selectedCarrier ?? "(none)"}'");
            });

            // Step 4 — The carrier information statement matches the Assurant / American Bankers copy.
            // The reader logs the full extracted text; the expected copy lives in the TestDataProvider
            // (CarrierInformationStatementData, keyed by carrier).
            await _logger.ExecuteStepAsync("Verify the Assurant carrier information statement text", async () =>
            {
                var statement = await ratesPage.GetCarrierInformationStatementAsync();
                Assert.That(statement, Is.EqualTo(CarrierInformationStatementData.GetExpectedStatement(CarrierEnums.Assurant)),
                    "The carrier information statement did not match the expected Assurant / American Bankers copy.");
            });
        }
    }
}
