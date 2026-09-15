using Bolt.Automation.Common;
using Bolt.Automation.Common.Logging.Extentions;
using Bolt.Automation.FrontEnds.FormData.Common;
using Bolt.Automation.FrontEnds.Projects.D2C.Pages;
using Bolt.Automation.Tests.TestExtension.Attributes;
using NUnit.Framework;
using AuthorAttribute = Bolt.Automation.Tests.TestExtension.Attributes.AuthorAttribute;
using FlowType = Bolt.Automation.FrontEnds.Projects.D2C.Flows.FlowType;
using static Bolt.Automation.Common.Tenant;

namespace Bolt.Automation.Tests.Tests.D2C
{
    public class D2CCrossSellTests : D2CTestBase
    {

        [Test]
        [RetryOnFailure(2)]
        [Author(Author.Gil)]
        [Tenant(BOLTAG)]
        [Category("D2C")][Category("Quoting")]
        [TestCaseId(160243)]
        [Description("Verify HO3 policy can cross-sell Auto insurance with bundle selection")]
        public async Task BOLTAG_D2C_HO3_CrossSell_Auto()
        {
            var personalDetailsPage = await _logger.ExecuteStepAsync("Navigate through Home flow with bundle selection", async () =>
            {
                return await Executor.Execute<D2C_YourAddressPage, D2C_PersonalDetailsPage>(
                    FlowType.D2CHomeFlow,
                    fillForms: true,
                    pagesToSkip: [typeof(D2C_PropertiesUsagePage)],
                    pageCallbacks: _d2cHelper.CreateBundleSelectionCallback()
                );
            });

            var friendlyId = await personalDetailsPage.GetFriendlyId();
            var ratesPage = await _logger.ExecuteStepAsync("Complete Home + Auto flow to rates page", async () =>
            {
                return await Executor.ExecuteToPage<D2C_RatesPage>(
                    FlowType.D2CHomeAutoFlow,
                    personalDetailsPage,
                    fillForms: true
                );
            });

            await _d2cHelper.ValidateLobsOnRatesPage(ratesPage, new List<string> { "Auto", "Homeowners" });
            await _d2cHelper.ValidateOwnershipType(friendlyId, "Owned");
        }

        [Test]
        [RetryOnFailure(2)]
        [Author(Author.Gil)]
        [Tenant(BOLTAG)]
        [Category("D2C")][Category("Quoting")]
        [TestCaseId(165841)]
        [Description("Verify Condo policy can cross-sell Auto insurance and retrieve application")]
        public async Task BOLTAG_D2C_Condo_CrossSell_Auto_And_Retrieval()
        {
            var personalDetailsPage = await _logger.ExecuteStepAsync("Navigate through Condo flow with bundle selection", async () =>
            {
                return await Executor.Execute<D2C_YourAddressPage, D2C_PersonalDetailsPage>(
                    FlowType.D2CCondoFlow,
                    new Dictionary<string, string>
                    {
                        [FieldNames.PLTypeOfDwelling] = "Condominium",
                    },
                    fillForms: true,
                    pagesToSkip: [typeof(D2C_PropertiesUsagePage)],
                    pageCallbacks: _d2cHelper.CreateBundleSelectionCallback()
                );
            });

            var friendlyId = await personalDetailsPage.GetFriendlyId();
            await _d2cHelper.OpenRetrievalUrl(friendlyId);

            var ratesPage = await _logger.ExecuteStepAsync("Complete Condo + Auto flow to rates page", async () =>
            {
                return await Executor.ExecuteToPage<D2C_RatesPage>(
                    FlowType.D2CCondoAutoFlow,
                    PageFactory.CreatePage<D2C_YourAddressPage>(),
                    new Dictionary<string, string>
                    {
                        [FieldNames.PLTypeOfDwelling] = "Condominium",
                    },
                    fillForms: true,
                    pagesToSkip: [typeof(D2C_LobsPage), typeof(D2C_PropertiesUsagePage)],
                    pageCallbacks: _d2cHelper.CreateBundleSelectionCallback()
                );
            });

            // Handle bundle results if displayed
            await _d2cHelper.SwitchToBuySeparatelyIfNeeded(ratesPage);

            await _d2cHelper.ValidateLobsOnRatesPage(ratesPage, new List<string> { "Auto", "Condominium" });
            await _d2cHelper.ValidateSetLobs(friendlyId, ["PersonalAuto", "Condominium"]);
        }

