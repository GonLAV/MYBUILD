using Bolt.Automation.Tests.TestExtension.Base;

namespace Bolt.Automation.Tests.Tests.Progressive;

public class CTETests : ProgressiveUITestBase
{
//    public static IEnumerable<TestCaseData> StatesCarriersCombination
//    {
//        get
//        {
//            yield return new TestCaseData(AddressKey.AZ, CarrierEnums.Homesite).SetProperty("TestCaseId", "170567");
//            yield return new TestCaseData(AddressKey.IL_Peoria, CarrierEnums.Homesite).SetProperty("TestCaseId", "170568");
//        }
//    }

//    [Test]
//    [RunIn(includeProduction: true)]
//    [Tenant(Tenant.PROGRESSIVEPL)]
//    [TestCaseSource(nameof(StatesCarriersCombination))]
//    [Description("Testing dropdown values in Custom Package State-Carrier-Coverage permutations")]
//    public async Task PGR_HQX2_CovMod_Dropdown_Values(AddressKey addressKey, CarrierEnums carrier)
//    {
//        var address = Addresses.GetAddress(addressKey);
//        var testCaseId = TestContext.CurrentContext.Test.Properties.Get("TestCaseId");
//        await _logger.ExecuteStepAsync($"Test Start - TestCaseId: {testCaseId} | Address: {address?.Addr1}, {address?.StateProvCd} | Carrier: {carrier}", async () =>
//        {
//            ScopeContext.Set(ctx => ctx.CurrentUser, TestContextAccessor.CurrentUserCollection.Consumer);

//            var request = QuoteStartPrefillDataProvider.GetFullPrefillData(address);
//            var (externalId, startUrl) = await _progressiveHelper.CallQuoteStartAsync(request);
//            _logger.Info($"QuoteStart API response received. WebsiteURL: {startUrl}. ExternalId {externalId}");

//            await _logger.ExecuteStepAsync("Navigate to Rates Page", async () =>
//            {
//                await _progressiveHelper.NavigateToRatesPageAsync();
//                PageFactory.CreatePage<HQXConsumer_RatesPage>();
//            });

//        });
//    }
}
