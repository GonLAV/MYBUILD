using System.Globalization;
using Bolt.Automation.ApiClients.GetQuoteApi.Models.Application;
using Bolt.Automation.Common;
using Bolt.Automation.Common.Logging.Extentions;
using Bolt.Automation.Common.Models.TestData.DataModels.PersonalLine;
using Bolt.Automation.FrontEnds.FormData.Common;
using Bolt.Automation.FrontEnds.Projects.D2C.Pages;
using Bolt.Automation.TestDataProvider.Providers.GetQuoteApiDataProvider;
using Bolt.Automation.Tests.TestExtension.Attributes;
using NUnit.Framework;
using AuthorAttribute = Bolt.Automation.Tests.TestExtension.Attributes.AuthorAttribute;
using static Bolt.Automation.Common.Tenant;

namespace Bolt.Automation.Tests.Tests.D2C
{
    public class D2CPageSkippingTests : D2CTestBase
    {

        [Test]
        [RetryOnFailure(2)]
        [Author(Author.Gil)]
        [Tenant(BOLTAG)]
        [Category("D2C")][Category("Quoting")]
        [TestCaseId(187641)]
        [Description("Verify additional drivers page prevents navigation when required driver DOB is missing")]
        public async Task BOLTAG_D2C_Auto_AdditionalDrivers_Page_skipping()
        {
            var user = TestContextAccessor.CurrentUserCollection.D2CAutomation;
            ScopeContext.Set(ctx => ctx.CurrentUser, user);

            await _logger.ExecuteStepAsync("Create application via API and navigate to Additional Drivers page", async () =>
            {
                var requestData = new ApplicationRequestModel<PersonalLineData>
                {
                    Products = TestDataProvider.TestData.ApplicationTestData.ApplicationTestData.Products.PersonalAuto,
                    Data = PersonalLineDataProvider.GetMissingAdditionalDriversData()
                };

                await _d2cHelper.CreateApplicationAndNavigate(_getQuoteApi, requestData);

            });

            await _logger.ExecuteStepAsync("Verify continue button is disabled due to missing driver DOB", async () =>
            {
                var driversPage = PageFactory.CreatePage<D2C_AdditionalDriversPage>();
                var isContinueDisabled = await driversPage.IsContinueButtonDisabled();

                _logger.LogDataValidation("Continue Button Disabled", isContinueDisabled, "True", isContinueDisabled.ToString(), "Continue button should be disabled initially");
                Assert.That(isContinueDisabled, Is.True, "Continue button should be disabled initially");
            });

            await _logger.ExecuteStepAsync("Complete missing driver DOB and proceed to vehicles page", async () =>
            {
                var driversPage = PageFactory.CreatePage<D2C_AdditionalDriversPage>();
                var additionalDPopup = await driversPage.ClickOnSpecificDriverEditDetails("SECONDRIVE BOLT");
                var dob = DateTime.Today.AddYears(-30).ToString("MM/dd/yyyy", CultureInfo.InvariantCulture);
                await _pageHelper!.InteractWithField(FieldNames.DateOfBirth, dob);
                await additionalDPopup.ClickContinue();
                await additionalDPopup.ClickContinue();

                await driversPage.ClickContinue();
                var vehiclesPage = PageFactory.CreatePage<D2C_VehiclesPage>();
            });
        }

        [Test]
        [RetryOnFailure(2)]
        [Author(Author.Gil)]
        [Tenant(BOLTAG)]
        [Category("D2C")][Category("Quoting")]
        [TestCaseId(187642)]
        [Description("Verify vehicles page prevents navigation when required vehicle details are missing")]
        public async Task BOLTAG_D2C_Auto_AdditionalVehicles_Page_skipping()
        {
            var user = TestContextAccessor.CurrentUserCollection.D2CAutomation;
            ScopeContext.Set(ctx => ctx.CurrentUser, user);

            await _logger.ExecuteStepAsync("Create application via API and navigate to Vehicles page", async () =>
            {
                var data = PersonalLineDataProvider.GetMissingAdditionalDriversData();
                if (data.Drivers != null && data.Drivers.Count > 1)
                {
                    data.Drivers.RemoveAt(1);
                }

                var requestData = new ApplicationRequestModel<PersonalLineData>
                {
                    Products = TestDataProvider.TestData.ApplicationTestData.ApplicationTestData.Products.PersonalAuto,
                    Data = data
                };
                await _d2cHelper.CreateApplicationAndNavigate(_getQuoteApi, requestData);
            });

            await _logger.ExecuteStepAsync("Verify continue button is disabled due to missing vehicle details", async () =>
            {
                var vehiclesPage = PageFactory.CreatePage<D2C_VehiclesPage>();
                var isContinueDisabled = await vehiclesPage.IsContinueButtonDisabled();

                _logger.LogDataValidation("Continue Button Disabled", isContinueDisabled, "True", isContinueDisabled.ToString(), "Continue button should be disabled initially");
                Assert.That(isContinueDisabled, Is.True, "Continue button should be disabled initially");
            });

            await _logger.ExecuteStepAsync("Complete missing vehicle details and proceed to driver history", async () =>
            {
                var vehiclesPage = PageFactory.CreatePage<D2C_VehiclesPage>();

                var vehiclePopup = await vehiclesPage.ClickOnSpecificCarEditBtn("2008 TOYOTA CAMRY");
                await _pageHelper!.InteractWithField(FieldNames.VehicleOwnerShip, "Owned");
                await vehiclePopup.ClickContinue();

                vehiclePopup = await vehiclesPage.ClickOnSpecificCarEditBtn(" 2011 CHEVROLET TAHOE K1500 LS ");
                await _pageHelper!.InteractWithField(FieldNames.AnnualMileage, "1500");
                await vehiclePopup.ClickContinue();

                await vehiclesPage.ClickContinue();
                var historyPage = PageFactory.CreatePage<D2C_DriverHistoryPage>();
            });
        }