        [Test]
        [RetryOnFailure(2)]
        [Author(Author.Gil)]
        [Tenant(BOLTAG)]
        [Category("D2C")][Category("Quoting")]
        [TestCaseId(162615)]
        [Description("Verify Dwelling Fire policy can cross-sell Auto insurance")]
        public async Task BOLTAG_D2C_DF_CrossSell_Auto()
        {
            var personalDetailsPage = await _logger.ExecuteStepAsync("Navigate through Dwelling Fire flow with bundle selection", async () =>
            {
                return await Executor.Execute<D2C_YourAddressPage, D2C_PersonalDetailsPage>(
                    FlowType.D2CHomeFlow,
                    new Dictionary<string, string>
                    {
                        [FieldNames.IsPrimaryResidence] = "No",
                    },
                    fillForms: true,
                    pageCallbacks: _d2cHelper.CreateBundleSelectionCallback()
                );
            });

            var friendlyId = await personalDetailsPage.GetFriendlyId();
            await _d2cHelper.OpenRetrievalUrl(friendlyId);

            var ratesPage = await _logger.ExecuteStepAsync("Complete Dwelling Fire + Auto flow to rates page", async () =>
            {
                return await Executor.ExecuteToPage<D2C_RatesPage>(
                    FlowType.D2CHomeAutoFlow,
                    PageFactory.CreatePage<D2C_YourAddressPage>(),
                    new Dictionary<string, string>
                    {
                        [FieldNames.IsPrimaryResidence] = "No",
                    },
                    fillForms: true,
                    pagesToSkip: [typeof(D2C_LobsPage)],
                    pageCallbacks: _d2cHelper.CreateBundleSelectionCallback()
                );
            });

            await _d2cHelper.ValidateLobsOnRatesPage(ratesPage, new List<string> { "Dwelling Fire", "Auto" });
        }

        [Test]
        [RetryOnFailure(2)]
        [Author(Author.Gil)]
        [Tenant(BOLTAG)]
        [Category("D2C")][Category("Quoting")]
        [TestCaseId(158631)]
        [Description("Verify Auto policy can cross-sell HO3 homeowners insurance")]
        public async Task BOLTAG_D2C_Auto_CrossSell_HO3()
        {
            var crossSellInformationPage = await _logger.ExecuteStepAsync("Navigate through Auto flow to cross-sell page", async () =>
            {
                return await Executor.Execute<D2C_YourAddressPage, D2C_CrossSellInformationPage>(
                    FlowType.D2CAutoFlow,
                    fillForms: true
                );
            });

            await _logger.ExecuteStepAsync("Select bundle and retrieve friendly ID", async () =>
            {
                await crossSellInformationPage.SelectBundleBox();
            });

            var houseDetailPage = PageFactory.CreatePage<D2C_HouseDetailsPage>();
            var friendlyId = await houseDetailPage.GetFriendlyId();

            await _d2cHelper.OpenRetrievalUrl(friendlyId);

            await _logger.ExecuteStepAsync("Navigate through retrieved Auto flow", async () =>
            {
                var addressPage = PageFactory.CreatePage<D2C_YourAddressPage>();
                await addressPage.ClickContinue();

                var primaryVehiclePage = PageFactory.CreatePage<D2C_PrimaryDriverPage>();
                await primaryVehiclePage.ClickContinue();
                await primaryVehiclePage.ClickContinue();
            });

            var ratesPage = await _logger.ExecuteStepAsync("Complete Auto + Home flow to rates page", async () =>
            {
                return await Executor.ExecuteToPage<D2C_RatesPage>(
                    FlowType.D2CAutoHomeFlow,
                    PageFactory.CreatePage<D2C_AdditionalDriversPage>(),
                    fillForms: true,
                    pageCallbacks: _d2cHelper.CreateBundleSelectionCallback()
                );
            });

            await _d2cHelper.ValidateLobsOnRatesPage(ratesPage, new List<string> { "Auto", "Homeowners" });
            await _d2cHelper.ValidateOwnershipType(friendlyId, "Owned");
        }
    }
}