        [Test]
        [RetryOnFailure(2)]
        [Author(Author.Gil)]
        [Tenant(BOLTAG)]
        [Category("D2C")][Category("Quoting")]
        [TestCaseId(187708)]
        [Description("Verify page skipping validation across multiple LOB pages with missing data")]
        public async Task BOLTAG_D2C_MultiLob_Page_skipping()
        {
            var user = TestContextAccessor.CurrentUserCollection.D2CAutomation;
            ScopeContext.Set(ctx => ctx.CurrentUser, user);

            await _logger.ExecuteStepAsync("Create application via API with missing driver and vehicle data", async () =>
            {
                var requestData = new ApplicationRequestModel<PersonalLineData>
                {
                    Products = TestDataProvider.TestData.ApplicationTestData.ApplicationTestData.Products.PersonalAuto,
                    Data = PersonalLineDataProvider.GetMultiLobPageSkippingData()
                };

                await _d2cHelper.CreateApplicationAndNavigate(_getQuoteApi, requestData);
            });

            await _logger.ExecuteStepAsync("Fix missing driver DOB on Additional Drivers page", async () =>
            {
                var driversPage = PageFactory.CreatePage<D2C_AdditionalDriversPage>();
                var isContinueDisabled = await driversPage.IsContinueButtonDisabled();

                _logger.LogDataValidation("Continue Button Disabled (Drivers)", isContinueDisabled, "True", isContinueDisabled.ToString(), "Continue button should be disabled initially on drivers page");
                Assert.That(isContinueDisabled, Is.True, "Continue button should be disabled initially on drivers page");

                var driverPopup = await driversPage.ClickOnSpecificDriverEditDetails("SECONDRIVE BOLT");
                var dob = DateTime.Today.AddYears(-30).ToString("MM/dd/yyyy", CultureInfo.InvariantCulture);
                await _pageHelper!.InteractWithField(FieldNames.DateOfBirth, dob);
                await driverPopup.ClickContinue();
                await driverPopup.ClickContinue();

                await driversPage.ClickContinue();
            });

            await _logger.ExecuteStepAsync("Fix missing vehicle details on Vehicles page", async () =>
            {
                var vehiclesPage = PageFactory.CreatePage<D2C_VehiclesPage>();
                var isContinueDisabled = await vehiclesPage.IsContinueButtonDisabled();

                _logger.LogDataValidation("Continue Button Disabled (Vehicles)", isContinueDisabled, "True", isContinueDisabled.ToString(), "Continue button should be disabled initially on vehicles page");
                Assert.That(isContinueDisabled, Is.True, "Continue button should be disabled initially on vehicles page");

                var vehiclePopup = await vehiclesPage.ClickOnSpecificCarEditBtn("2008 TOYOTA CAMRY");
                await _pageHelper!.InteractWithField(FieldNames.VehicleOwnerShip, "Owned");
                await vehiclePopup.ClickContinue();

                await vehiclesPage.ClickContinue();
                var historyPage = PageFactory.CreatePage<D2C_DriverHistoryPage>();
            });
        }

        [Test]
        [RetryOnFailure(2)]
        [Author(Author.Gil)]
        [Tenant(BOLTAG)]
        [Category("D2C")][Category("Quoting")]
        [TestCaseId(187956)]
        [Description("Verify driver history page prevents navigation when accident details are incomplete")]
        public async Task BOLTAG_D2C_Driver_Losses_Page_skipping_Test()
        {
            var user = TestContextAccessor.CurrentUserCollection.D2CAutomation;
            ScopeContext.Set(ctx => ctx.CurrentUser, user);

            await _logger.ExecuteStepAsync("Create application via API with driver losses data", async () =>
            {
                var data = PersonalLineDataProvider.GetDriverLossesPageSkippingData();
                data.Drivers[0].DriverRelationshipToDriver1 = "Other";

                var requestData = new ApplicationRequestModel<PersonalLineData>
                {
                    Products = TestDataProvider.TestData.ApplicationTestData.ApplicationTestData.Products.PersonalAuto,
                    Data = data
                };

                await _d2cHelper.CreateApplicationAndNavigate(_getQuoteApi, requestData);
            });

            await _logger.ExecuteStepAsync("Verify continue button disabled and complete accident details", async () =>
            {
                var driverHistoryPage = PageFactory.CreatePage<D2C_DriverHistoryPage>();
                var isContinueDisabled = await driverHistoryPage.IsContinueButtonDisabled();

                _logger.LogDataValidation("Continue Button Disabled (Driver History)", isContinueDisabled, "True", isContinueDisabled.ToString(), "Continue button should be disabled initially on driver history page");
                Assert.That(isContinueDisabled, Is.True, "Continue button should be disabled initially on driver history page");

                await driverHistoryPage.AddIncidents("accidents", "Not At Fault", 1);

                await driverHistoryPage.ClickContinueButton();
                var coveragesPage = PageFactory.CreatePage<D2C_CoveragesPage>();
            });
        }
    }
}
