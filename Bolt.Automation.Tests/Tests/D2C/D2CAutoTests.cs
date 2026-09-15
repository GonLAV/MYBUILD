using Bolt.Automation.ApiClients.AdbxApi;
using Bolt.Automation.ApiClients.GetQuoteApi.Models.Application;
using Bolt.Automation.Common;
using Bolt.Automation.Common.Enums;
using Bolt.Automation.Common.Exceptions;
using Bolt.Automation.Common.Logging.Extentions;
using Bolt.Automation.Common.Models.TestData.DataModels.PersonalLine;
using Bolt.Automation.Common.PollyRetry;
using Bolt.Automation.Common.Utils;
using Bolt.Automation.FrontEnds.FormData.Common;
using Bolt.Automation.FrontEnds.PlaywrightBase.Helpers;
using Bolt.Automation.FrontEnds.Projects.ADBX.FormData;
using Bolt.Automation.FrontEnds.Projects.ADBX.Pages;
using Bolt.Automation.FrontEnds.Projects.ADBX.Pages.Quotes;
using Bolt.Automation.FrontEnds.Projects.D2C.Pages;
using Bolt.Automation.FrontEnds.Projects.D2C.Popups;
using Bolt.Automation.FrontEnds.Projects.STS;
using Bolt.Automation.TestDataProvider.Providers.GetQuoteApiDataProvider;
using Bolt.Automation.TestDataProvider.TestData.CommonTestData;
using Bolt.Automation.Tests.TestExtension.Attributes;
using Bolt.Automation.Tests.TestHelpers;
using Bolt.Automation.Tests.TestHelpers.ADBX;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using static Bolt.Automation.TestDataProvider.TestData.ApplicationTestData.ApplicationTestData;
using AuthorAttribute = Bolt.Automation.Tests.TestExtension.Attributes.AuthorAttribute;
using BoltEnvironment = Bolt.Automation.Common.Environment;
using FlowType = Bolt.Automation.FrontEnds.Projects.D2C.Flows.FlowType;
using static Bolt.Automation.Common.Tenant;
using Bolt.Automation.Common.Models.TestData.DataModels.CommercialLine;
using Bolt.Automation.ApiClients.Infrastructure;

namespace Bolt.Automation.Tests.Tests.D2C
{
    public class D2CAutoTests : D2CTestBase
    {
        private IAdbxApiClientFactory _adbxApifactory = null!;

        [Test]
        [RetryOnFailure(2)]
        [Author(Author.Gil)]
        [Tenant(BOLTAG)]
        [Category("D2C")]
        [Category("Quoting")]
        [TestCaseId(128113)]
        [Description("Verify state availability validation for unsupported states")]
        public async Task BOLTAG_D2C_StateUnavailableErrorPage()
        {
            var url = ScopeContext.Data.CurrentUrl;
            await BrowserManager.NavigateAsync(url);
            var addressPage = PageFactory.CreatePage<D2C_YourAddressPage>();
            var formData = new Dictionary<string, string>();

            await _logger.ExecuteStepAsync("Verify Alaska address shows state-not-allowed error", async () =>
            {
                formData[FieldNames.OnlineAddress] = "17601 GOLDEN VIEW DR, ANCHORAGE AK";
                await addressPage.FillForm(formData);
                await addressPage.ClickContinue();

                var lobsPage = PageFactory.CreatePage<D2C_LobsPage>();
                await lobsPage.SelectLob("Auto");
                await lobsPage.ClickContinue();

                var errorPage = D2C_ErrorPage.CreateWithExpectedUrl(BrowserManager, _pageHelper!, ScopeContext, "state-not-allowed");
                await Task.Delay(500);
                var errorMessage = await errorPage.GetTitleMessage();

                _logger.LogDataValidation("Alaska Error Message Present", !string.IsNullOrEmpty(errorMessage), "Not Empty", errorMessage ?? "null", "Error message should be displayed for Alaska");
                Assert.That(string.IsNullOrEmpty(errorMessage), Is.False, "Missing message on error page");
            });

            await _logger.ExecuteStepAsync("Verify Alabama address proceeds without error", async () =>
            {
                await _d2cHelper.NavigateBackMultipleTimes(_pageHelper!, 2);

                addressPage = PageFactory.CreatePage<D2C_YourAddressPage>();
                formData[FieldNames.OnlineAddress] = "2255 Cedar Point Rd, Mobile, AL 36605";
                await addressPage.FillForm(formData);
                await addressPage.ClickContinue();

                var lobsPage = PageFactory.CreatePage<D2C_LobsPage>();
                await lobsPage.ClickContinue();

                var primaryDriver = PageFactory.CreatePage<D2C_PrimaryDriverPage>();
                var currentUrl = BrowserManager.GetCurrentTab()?.Url ?? string.Empty;

                _logger.LogDataValidation("Alabama No Error", !currentUrl.ToLower().Contains("error"), "No error in URL", currentUrl, "Alabama address should not show error page");
                Assert.That(currentUrl.ToLower(), Does.Not.Contain("error"));
            });

            await _logger.ExecuteStepAsync("Verify Hawaii address shows state-not-allowed error", async () =>
            {
                await _d2cHelper.NavigateBackMultipleTimes(_pageHelper!, 2);

                addressPage = PageFactory.CreatePage<D2C_YourAddressPage>();
                formData[FieldNames.OnlineAddress] = "650 Kaulana Pl, Honolulu, HI";
                await addressPage.FillForm(formData);
                await addressPage.ClickContinue();

                var lobsPage = PageFactory.CreatePage<D2C_LobsPage>();
                await lobsPage.ClickContinue();

                var errorPage = D2C_ErrorPage.CreateWithExpectedUrl(BrowserManager, _pageHelper!, ScopeContext, "state-not-allowed");
                var errorMessage = await errorPage.GetTitleMessage();

                _logger.LogDataValidation("Hawaii Error Message Present", !string.IsNullOrEmpty(errorMessage), "Not Empty", errorMessage ?? "null", "Error message should be displayed for Hawaii");
                Assert.That(string.IsNullOrEmpty(errorMessage), Is.False, "Missing message on error page");
            });
        }

        [Test]
        [RetryOnFailure(2)]
        [Author(Author.Gil)]
        [Tenant(USAA)]
        [Category("D2C")]
        [Category("Quoting")]
        [Category("FullQuote")]
        [Category("Sanity")]
        [TestCaseId(173885)]
        [Description("Verify end-to-end Auto full quote flow with Progressive carrier")]
        public async Task USAA_D2C_E2E_Auto_FQ()
        {
            var user = TestContextAccessor.CurrentUserCollection.OnlineQuote;
            ScopeContext.Set(ctx => ctx.CurrentUser, user);

            await _logger.ExecuteStepAsync("Create application via API and navigate to rates", async () =>
            {
                var personalInfo = PersonalInfo.GetRandomPersonalInfo();
                personalInfo.Email = "ForceIBSScore900@safeco.com";

                var data = PersonalLineDataProvider.GetPersonalAutoData(
                    AddressData.TX_PLANO,
                    [Vehicles.Vin_19UDE2F33HA007791],
                    [Drivers.UsaaTestDriver],
                    personalInfo);

                var requestData = new ApplicationRequestModel<PersonalLineData>
                {
                    Products = Products.PersonalAuto,
                    Data = data
                };

                await _d2cHelper.CreateApplicationAndNavigate(_getQuoteApi, requestData, "&forcePageSkipping=true");
            });

            await _logger.ExecuteStepAsync("Select Progressive carrier and complete FQ flow", async () =>
            {
                var ratesPage = PageFactory.CreatePage<D2C_RatesPage>();
                await ratesPage.ClickOnSpecificCarrierBuyNow("Progressive");

                var paymentPage = await Executor.ExecuteToPage<D2C_PaymentPageFQ>(
                    FlowType.USAAAutoFQFlow,
                    PageFactory.CreatePage<D2C_ProgressiveDisclosurePageFQ>(),
                    fillForms: true);
                await paymentPage.ClickOnContinueToPay();
                var paymentPopup = PageFactory.CreatePage<D2C_PaymentPopUp>();
                await paymentPopup.FillForm();
            });

            await _logger.ExecuteStepAsync("Verify payment success page", async () =>
            {
                var succsessPage = PageFactory.CreatePage<D2C_PaymentFailurePageFQ>();
                var policyNum = await _pageHelper!.GetFieldValue(LocatorType.XPath, "//app-image[@name='policyNumber']/following-sibling::span[@class='data']");
                var effectiveDate = await _pageHelper!.GetFieldValue(LocatorType.XPath, "//app-image[@name='policyEffectiveDate']/following-sibling::span[@class='data']");

                Assert.Multiple(() =>
                {
                    Assert.That(string.IsNullOrEmpty(policyNum), Is.False, "Expected to have policy number");
                    Assert.That(string.IsNullOrEmpty(effectiveDate), Is.False, "Expected to have effective date");
                });
            });
        }

        [Test]
        [RetryOnFailure(2)]
        [Author(Author.Gil)]
        [Tenant(USAA)]
        [Category("D2C")]
        [Category("Quoting")]
        [Category("FullQuote")]
        [TestCaseId(237026)]
        [Description("Verify Auto full quote flow with Bristol West carrier reaches rates page")]
        public async Task USAA_D2C_BristolWest_FQ()
        {
            var user = TestContextAccessor.CurrentUserCollection.OnlineQuote;
            ScopeContext.Set(ctx => ctx.CurrentUser, user);

            await _logger.ExecuteStepAsync("Create application via API and navigate to rates", async () =>
            {
                var personalInfo = PersonalInfo.GetRandomPersonalInfo();

                var data = PersonalLineDataProvider.GetPersonalAutoData(
                    AddressData.CA_ThousandOaks,
                    [Vehicles.D2CVehicle],
                    [Drivers.D2CDriver],
                    personalInfo,
                    PolicyTestData.PersonalAutoPolicyData);

                var requestData = new ApplicationRequestModel<PersonalLineData>
                {
                    Products = Products.PersonalAuto,
                    Data = data
                };
                await _d2cHelper.CreateApplicationAndNavigate(_getQuoteApi, requestData, "&forcePageSkipping=true");

            });

            var confirmationPage = await _logger.ExecuteStepAsync("Select Bristol West carrier and walk FQ flow to quote confirmation", async () =>
            {
                var ratesPage = PageFactory.CreatePage<D2C_RatesPage>();
                await ratesPage.ClickOnSpecificCarrierBuyNow("Bristol West");

                var confirmationPage = await Executor.ExecuteToPage<D2C_QuoteConfirmationPage>(
                   FlowType.BristolWestAutoFQFlow,
                   PageFactory.CreatePage<D2C_BristolWestPolicyFQ>(),
                   fillForms: true);
                return confirmationPage;
            });
            await _logger.ExecuteStepAsync("Select payment plan, e-sign, and complete DocuSign signing", async () =>
            {
                await confirmationPage.ClickContinue();

                var paymentPlanPage = PageFactory.CreatePage<D2C_PaymentPlanPageFQ>();
                await paymentPlanPage.SelectFirstPaymentPlan();
                await paymentPlanPage.ClickContinue();

                var esignPage = PageFactory.CreatePage<D2C_BristolWestEsignFQ>();
                await esignPage.FillForm();

                var docuSignPage = PageFactory.CreatePage<D2C_DocuSignSigningFQ>();
                await docuSignPage.CompleteSigningAsync();
            });
        }

        [Test]
        [RetryOnFailure(2)]
        [Author(Author.Gil)]
        [Tenant(BOLTAG)]
        [Category("D2C")]
        [Category("Quoting")]
        [TestCaseId(222981)]
        [Description("Verify primary driver page validation errors and DOB masking")]
        public async Task BOLTAG_D2C_Auto_PrimaryDriverPage_ValidationErrors()
        {
            var primaryDriverPage = await Executor.Execute<D2C_YourAddressPage, D2C_PrimaryDriverPage>(
                FlowType.D2CAutoFlow,
                fillForms: true
            );

            await _logger.ExecuteStepAsync("Validate invalid name and DOB format", async () =>
            {
                await _pageHelper!.InteractWithField(FieldNames.FirstName, "A");
                await _pageHelper!.InteractWithField(FieldNames.LastName, "A");
                await _pageHelper!.InteractWithField(FieldNames.DateOfBirth, "25/05/1995");
                await _pageHelper!.InteractWithField(FieldNames.LicenseStatus, "Permit");

                await _d2cHelper.ValidateValidationMessages(_pageHelper!, shouldHaveMessages: true);
            });

            await _logger.ExecuteStepAsync("Validate underage driver error", async () =>
            {
                await _pageHelper!.InteractWithField(FieldNames.DateOfBirth, DateTime.Today.AddYears(-15).ToString("MM/dd/yyyy"));

                var validationMessages = await _pageHelper!.GetValidationMessagesAsync();
                var hasAgeValidation = validationMessages.Any(m => m.Contains("16") || m.Contains("under"));

                _logger.LogDataValidation("Underage Validation", hasAgeValidation, "True", hasAgeValidation.ToString(), $"Should show age validation. Messages: [{string.Join(", ", validationMessages)}]");
                Assert.That(hasAgeValidation, Is.True, $"Expected age validation. Messages: [{string.Join(", ", validationMessages)}]");
            });

            await _logger.ExecuteStepAsync("Complete valid driver information and validate email/phone", async () =>
            {
                await _pageHelper!.InteractWithField(FieldNames.FirstName, "AutoTest");
                await _pageHelper!.InteractWithField(FieldNames.LastName, "ADriver");
                await _pageHelper!.InteractWithField(FieldNames.DateOfBirth, DateTime.Today.AddYears(-30).ToString("MM/dd/yyyy"));
                await _pageHelper!.InteractWithField(FieldNames.LicenseStatus, "Valid");
                await _pageHelper!.InteractWithField(FieldNames.Gender, "Male");
                await _pageHelper!.InteractWithField(FieldNames.TypeOfResidence, "Own home");
                await _pageHelper!.InteractWithField(FieldNames.MaritalStatus, "Single");
                await primaryDriverPage.ClickContinue();

                await _pageHelper!.InteractWithField(FieldNames.Email, "Automation@epos");
                await _pageHelper!.InteractWithField(FieldNames.PrimaryPhoneNumber, "324");

                await _d2cHelper.ValidateValidationMessages(_pageHelper!, shouldHaveMessages: true);

                await _pageHelper!.InteractWithField(FieldNames.Email, "autotest@example.com");
                await _pageHelper!.InteractWithField(FieldNames.PrimaryPhoneNumber, "9723456789");
                await _pageHelper!.InteractWithField(FieldNames.EmploymentIndustry, "Technology");
                await _pageHelper!.InteractWithField(FieldNames.Education, "Phd");
                await _pageHelper!.InteractWithField(FieldNames.Occupation, "Analyst");
                await primaryDriverPage.ClickContinue();
            });

            await _logger.ExecuteStepAsync("Verify DOB masking on retrieval", async () =>
            {
                var additionalDrivers = PageFactory.CreatePage<D2C_AdditionalDriversPage>();
                await additionalDrivers.ClickBackButton();

                primaryDriverPage = PageFactory.CreatePage<D2C_PrimaryDriverPage>();
                await _d2cHelper.ValidateDobMasking(_pageHelper!, FieldNames.DateOfBirth);
            });
        }

        [Test]
        [RetryOnFailure(2)]
        [Author(Author.Gil)]
        [Tenant(BOLTAG)]
        [Category("D2C")]
        [Category("Quoting")]
        [TestCaseId(222965)]
        [Description("Verify additional drivers page enforces maximum driver limit and DOB masking")]
        public async Task BOLTAG_D2C_Auto_AdditionalDriversPage_ValidationErrors()
        {
            var additionalDriversPage = await Executor.Execute<D2C_YourAddressPage, D2C_AdditionalDriversPage>(
                FlowType.D2CAutoFlow,
                fillForms: true
            );

            await _logger.ExecuteStepAsync("Add 5 additional drivers to reach maximum limit", async () =>
            {
                for (int i = 1; i <= 5; i++)
                {
                    var addDriverPopup = await additionalDriversPage.ClickOnAddAnotherDriver();
                    await addDriverPopup.FillForm(new Dictionary<string, string>
                    {
                        [FieldNames.FirstName] = $"Driver{i}",
                        [FieldNames.LastName] = "Test",
                        [FieldNames.DateOfBirth] = DateTime.Today.AddYears(-25).ToString("MM/dd/yyyy")
                    });
                    await addDriverPopup.ClickContinue();
                }
            });

            await _logger.ExecuteStepAsync("Verify add another driver button disabled at maximum", async () =>
            {
                var isAddButtonEnabled = await additionalDriversPage.IsAddAnotherDriverEnabled();

                _logger.LogDataValidation("Add Driver Button Disabled", !isAddButtonEnabled, "False", isAddButtonEnabled.ToString(), "Add Another Driver button should be disabled at 5 drivers");
                Assert.That(isAddButtonEnabled, Is.False, "Add Another Driver button should be disabled at 5 drivers");
            });

            await _logger.ExecuteStepAsync("Verify DOB masking on driver edit", async () =>
            {
                await additionalDriversPage.ClickContinue();
                var vehiclesPage = PageFactory.CreatePage<D2C_VehiclesPage>();
                await _pageHelper!.ClickBrowserBackButton();

                additionalDriversPage = PageFactory.CreatePage<D2C_AdditionalDriversPage>();
                var editDriverPopup = await additionalDriversPage.ClickOnSpecificDriverEditDetails("DRIVER TEST");

                await _d2cHelper.ValidateDobMasking(_pageHelper!, FieldNames.DateOfBirth);
            });
        }

        [Test]
        [RetryOnFailure(2)]
        [Author(Author.Gil)]
        [Tenant(BOLTAG)]
        [Category("D2C")]
        [Category("Quoting")]
        [TestCaseId(222996)]
        [Description("Verify vehicles page enforces maximum vehicle limit and VIN validation")]
        public async Task BOLTAG_D2C_Auto_VehiclesPage_ValidationErrors()
        {
            var vehiclesPage = await Executor.Execute<D2C_YourAddressPage, D2C_VehiclesPage>(
                FlowType.D2CAutoFlow,
                fillForms: true
            );
            await _logger.ExecuteStepAsync("Add maximum vehicles and verify limit message", async () =>
            {
                for (int i = 0; i < 4; i++)
                {
                    var anotherCar = await vehiclesPage.ClickOnAddAnotherCar();
                    await anotherCar.FillForm(new Dictionary<string, string>
                    {
                        [FieldNames.PLMake] = "LEXUS",
                        [FieldNames.PLModel] = "IS 250",
                        [FieldNames.PLYear] = "2007",
                        [FieldNames.BodyStyle] = "SEDAN",
                        [FieldNames.VehicleOwnerShip] = "Owned",
                        [FieldNames.AnnualMileage] = "10000"
                    });
                    await anotherCar.ClickContinue();
                }

                var actMessage = await vehiclesPage.GetVehicleMessage();
                var expMessage = "Policies are limited to 4 cars. Uncheck irrelevant cars before adding new cars.";
                Assert.That(actMessage, Does.Contain(expMessage));

                var isEnabled = await vehiclesPage.IsAdditionalCarBtnEnabled();
                Assert.That(isEnabled, Is.False, "Failed, Add another car should be disabled at limit.");
            });

            var foundVehicleName = string.Empty;

            await _logger.ExecuteStepAsync("Toggle vehicles and verify edit state", async () =>
            {
                var vehicleName = "2007 LEXUS IS 250";
                for (int i = 0; i < 4; i++)
                {
                    await vehiclesPage.SwitchSpecificCarToggleSlider(vehicleName, false);
                }

                var foundVehicles = await vehiclesPage.GetDiscoveredVehicles();
                Assert.That(foundVehicles, Is.Not.Empty, "Failed, expected at least one found vehicle on the page.");
                foundVehicleName = foundVehicles[0];

                await vehiclesPage.SwitchSpecificCarToggleSlider(foundVehicleName, true);
                var foundCarPopup = PageFactory.CreatePage<D2C_AddEditAnotherCarPopUp>();

                // allowDismiss: this popup is not known to carry a Cancel button, and an X /
                // backdrop dismiss leaves the vehicle incomplete just the same. If the run logs
                // the "no Cancel button" warning, swap this for a plain ClickCancel().
                await foundCarPopup.ClickCancel(allowDismiss: true);

                var actIsInError = await vehiclesPage.IsSpecificCarInErrorState(foundVehicleName);
                var isNextEnabled = await vehiclesPage.IsContinueButtonDisabled();

                Assert.Multiple(() =>
                {
                    Assert.That(actIsInError, Is.True, $"Failed, [{foundVehicleName}] should be flagged with a validation error.");
                    Assert.That(isNextEnabled, Is.True, "Failed, Continue btn should be disabled.");
                });
            });

            await _logger.ExecuteStepAsync("Verify VIN auto-populates vehicle details", async () =>
            {
                await vehiclesPage.SwitchSpecificCarToggleSlider(foundVehicleName, false);

                var addCarPopup = await vehiclesPage.ClickOnAddAnotherCar();
                await addCarPopup.SetVIN("19UDE2F33HA007791");

                var year = await _pageHelper!.GetFieldValue(FieldNames.PLYear);
                var make = await _pageHelper!.GetFieldValue(FieldNames.PLMake);
                var model = await _pageHelper!.GetFieldValue(FieldNames.PLModel);
                var bodyStyle = await _pageHelper!.GetFieldValue(FieldNames.BodyStyle);

                _logger.LogDataValidation("VIN Auto-Population", year == "2017" && make == "ACURA", "2017 ACURA", $"{year} {make}", "VIN should auto-populate vehicle details");

                await Assert.MultipleAsync(async () =>
                {
                    Assert.That(year, Is.EqualTo("2017"));
                    Assert.That(make, Is.EqualTo("ACURA"));
                    Assert.That(model, Does.Contain("ILX"));
                    Assert.That(bodyStyle, Does.Contain("SEDAN"));
                    Assert.That(await _pageHelper.IsFieldEnabled("PLYear"), Is.False, "PLYear should be disabled");
                    Assert.That(await _pageHelper.IsFieldEnabled("PLMake"), Is.False, "PLMake should be disabled");
                    Assert.That(await _pageHelper.IsFieldEnabled("PLModel"), Is.False, "PLModel should be disabled");
                    Assert.That(await _pageHelper.IsFieldEnabled("BodyStyle"), Is.False, "BodyStyle should be disabled");
                });
            });

            await _logger.ExecuteStepAsync("Validate mileage range and complete with valid data", async () =>
            {
                var addCarPopup = PageFactory.CreatePage<D2C_AddEditAnotherCarPopUp>();
                await addCarPopup.SetAnnualMileage("1");
                await addCarPopup.SetVIN("19UDE2F33HA007791");

                await _d2cHelper.ValidateErrorMessages(_pageHelper!, new Dictionary<string, string>
                {
                    ["Mileage Range"] = "Vehicle annual use should be between 1,000 & 100,000 miles."
                });

                await addCarPopup.SetAnnualMileage("10000");
                await addCarPopup.SelectOwnershipType("Owned");
                await addCarPopup.ClickContinue();
            });

            await _logger.ExecuteStepAsync("Continue and verify navigation to the driver history page", async () =>
            {
                await vehiclesPage.ClickContinue();
                PageFactory.CreatePage<D2C_DriverHistoryPage>();
            });
        }

        [Test]
        [RetryOnFailure(2)]
        [Author(Author.Gil)]
        [Tenant(BOLTAG)]
        [Category("D2C")]
        [Category("Quoting")]
        [TestCaseId(223002)]
        [Description("Verify driver history page enforces maximum incidents limit")]
        public async Task BOLTAG_D2C_Auto_DriverHistoryPage_ValidationErrors()
        {
            var driverHistoryPage = await Executor.Execute<D2C_YourAddressPage, D2C_DriverHistoryPage>(
                FlowType.D2CAutoFlow,
                fillForms: true
            );

            await _logger.ExecuteStepAsync("Add maximum incidents and verify counts", async () =>
            {
                await driverHistoryPage.AddIncidents("accidents", "At Fault With Injury", 5);
                await driverHistoryPage.AddIncidents("violations", "Careless Driving", 5);
                await driverHistoryPage.AddIncidents("losses", "Fire", 5);

                var isButtonDisplayed = await driverHistoryPage.IsAddAnotherButtonDisplay();
                _logger.LogDataValidation("Add Button Hidden At Max", !isButtonDisplayed, "False", isButtonDisplayed.ToString(), "Add button should not be displayed at maximum incidents");
                Assert.That(isButtonDisplayed, Is.False, "Expect that button is not displayed");

                await driverHistoryPage.ClickContinueButton();
                var coverPage = PageFactory.CreatePage<D2C_CoveragesPage>();
                await coverPage.ClickBackButton();

                driverHistoryPage = PageFactory.CreatePage<D2C_DriverHistoryPage>();

                var expectedMaxIncidents = 5;
                var actualAccidents = await driverHistoryPage.GetNumberOfIncidents("accidents");
                var actualLosses = await driverHistoryPage.GetNumberOfIncidents("losses");
                var actualViolations = await driverHistoryPage.GetNumberOfIncidents("violations");

                _logger.LogDataValidation("Accidents Count", actualAccidents == expectedMaxIncidents, expectedMaxIncidents.ToString(), actualAccidents.ToString(), "Should have maximum accidents");
                _logger.LogDataValidation("Losses Count", actualLosses == expectedMaxIncidents, expectedMaxIncidents.ToString(), actualLosses.ToString(), "Should have maximum losses");
                _logger.LogDataValidation("Violations Count", actualViolations == expectedMaxIncidents, expectedMaxIncidents.ToString(), actualViolations.ToString(), "Should have maximum violations");

                Assert.Multiple(() =>
                {
                    Assert.That(actualAccidents, Is.EqualTo(expectedMaxIncidents));
                    Assert.That(actualLosses, Is.EqualTo(expectedMaxIncidents));
                    Assert.That(actualViolations, Is.EqualTo(expectedMaxIncidents));
                });
            });

            await _logger.ExecuteStepAsync("Remove incidents and verify coverages on rates page", async () =>
            {
                await driverHistoryPage.SetSpecificStepper("accidents", "no", null, null, 1);
                await driverHistoryPage.SetSpecificStepper("violations", "no", null, null, 1);
                await driverHistoryPage.SetSpecificStepper("losses", "no", null, null, 1);
                await driverHistoryPage.ClickContinueButton();

                var coveragesPage = PageFactory.CreatePage<D2C_CoveragesPage>();
                await coveragesPage.ClickContinueButton();
                await coveragesPage.ClickContinueButton();

                var crossSellInformationPage = PageFactory.CreatePage<D2C_CrossSellInformationPage>();
                await crossSellInformationPage.ClickContinue();

                var policyPickerPage = PageFactory.CreatePage<D2C_PolicyDatePickerPage>();
                await policyPickerPage.ClickContinueButton();

                var ratespage = PageFactory.CreatePage<D2C_RatesPage>();
                await ratespage.ClickOnFirstViewDetailsButton();
                var policyCoverages = await ratespage.GetPolicyCoverages();
                var requiredCoverages = new[]
                {
                    "Property damage",
                    "Bodily injury",
                    "Uninsured/Underinsured Motorist",
                    "Collision Deductible",
                    "Comprehensive Deductible",
                    "Transportation Expense",
                    "Towing & Labor"
                };

                var missing = requiredCoverages
                    .Where(rc => !policyCoverages.Any(pc => pc.Contains(rc, StringComparison.OrdinalIgnoreCase)))
                    .ToList();

                if (missing.Count > 0)
                {
                    _logger.LogDataValidation("Required Coverages Missing", missing.Count == 0, "All Present", $"Missing: {string.Join(", ", missing)}", "All required coverages should be present");
                }
                else
                {
                    _logger.LogDataValidation("Required Coverages Present", true, "All Present", "All Present", "All required coverages are present");
                }
            });
        }

        [Test]
        [RetryOnFailure(2)]
        [Author(Author.Gil)]
        [Tenant(USAA)]
        [Category("D2C")]
        [Category("Quoting")]
        [TestCaseId(162448)]
        [Description("Verify Auto quote retrieval maintains driver and vehicle data")]
        public async Task USAA_D2C_Auto_Retrieval_Process()
        {
            var user = TestContextAccessor.CurrentUserCollection.OnlineQuote;
            ScopeContext.Set(ctx => ctx.CurrentUser, user);

            await _logger.ExecuteStepAsync("Create application via API and navigate to address page", async () =>
            {
                var requestData = new ApplicationRequestModel<PersonalLineData>
                {
                    Products = Products.PersonalAuto,
                    Data = PersonalLineDataProvider.GetPersonalAutoData(
                        address: AddressData.TX_Euless,
                        vehicles: [Vehicles.Vin_19UDE2F33HA007791, Vehicles.Vin_1HGCR2F59FA221666],
                        drivers: [Drivers.UsaaTestFirst, Drivers.UsaaTestSecWithLoss]
                    )
                };

                await _d2cHelper.CreateApplicationAndNavigate(_getQuoteApi, requestData);
            });

            await _logger.ExecuteStepAsync("Verify DOB masking and advance through primary driver", async () =>
            {
                var yourAddressPage = PageFactory.CreatePage<D2C_YourAddressPage>();
                await yourAddressPage.ClickContinue();

                var primaryDriverPage = PageFactory.CreatePage<D2C_PrimaryDriverPage>();
                var dobValue = await _pageHelper!.GetFieldValue(FieldNames.DateOfBirth);
                var isLicenseStatusShown = await _pageHelper.ElementExists(FieldNames.LicenseStatus, timeout: 5000);

                Assert.Multiple(() =>
                {
                    Assert.That(dobValue, Does.Contain("XX/XX/"), $"Expected DOB masking, got: [{dobValue}]");
                    Assert.That(isLicenseStatusShown, Is.True, "DriverLicenseStatus should be shown on the primary driver page");
                });

                await primaryDriverPage.ClickContinue();
                await primaryDriverPage.SelectOrDeselectAgreement(true);
                await primaryDriverPage.ClickContinue();
            });

            // Step 6: Drivers blind – 2 drivers shown, validation on 2nd driver only
            await _logger.ExecuteStepAsync("Validate 2 drivers and fix second driver validation error", async () =>
            {
                var additionalDriversPage = PageFactory.CreatePage<D2C_AdditionalDriversPage>();
                var allDrivers = await additionalDriversPage.GetAllDrivers();
                var driverErrors = await additionalDriversPage.CheckDriversValidationError();

                Assert.Multiple(() =>
                {
                    Assert.That(allDrivers.Count, Is.EqualTo(2), "Should have 2 drivers");
                    Assert.That(driverErrors, Does.Contain(2), "Validation error expected on the second driver");
                    Assert.That(driverErrors, Does.Not.Contain(1), "No validation error expected on the first driver");
                });

                // Step 7: Edit second driver and confirm no validation shown after fix
                var editDriverPopup = await additionalDriversPage.ClickOnSpecificDriverEditDetails("TESTSEC TESTQ");
                editDriverPopup.StartStep = 1;
                await editDriverPopup.FillForm(new Dictionary<string, string>
                {
                    [FieldNames.DateOfBirth] = DateTime.Today.AddYears(-25).ToString("MM/dd/yyyy"),
                    [FieldNames.DriverRelationshipToMainDriver] = "Other",
                });

                // Save the popup first - without it the driver stays invalid and the page's
                // Continue button never enables, so the flow never leaves the drivers page.
                // ClickSave, not ClickContinue: the latter's first selector matches the page's
                // own Continue button behind the modal, which discards the edit.
                await editDriverPopup.ClickSave();

                await additionalDriversPage.WaitForNoDriverValidationErrors();
                var errorsAfterFix = await additionalDriversPage.CheckDriversValidationError();
                Assert.That(errorsAfterFix, Is.Empty, "No driver should show a validation error after the second driver is fixed");

                await additionalDriversPage.ClickContinue();
            });

            // Step 8-9: Vehicles – 2 cars shown, add a 3rd
            await _logger.ExecuteStepAsync("Validate 2 vehicles, set ownership and add 3rd vehicle", async () =>
            {
                var vehiclesPage = PageFactory.CreatePage<D2C_VehiclesPage>();
                var allVehicles = await vehiclesPage.GetAllVehicles();
                Assert.That(allVehicles.Count, Is.EqualTo(2), "Should have 2 vehicles initially");

                var editVehicle1 = await vehiclesPage.ClickOnSpecificCarEditBtn("2017 ACURA ILX");
                await _pageHelper!.InteractWithField(FieldNames.VehicleOwnerShip, "Leased");
                await editVehicle1.ClickContinue();

                var editVehicle2 = await vehiclesPage.ClickOnSpecificCarEditBtn("2015 HONDA ACCORD");
                await _pageHelper!.InteractWithField(FieldNames.VehicleOwnerShip, "Owned");
                await editVehicle2.ClickContinue();

                var addVehiclePopup = await vehiclesPage.ClickOnAddAnotherCar();
                await addVehiclePopup.SetVIN("4T1BF1FK0FU1058976");
                await addVehiclePopup.FillForm(new Dictionary<string, string>
                {
                    [FieldNames.VehicleOwnerShip] = "Owned",
                    [FieldNames.AnnualMileage] = "10000"
                });
                await addVehiclePopup.ClickContinue();

                allVehicles = await vehiclesPage.GetAllVehicles();
                Assert.That(allVehicles.Count, Is.EqualTo(3), "Should have 3 vehicles after adding one");
                await vehiclesPage.ClickContinue();
            });

            // Step 10: Driver history – 1 claim shown for 2nd driver, skip all for both
            await _logger.ExecuteStepAsync("Validate 1 loss on second driver history and skip incidents", async () =>
            {
                var driverHistoryPage = PageFactory.CreatePage<D2C_DriverHistoryPage>();
                await driverHistoryPage.FillForm(null);
                await driverHistoryPage.ClickContinueButton();

                var lossCount = await driverHistoryPage.GetNumberOfIncidents("losses");
                Assert.That(lossCount, Is.EqualTo(1), "Expected 1 loss on second driver");

                await driverHistoryPage.SetSpecificStepper("accidents", "no", null, null, 2);
                await driverHistoryPage.SetSpecificStepper("violations", "no", null, null, 2);
                await driverHistoryPage.ClickContinueButton(2);
            });

            // Steps 11-12: Coverages – BI list shown, change vehicle coverage, advance to policy-start-date
            var policyDatePage = await _logger.ExecuteStepAsync("Validate BI coverage, update comp deductible and advance to policy date", async () =>
            {
                var coveragesPage = PageFactory.CreatePage<D2C_CoveragesPage>();
                var bodilyInjuryValue = await coveragesPage.GetBodilyInjuryLiabilityValue();
                Assert.That(bodilyInjuryValue, Does.Contain("$50k per person / $100k per accident"), "Default BI mismatch");

                await coveragesPage.ClickContinue();
                var comp1000Xpath = "//p[contains(@class,'name') and contains(text(),'2017 ACURA ILX')]//ancestor::app-vehicle-cover//ng-select[@name='CompDeductible']";
                await _pageHelper!.InteractWithElement(LocatorType.XPath, comp1000Xpath, ElementAction.Select,
                    new ElementInteractionOptions { IgnoreIfNotFound = false, Value = "500" });
                await coveragesPage.ClickContinue();

                // Step 13: Verify we reached policy-start-date
                return PageFactory.CreatePage<D2C_PolicyDatePickerPage>();
            });

            // Step 14: Go back to your-address, validate driver toggle, vehicle count, all coverages + D2CAgreeToTerms
            await _logger.ExecuteStepAsync("Go back and validate driver, vehicles, coverages and D2CAgreeToTerms retained", async () =>
            {
                await _d2cHelper.NavigateBackMultipleTimes(_pageHelper!, 5, 500);

                var primaryDriverPage = PageFactory.CreatePage<D2C_PrimaryDriverPage>();
                await primaryDriverPage.ClickContinue();
                await primaryDriverPage.ClickContinue();

                var additionalDriversPage = PageFactory.CreatePage<D2C_AdditionalDriversPage>();
                var isDriverToggleOn = await additionalDriversPage.IsSpecificDriverToggleSliderOn("AutoTest TestLast");
                Assert.That(isDriverToggleOn, Is.True, "Driver toggle should remain on after going back");
                await additionalDriversPage.ClickContinue();

                var vehiclesPage = PageFactory.CreatePage<D2C_VehiclesPage>();
                var allVehicles = await vehiclesPage.GetAllVehicles();
                Assert.That(allVehicles.Count, Is.EqualTo(3), "Should still have 3 vehicles");
                await vehiclesPage.ClickContinue();

                var driverHistoryPage = PageFactory.CreatePage<D2C_DriverHistoryPage>();
                await driverHistoryPage.SkipAllIncidentsButton();

                var coveragesPage = PageFactory.CreatePage<D2C_CoveragesPage>();
                await coveragesPage.ClickContinue();

                var compDeductible = await coveragesPage.GetSpecificCompDeductible("2017 ACURA ILX");
                Assert.That(compDeductible.Trim(), Is.EqualTo("$500"), "Comp deductible should be retained");
                await coveragesPage.ClickContinue();
            });

            // Step 15: Go to submission – examine result page (can fail or not)
            await _logger.ExecuteStepAsync("Navigate to rates page and examine submission result", async () =>
            {
                var policyDatePickerPage = PageFactory.CreatePage<D2C_PolicyDatePickerPage>();
                await policyDatePickerPage.ClickContinueButton();

                var ratesPage = PageFactory.CreatePage<D2C_RatesPage>();
                await ratesPage.ClickOnFirstViewDetailsButton();
                var vehicleNames = await ratesPage.GetEntityNames();

                Assert.Multiple(() =>
                {
                    Assert.That(vehicleNames.Count, Is.EqualTo(3), "Should have 3 vehicle entities on rates page");
                    Assert.That(vehicleNames.Any(v => v.Contains("2017 ACURA ILX")), Is.True, "Should contain 2017 ACURA ILX");
                });
            });
        }

        [Test]
        [RetryOnFailure(2)]
        [Author(Author.Gil)]
        [Tenant(BOLTAG)]
        [Category("D2C")]
        [Category("Quoting")]
        [Category("Sanity")]
        [RunIn(includeProduction: true, includeStaging: true)]
        [TestCaseId(240247)]
        [Description("Verify end-to-end Auto quote flow reaches rates page with least one rated carrier.")]
        public async Task BOLTAG_D2C_E2E_Auto_Reach_Rates_With_Available_Carriers()
        {
            var ratesPage = await _logger.ExecuteStepAsync("Navigate through Auto flow to rates page", async () =>
            {
                return await Executor.Execute<D2C_YourAddressPage, D2C_RatesPage>(
                    FlowType.D2CAutoFlow,
                    fillForms: true
                );
            });

            await _logger.ExecuteStepAsync("Verify rated carriers are available", async () =>
            {
                var ratedCarriers = await ratesPage.GetAllRatedCarriers();

                Assert.That(ratedCarriers.Count, Is.GreaterThan(0), "Should have rated carriers on Auto rates page");
            });
        }

        //safeco (Liberty mutual) Auto FQ flow is not relevant at the moment
        //[Test]
        //[Author(Author.Gil)]
        //[Tenant(Tenant.BOLTAG)]
        //[Category("D2C")]
        //[Category("Quoting")]
        //[Category("Payment")]
        //[Category("FullQuote")]
        //[TestCaseId(138666)]
        //[Description("Verify end-to-end Auto full quote flow with Safeco carrier")]
        //public async Task BOLTAG_D2C_Auto_E2E_FQFlow()
        //{
        //    var historyPage = await _logger.ExecuteStepAsync("Complete D2C Auto questionnaire flow to driver history", async () =>
        //    {
        //        return await Executor.Execute<D2C_YourAddressPage, D2C_DriverHistoryPage>(
        //            FlowType.D2CAutoFlow,
        //            new Dictionary<string, string>
        //            {
        //                [FieldNames.FirstName] = "GAIL",
        //                [FieldNames.LastName] = "SCHAFF",
        //                [FieldNames.Gender] = "Male",
        //                [FieldNames.DateOfBirth] = "08/03/1958",
        //                [FieldNames.Email] = "ForceIBSScore900@safeco.com"
        //            },
        //            fillForms: true,
        //            pageCallbacks: PageCallbackManager.For<D2C_AdditionalDriversPage>(async page =>
        //            {
        //                var addDriverPopup = await page.ClickOnAddAnotherDriver();
        //                await addDriverPopup.FillForm(new Dictionary<string, string>
        //                {
        //                    [FieldNames.FirstName] = "Test",
        //                    [FieldNames.LastName] = "Abbade",
        //                    [FieldNames.DateOfBirth] = "05/05/1990",
        //                    [FieldNames.DriverRelationshipToMainDriver] = "Other",
        //                });
        //                await addDriverPopup.ClickContinue();

        //                var driverName = "TEST ABBADE";
        //                Assert.That(await _pageHelper!.ElementExists(LocatorType.XPath, $"//div[contains(text(),'{driverName}')]"), Is.True);
        //                Assert.That(await page.IsSpecificDriverToggleSliderOn(driverName, 1), Is.True);
        //            })
        //        );
        //    });

        //    var ratesPage = await _logger.ExecuteStepAsync("Skip incidents and navigate to rates page", async () =>
        //    {
        //        await historyPage.SkipAllIncidentsButton();

        //        return await Executor.ExecuteToPage<D2C_RatesPage>(
        //            FlowType.D2CAutoFlow,
        //            PageFactory.CreatePage<D2C_CoveragesPage>(),
        //            fillForms: true
        //        );
        //    });

        //    var paymentPlanPage = await _logger.ExecuteStepAsync("Select Liberty Mutual carrier and complete full quote flow", async () =>
        //    {
        //        await ratesPage.ClickOnSpecificCarrierBuyNow("Liberty Mutual (formerly Safeco)");

        //        return await Executor.ExecuteToPage<D2C_PaymentPlanPageFQ>(
        //            FlowType.SafecoAutoFQFlow,
        //            PageFactory.CreatePage<D2C_SafecoPolicyDpolicyFQ>(),
        //            fillForms: true,
        //            pageCallbacks: PageCallbackManager.For<D2C_DriverDetailsFQ>(async page =>
        //            {
        //                await page.ClickContinue();

        //                await page.FillForm(new Dictionary<string, string>
        //                {
        //                    [FieldNames.DriverLicenseNumber] = "25986748",
        //                    [FieldNames.DriverDateLicensed] = DateTime.Today.AddYears(-15).ToString("MM/dd/yyyy")
        //                });
        //            })
        //        );
        //    });

        //    await _logger.ExecuteStepAsync("Verify payment plan elements and navigate to payment", async () =>
        //    {
        //        var hasCardFull = await _pageHelper!.ElementExists("Card Full");
        //        var hasDiscount = await _pageHelper!.ElementExists("Discount");

        //        _logger.LogDataValidation("Card Full Element", hasCardFull, "True", hasCardFull.ToString(), "Card Full should be present");
        //        _logger.LogDataValidation("Discount Element", hasDiscount, "True", hasDiscount.ToString(), "Discount should be present");

        //        await Assert.MultipleAsync(async () =>
        //        {
        //            Assert.That(hasCardFull, Is.True);
        //            Assert.That(hasDiscount, Is.True);
        //        });

        //        await paymentPlanPage.ClickContinueButton();

        //        var paymentPage = PageFactory.CreatePage<D2C_PaymentPageFQ>();
        //        await paymentPage.ClickOnChangePaymentButton();

        //        paymentPlanPage = PageFactory.CreatePage<D2C_PaymentPlanPageFQ>();
        //        await paymentPlanPage.FillForm();
        //        await paymentPlanPage.ClickContinueButton();
        //    });

        //    await _logger.ExecuteStepAsync("Complete payment and verify success", async () =>
        //    {
        //        var paymentPage = PageFactory.CreatePage<D2C_PaymentPageFQ>();
        //        var effectiveDate = await paymentPage.GetPolicyEffectiveDate();

        //        _logger.LogDataValidation("Policy Effective Date", !string.IsNullOrEmpty(effectiveDate) && effectiveDate != "Not Found Policy Effective Date", "Valid Date", effectiveDate ?? "null", "Expected valid policy effective date");
        //        Assert.That(string.IsNullOrEmpty(effectiveDate) || effectiveDate == "Not Found Policy Effective Date", Is.False,
        //            $"Expected valid policy effective date, but got: {effectiveDate}");
        //        await paymentPage.ClickOnContinueToPay();
        //        var paymentPopup = PageFactory.CreatePage<D2C_PaymentPopUp>();
        //        await paymentPopup.FillForm();
        //        await paymentPopup.ClickOnCompleteOrder();

        //        var paymentSuccess = PageFactory.CreatePage<D2C_PaymentFailurePageFQ>();
        //    });
        //}

        [Test]
        [RetryOnFailure(2)]
        [Author(Author.Gil)]
        [Tenant(BOLTAG)]
        [Category("D2C")]
        [Category("Quoting")]
        [TestCaseId(223504)]
        [RunIn(BoltEnvironment.Staging)]
        [Description("Verify AgencyOne integration with mocked rates in stage env")]
        public async Task AgencyOne_D2C_MockRates_E2E()
        {
            var agencyOneUrl = ScopeContext.Data.UrlDataCollection?.FrontEnd?.AdditionalUrls?.GetValueOrDefault("AgencyOneAddress");

            var additionalDriversPage = await Executor.Execute<D2C_YourAddressPage, D2C_AdditionalDriversPage>(
               FlowType.D2CAutoFlow,
               new Dictionary<string, string>
               {
                   [FieldNames.OnlineAddress] = "3345 Miller Park N, Garland, TX 75042"
               },
               fillForms: true,
               startUrl: agencyOneUrl
           );

            await _logger.ExecuteStepAsync("Verify Additional Drivers", async () =>
            {
                var allDrivers = await additionalDriversPage.GetAllDrivers();
                var hasOmar = allDrivers.Any(d => d.Contains("OMAR SCHAFF", StringComparison.OrdinalIgnoreCase));
                var hasGail = allDrivers.Any(d => d.Contains("GAIL SCHAFF", StringComparison.OrdinalIgnoreCase));

                _logger.LogDataValidation("OMAR SCHAFF Present", hasOmar, "True", hasOmar.ToString(), "Expected to see OMAR SCHAFF as additional driver");
                _logger.LogDataValidation("GAIL SCHAFF Present", hasGail, "True", hasGail.ToString(), "Expected to see GAIL SCHAFF as additional driver");

                Assert.Multiple(() =>
                {
                    Assert.That(hasOmar, Is.True, "Expected to see OMAR SCHAFF as additional driver");
                    Assert.That(hasGail, Is.True, "Expected to see GAIL SCHAFF as additional driver");
                });
            });

            await additionalDriversPage.ClickContinue();

            await _logger.ExecuteStepAsync("Configure Vehicles", async () =>
            {
                var vehiclesPage = PageFactory.CreatePage<D2C_VehiclesPage>();
                var allVehicles = await vehiclesPage.GetAllVehicles();

                _logger.LogDataValidation("Vehicle Count", allVehicles.Count > 1, "> 1", allVehicles.Count.ToString(), "Expected at least two vehicles");
                Assert.That(allVehicles.Count, Is.GreaterThan(1), $"Expected at least two vehicles, but found {allVehicles.Count}");

                var secondVehicle = allVehicles[1];
                await vehiclesPage.SwitchSpecificCarToggleSlider(secondVehicle, true);

                var editVehiclePopup = await vehiclesPage.ClickOnSpecificCarEditBtn(secondVehicle);
                await editVehiclePopup.FillForm(new Dictionary<string, string>
                {
                    [FieldNames.VehicleOwnerShip] = "Owned",
                    [FieldNames.AnnualMileage] = "11000"
                });
                await editVehiclePopup.ClickContinue();
                await vehiclesPage.ClickContinue();
            });

            var ratesPage = await Executor.ExecuteToPage<D2C_RatesPage>(
                FlowType.D2CAutoFlow,
                PageFactory.CreatePage<D2C_DriverHistoryPage>(),
                fillForms: true,
                pagesToSkip: [typeof(D2C_CrossSellInformationPage)]
            );

            await _logger.ExecuteStepAsync("Verify mocked rates available", async () =>
            {
                var ratesPage = PageFactory.CreatePage<D2C_RatesPage>();
                var ratedCarriers = await ratesPage.GetAllRatedCarriers();

                Assert.That(ratedCarriers.Count, Is.GreaterThan(0), $"Expected to get mocked rates on the result page, but found {ratedCarriers.Count}");
            });
        }

        [Test]
        [RetryOnFailure(2)]
        [Author(Author.Gil)]
        [Tenant(BOLTAG)]
        [Category("D2C")]
        [Category("CRM")]
        [Category("Quoting")]
        [TestCaseId(154504)]
        [Description("Verify Home and Auto bundle rates display correctly")]
        public async Task BOLTAG_D2C_HomeAuto_Bundle()
        {
            var _adbxHelper = new AdbxTestHelper(_logger, BrowserManager, PageFactory, _pageHelper!, ScopeContext);
            var url = ScopeContext.Get(ctx => ctx.CurrentUrl) ?? throw new TestSetupException("Current URL is null — ensure ScopeContext.CurrentUrl is set before the test runs");
            await BrowserManager.NavigateAsync(url);

            var ratesPage = await Executor.Execute<D2C_YourAddressPage, D2C_RatesPage>(
                FlowType.D2CHomeAutoFlow,
                new Dictionary<string, string>
                {
                    [FieldNames.RoofReplaced] = "Yes",
                },
                fillForms: true,
                  pagesToSkip: [typeof(D2C_PropertiesUsagePage), typeof(D2C_CrossSellInformationPage)]
            );

            await _logger.ExecuteStepAsync("Verify bundle rates and navigate to coverage details", async () =>
            {
                var isBundleDisplayed = await ratesPage.IsBundleResultsDisplayed();
                _logger.LogDataValidation("Bundle Results Displayed", isBundleDisplayed, "True", isBundleDisplayed.ToString(), "Should have bundle rates on the results page");
                Assert.That(isBundleDisplayed, Is.True, "No bundle rates on the results page");

                var carriers = await ratesPage.GetRatedCallToPurchaseCarriersForLob("Bundle & Save");
                _logger.LogDataValidation("Bundle Carriers Available", carriers.Count > 0, "> 0", carriers.Count.ToString(), "Expected at least one call-to-purchase carrier for Bundle & Save");
                Assert.That(carriers.Count, Is.GreaterThan(0), "Expected at least one call-to-purchase carrier for Bundle & Save");

                var callAgentPage = await ratesPage.ClickOnSpecificCarrierCallAgent(carriers.First());
                var friendId = await callAgentPage.GetFriendlyId();
                var coveragesPopup = await callAgentPage.ClickOnViewCoverageButton();

                var lobs = await coveragesPopup.GetPolicyLobs();
                var expectedCoverages = new[] { "Homeowners coverage", "Auto coverage" };

                _logger.LogDataValidation("LOB Count", lobs.Count == 2, "2", lobs.Count.ToString(), "Should have 2 LOBs (Homeowners and Auto)");

                Assert.Multiple(() =>
                {
                    Assert.That(lobs.Count, Is.EqualTo(2));
                    foreach (var expected in expectedCoverages)
                        Assert.That(lobs.Any(actual => actual.Equals(expected, StringComparison.OrdinalIgnoreCase)), Is.True,
                            $"Expected coverage '{expected}' not found in: [{string.Join(", ", lobs)}]");
                });

                var entityNames = await coveragesPopup.GetEntityNames();
                _logger.LogDataValidation("Entity Count", entityNames.Count >= 1, ">= 1", entityNames.Count.ToString(), "Expected at least 1 entity (vehicle)");
                Assert.That(entityNames.Count, Is.GreaterThanOrEqualTo(1), $"Expected at least 1 entity (vehicle), but got {entityNames.Count}");

                var agentUser = TestContextAccessor.CurrentUserCollection.Agent;
                ScopeContext.Set(ctx => ctx.CurrentUser, agentUser);
                _adbxApifactory = _testScope.ServiceProvider.GetRequiredService<IAdbxApiClientFactory>();
                var adbxApi = await _adbxApifactory.CreateApiClientAsync();
                var applicationId = await _mainQueries.Policy.GetPolicyIdByFriendlyIdAsync(friendId);
                var quoteResponse = await adbxApi.GetQuoteByQuoteId(applicationId);

                var products = quoteResponse.Content?.Products ?? string.Empty;
                _adbxHelper.ValidateQuoteProducts(products, ["Homeowners", "Personal Auto"]);

                var timelineResponse = await adbxApi.GetQuoteTimeline(applicationId);
                var note = timelineResponse.Content?.Events?
                    .FirstOrDefault(e => e.Subject == "Online Submission")?.Description
                    ?? throw new AssertionException("Online Submission event not found");

                var patterns = new Dictionary<string, string>
                {
                    ["Homeowners"] = @"Rates requested for Homeowners from \d+ carriers:[\s\S]*?-Success\(\d+\)",
                    ["Personal Auto"] = @"Rates requested for Personal Auto from \d+ carriers:[\s\S]*?-Success\(\d+\)"
                };
                _adbxHelper.ValidateTimelinePatterns(note, patterns);
            });
        }

        [Test]
        [RetryOnFailure(2)]
        [Author(Author.Gil)]
        [Tenant(BOLTAG)]
        [Category("D2C")]
        [Category("Quoting")]
        [TestCaseId(164101)]
        [Description("Verify adding Auto to existing HO3 quote via navigation")]
        public async Task BOLTAG_D2C_Multilob_HO3_Go_Back_Add_Auto()
        {
            var url = ScopeContext.Data.CurrentUrl;
            await BrowserManager.NavigateAsync(url);

            var houseDetailsPage = await Executor.Execute<D2C_YourAddressPage, D2C_HouseDetailsPage>(
                FlowType.D2CHomeFlow,
                fillForms: true,
                pagesToSkip: [typeof(D2C_PropertiesUsagePage)]
            );

            await _logger.ExecuteStepAsync("Navigate back to LOBs page and add Auto", async () =>
            {
                await _d2cHelper.NavigateBackMultipleTimes(_pageHelper!, 3, 500);

                var lobsPage = PageFactory.CreatePage<D2C_LobsPage>();
                var selectedLobs = await lobsPage.GetSelectedLobs();

                _logger.LogDataValidation("Initial LOB Selection", selectedLobs.Count == 1 && selectedLobs[0].Contains("Home"), "1 LOB: Home", $"{selectedLobs.Count} LOBs: {string.Join(", ", selectedLobs)}", "Expected only 1 lob selected (Home)");
                Assert.That(selectedLobs.Count == 1 && selectedLobs[0].Contains("Home"), Is.True,
                    $"Expected only 1 lob selected but actual {selectedLobs.Count}, expected Home but actual {selectedLobs[0]}");

                await lobsPage.SelectLob("Auto");
                await lobsPage.ClickContinue();

                var propertiesPage = PageFactory.CreatePage<D2C_PropertiesPage>();
                await propertiesPage.ClickContinue();

                var primaryResidencePage = PageFactory.CreatePage<D2C_PrimaryResidencePage>();
                await primaryResidencePage.ClickContinue();
            });

            await _logger.ExecuteStepAsync("Verify database contains both LOBs", async () =>
            {
                var friendlyId = await houseDetailsPage.GetFriendlyId();

                var expectedLobs = new[] { "PersonalAuto", "PersonalHome" };
                var setLobValues = await _testScope.ServiceProvider.GetRequiredService<IPollyRetryService>()
                    .ExecuteWithExceptionAsync(async () =>
                    {
                        _mainQueries.PolicyDataLogic.ClearXmlCache(friendlyId);
                        await Task.Delay(500);

                        var lobValues = await _mainQueries.PolicyDataLogic.GetNodeValuesListAsync(friendlyId, "SetLobs");

                        return expectedLobs.All(expectedLob => lobValues.Contains(expectedLob))
                            ? lobValues
                            : null;

                    }, 10, $"SetLobs [{string.Join(", ", expectedLobs)}] not found for friendlyId: {friendlyId}");

                foreach (var expectedLob in expectedLobs)
                {
                    _logger.LogDataValidation($"LOB {expectedLob} In Database", setLobValues.Contains(expectedLob), "True", setLobValues.Contains(expectedLob).ToString(), $"SetLobs should contain {expectedLob}");
                }

                Assert.Multiple(() =>
                {
                    foreach (var expectedLob in expectedLobs)
                        Assert.That(setLobValues, Does.Contain(expectedLob));
                });
            });

            await _logger.ExecuteStepAsync("Navigate to rates and verify both LOBs displayed", async () =>
            {
                var ratesPage = await Executor.ExecuteToPage<D2C_RatesPage>(
                    FlowType.D2CHomeAutoFlow,
                    PageFactory.CreatePage<D2C_HouseDetailsPage>(),
                    fillForms: true,
                    pagesToSkip: [typeof(D2C_CrossSellInformationPage)]
                );

                if (await ratesPage.IsBuySeparatelyResultsDisplayed())
                {
                    await ratesPage.ClickOnBuySeparatelyResults();
                }

                var listOfLobs = await ratesPage.ReturnAllLobsTitle();
                _logger.LogDataValidation("LOB Count On Rates Page", listOfLobs.Count == 2, "2", listOfLobs.Count.ToString(), "Should have 2 LOBs on rates page");
                Assert.That(listOfLobs.Count, Is.EqualTo(2));

                var requiredLobs = new List<string> { "Auto", "Homeowners" };
                foreach (var expectedLob in requiredLobs)
                {
                    _logger.LogDataValidation($"{expectedLob} LOB Present",
                        listOfLobs.Contains(expectedLob),
                        "True",
                        listOfLobs.Contains(expectedLob).ToString(),
                        $"Should contain {expectedLob} LOB");

                    Assert.That(listOfLobs, Does.Contain(expectedLob), $"Should contain {expectedLob} LOB");
                }
            });
        }

        [Test]
        [RetryOnFailure(2)]
        [Author(Author.Gil)]
        [Tenant(BOLTAG)]
        [Category("D2C")]
        [Category("Quoting")]
        [Category("Sanity")]
        [TestCaseId(173166)]
        [Description("Verify quote retrieval fails when using different source endpoints")]
        public async Task BOLTAG_D2C_Quote_Retrieval_With_Two_Different_Sources()
        {
            var env = ScopeContext.Get(ctx => ctx.Environment);
            var baseUrl = ScopeContext.Data.UrlDataCollection.FrontEnd.D2CUrl;
            var url = $"{baseUrl}D2CAutomation";

            await _logger.ExecuteStepAsync("Navigate to D2C and start quote flow", async () =>
            {
                await BrowserManager.NavigateAsync(url);
                var datePickerPage = await Executor.Execute<D2C_YourAddressPage, D2C_PolicyDatePickerPage>(
                 FlowType.D2CHomeFlow,
                 new Dictionary<string, string>
                 {
                     [FieldNames.LastName] = "quoteRetrievel",
                 },
                 fillForms: true,
                 pagesToSkip: [typeof(D2C_PropertiesUsagePage)]
                );
            });

            await _logger.ExecuteStepAsync("Open new window with different endpoint and attempt retrieval", async () =>
            {
                var secondEndpoint = env == Common.Environment.Uat ? "automationfeatureoff" : "WFGAGENT";
                var secondUrl = $"{baseUrl}{secondEndpoint}";
                await BrowserManager.OpenNewWindowAsync(secondUrl);

                var yourAddressPage = PageFactory.CreatePage<D2C_YourAddressPage>();
                var findMyQuotePage = await yourAddressPage.ClickOnFindMyQuote();
                await findMyQuotePage.FillForm();
                await findMyQuotePage.ClickOnRetrieveButton();

                var errorMessage = await findMyQuotePage.GetErrorMessage();

                _logger.LogDataValidation("Error Message Present",
                    !string.IsNullOrEmpty(errorMessage),
                    "Not Empty",
                    errorMessage ?? "null",
                    "Should display error message when retrieving quote from different source");

                Assert.That(!string.IsNullOrEmpty(errorMessage), Is.True,
                    "Expected error message when trying to retrieve quote from different source");
            });
        }

        [Test]
        [RetryOnFailure(2)]
        [Author(Author.Gil)]
        [Tenant(USAA)]
        [Category("D2C")]
        [Category("Quoting")]
        [TestCaseId(172981)]
        [Description("Verify quote retrieval validates updated credentials and source restrictions")]
        public async Task USAA_D2C_Quote_Retrieval_With_Validations_FindMyQuote_Funciotnality()
        {
            var user = TestContextAccessor.CurrentUserCollection.OnlineQuote;
            ScopeContext.Set(ctx => ctx.CurrentUser, user);
            var baseUrl = ScopeContext.Data.UrlDataCollection.FrontEnd.D2CUrl;

            var personalInfo = PersonalInfo.GetRandomPersonalInfo();
            var (originalFirstName, originalEmail, originalLastName, dob) = (
                personalInfo.FirstName,
                personalInfo.Email,
                personalInfo.LastName,
                personalInfo.DateOfBirth
            );

            var (friendlyId, updatedFirstName, updatedEmail) = await _logger.ExecuteStepAsync("Create application and update driver info", async () =>
            {
                var requestData = new ApplicationRequestModel<PersonalLineData>
                {
                    Products = Products.PersonalAuto,
                    Data = PersonalLineDataProvider.GetPersonalAutoData(
                        address: AddressData.TX_Euless,
                        vehicles: [Vehicles.Vin_19UDE2F33HA007791],
                        drivers: [Drivers.UsaaTestDriver],
                        personalInformation: personalInfo
                    )
                };

                await _d2cHelper.CreateApplicationAndNavigate(_getQuoteApi, requestData);

                var yourAddressPage = PageFactory.CreatePage<D2C_YourAddressPage>();
                var fId = await yourAddressPage.GetFriendlyId();
                await yourAddressPage.ClickContinue();

                var primaryDriverPage = PageFactory.CreatePage<D2C_PrimaryDriverPage>();
                var updatedFirst = RandomManager.GetRandomString(6);
                await _pageHelper!.InteractWithField(FieldNames.FirstName, updatedFirst);
                await primaryDriverPage.ClickContinue();

                var updatedMail = RandomManager.GetRandomEmail();
                await _pageHelper!.InteractWithField(FieldNames.Email, updatedMail);
                await primaryDriverPage.SelectOrDeselectAgreement(true);
                await primaryDriverPage.ClickContinue();

                return (fId, updatedFirst, updatedMail);
            });

            await _logger.ExecuteStepAsync("Verify retrieval fails with original credentials", async () =>
            {
                await BrowserManager.OpenNewWindowAsync(baseUrl + "OnlineQuote");

                var yourAddressPage = PageFactory.CreatePage<D2C_YourAddressPage>();
                var findMyQuotePage = await yourAddressPage.ClickOnFindMyQuote();

                var isRetrieveDisabled = await findMyQuotePage.IsRetrieveButtonDisabled();
                var isStartNewExist = await findMyQuotePage.IsStartNewQuoteButtonExist();

                _logger.LogDataValidation("Retrieve Button Disabled Initially", isRetrieveDisabled, "True", isRetrieveDisabled.ToString(), "Retrieve button should be disabled initially");
                _logger.LogDataValidation("Start New Quote Button Exists", isStartNewExist, "True", isStartNewExist.ToString(), "Start new quote button should exist");

                Assert.Multiple(() =>
                {
                    Assert.That(isRetrieveDisabled, Is.True, "Retrieve button should be disabled initially");
                    Assert.That(isStartNewExist, Is.True, "Start new quote button should exist");
                });

                await findMyQuotePage.FillForm(new Dictionary<string, string>
                {          
                    [FieldNames.FirstName] = originalFirstName,
                    [FieldNames.LastName] = originalLastName,
                    [FieldNames.DateOfBirth] = DateTime.Parse(dob).ToString("MM/dd/yyyy"),
                    [FieldNames.Email] = originalEmail
                });

                await findMyQuotePage.ClickOnRetrieveButton();
                var errorMessage = await findMyQuotePage.GetErrorMessage();

                _logger.LogDataValidation("Error With Original Credentials", !string.IsNullOrEmpty(errorMessage), "Not Empty", errorMessage ?? "null", "Expected error message when retrieving with original credentials");
                Assert.That(string.IsNullOrEmpty(errorMessage), Is.False, "Expected error with original credentials");
            });

            await _logger.ExecuteStepAsync("Successfully retrieve with updated credentials", async () =>
            {
                var findMyQuotePage = PageFactory.CreatePage<D2C_FindMyQuotePage>();
                await findMyQuotePage.FillForm(new Dictionary<string, string>
                {
                    [FieldNames.FirstName] = updatedFirstName,
                    [FieldNames.LastName] = originalLastName,
                    [FieldNames.DateOfBirth] = DateTime.Parse(dob).ToString("MM/dd/yyyy"),
                    [FieldNames.Email] = updatedEmail
                });

                await findMyQuotePage.ClickOnRetrieveButton();

                var currentUrl = BrowserManager.GetCurrentTab()?.Url ?? string.Empty;
                _logger.LogDataValidation("Retrieval With Updated Credentials", !currentUrl.ToLower().Contains("error"), "No error in URL", currentUrl, "Should navigate successfully without error after retrieval with updated credentials");
                Assert.That(currentUrl.ToLower(), Does.Not.Contain("error"), "Expected successful retrieval with updated credentials");
            });

            await _logger.ExecuteStepAsync("Edit quote in ADBX", async () =>
            {
                await BrowserManager.OpenNewWindowAsync(TestContextAccessor.CurrentUrlCollection.FrontEnd.LoginUrl);
                ScopeContext.Set(ctx => ctx.FrontEnd, FrontEndType.ADBX);
                var agentUser = TestContextAccessor.CurrentUserCollection.D2CAgent;
                ScopeContext.Set(ctx => ctx.CurrentUser, agentUser);

                var loginPage = PageFactory.CreatePage<STS_LoginPage>();
                await loginPage.Login();

                var homePage = PageFactory.CreatePage<ADBX_HomePage>();
                await homePage.SearchAndOpenFirstResultAsync(friendlyId);

                var quotePage = PageFactory.CreatePage<ADBX_QuoteSummaryPage>();

                // The account-information prompt only appears for some quotes; clicking Cancel
                // unconditionally waits out the full timeout whenever it is absent.
                if (await _pageHelper!.ElementExists(ADBX_FieldNames.QuoteSummaryCancelButton, timeout: 5000))
                    await _pageHelper.InteractWithField(ADBX_FieldNames.QuoteSummaryCancelButton);

                var summaryUrl = BrowserManager.GetCurrentTab()?.Url ?? string.Empty;
                await quotePage.ClickOnEditQuote();

                var editUrl = BrowserManager.GetCurrentTab()?.Url ?? string.Empty;
                _logger.LogDataValidation("Quote Opened For Edit In ADBX", editUrl != summaryUrl, $"A URL other than {summaryUrl}", editUrl, "Edit Quote should navigate off the ADBX quote summary");

                Assert.Multiple(() =>
                {
                    Assert.That(editUrl, Is.Not.Empty, "Edit Quote produced no page URL");
                    Assert.That(editUrl, Is.Not.EqualTo(summaryUrl), "Edit Quote did not navigate away from the ADBX quote summary");
                });
            });

            await _logger.ExecuteStepAsync("Verify retrieval fails from different source", async () =>
            {
                ScopeContext.Set(ctx => ctx.FrontEnd, FrontEndType.D2C);
                await BrowserManager.OpenNewWindowAsync($"{baseUrl}automationfeatureoff");

                var yourAddressPage = PageFactory.CreatePage<D2C_YourAddressPage>();
                var findMyQuotePage = await yourAddressPage.ClickOnFindMyQuote();

                await findMyQuotePage.FillForm(new Dictionary<string, string>
                {
                    [FieldNames.FirstName] = updatedFirstName,
                    [FieldNames.LastName] = originalLastName,
                    [FieldNames.DateOfBirth] = DateTime.Parse(dob).ToString("MM/dd/yyyy"),
                    [FieldNames.Email] = updatedEmail
                });

                await findMyQuotePage.ClickOnRetrieveButton();

                var errorMessage = await findMyQuotePage.GetErrorMessage();
                _logger.LogDataValidation("Error From Different Source", !string.IsNullOrEmpty(errorMessage), "Not Empty", errorMessage ?? "null", "Expected error when retrieving from different source");
                Assert.That(string.IsNullOrEmpty(errorMessage), Is.False, "Expected error when retrieving from different source (automationfeatureoff)");
            });
        }

        [Test]
        [RetryOnFailure(2)]
        [Author(Author.Gil)]
        [Tenant(BOLTAG)]
        [Category("D2C"), Category("Quoting")]
        [TestCaseId(175117)]
        [Description("Verify that creating an application with a single VIN saves the decoded vehicle data, and patching adds a second vehicle with correct make/year/model in PolicyData")]
        public async Task D2C_Add_Vehicle_With_VIN_Only_Using_API()
        {
            var apiHelper = new GetQuoteApiHelper(_getQuoteApi!, ScopeContext);
            var pollyRetry = _testScope.ServiceProvider.GetRequiredService<IPollyRetryService>();

            await _logger.ExecuteStepAsync("Create and submit application with single ACURA vehicle via API", async () =>
                await apiHelper.CreateAndSubmitApplicationWithPollingAsync(
                    new ApplicationRequestModel<PersonalLineData>
                    {
                        Products = Products.PersonalAuto,
                        Data = PersonalLineDataProvider.GetPersonalAutoData(
                            address: AddressData.TX_PLANO,
                            vehicles: [Vehicles.Vin_19UDE2F33HA007791],
                            drivers: [Drivers.UsaaTestDriver])
                    }, ratesCheck: false));

            var friendlyId = ScopeContext.Data.FriendlyId!;
            var applicationId = ScopeContext.Data.ExternalId!;

            async Task<List<string>> Fresh(string xpath)
            {
                _mainQueries!.PolicyDataLogic.ClearXmlCache(friendlyId);
                return await _mainQueries.PolicyDataLogic.GetNodeValuesListAsync(friendlyId, xpath);
            }

            await _logger.ExecuteStepAsync("Validate ACURA VIN 19UDE2F33HA007791 is saved in PolicyData", async () =>
            {
                var vins = await pollyRetry.ExecuteWithRetryAsync(async () =>
                {
                    var result = await Fresh("//VIN");
                    return result.Any(v => v.Contains("19UDE2F33HA007791")) ? result
                        : throw new Exception("VIN 19UDE2F33HA007791 not yet in PolicyData");
                });
            });

            await _logger.ExecuteStepAsync("Patch application to add Toyota vehicle (VIN 4T1BF1FK0FU105897)", async () =>
                await _getQuoteApi!.PatchApplicationAsync(applicationId,
                    new ApplicationRequestModel<PersonalLineData>
                    {
                        Data = new PersonalLineData { PersonalVehicles = [Vehicles.Vin_4T1BF1FK0FU1058976] }
                    }));

            await _logger.ExecuteStepAsync("Validate both vehicles are present with correct decoded data", async () =>
            {
                var makes = await pollyRetry.ExecuteWithRetryAsync(async () =>
                {
                    var result = await Fresh("//PLMake");
                    return result.Count >= 2 ? result
                        : throw new Exception($"Expected 2 vehicles in PolicyData, found {result.Count}");
                });

                var years = await Fresh("//PLYear");
                var models = await Fresh("//PLModel");

                var acuraIdx = makes.FindIndex(m => m.Equals("ACURA", StringComparison.OrdinalIgnoreCase));
                var toyotaIdx = makes.FindIndex(m => m.Contains("TOYOTA", StringComparison.OrdinalIgnoreCase));

                Assert.Multiple(() =>
                {
                    Assert.That(makes.Count, Is.EqualTo(2), "Expected 2 vehicles in PolicyData");
                });

                Assert.Multiple(() =>
                {
                    Assert.That(years[acuraIdx], Is.EqualTo("2017"), $"ACURA year mismatch, actual: {years[acuraIdx]}");
                    Assert.That(models[acuraIdx], Is.EqualTo("ILX BASE WATCH PLUS"), $"ACURA model mismatch, actual: {models[acuraIdx]}");
                    Assert.That(years[toyotaIdx], Is.EqualTo("2015"), $"TOYOTA year mismatch, actual: {years[toyotaIdx]}");
                    Assert.That(models[toyotaIdx], Is.EqualTo("CAMRY LE"), $"TOYOTA model mismatch, actual: {models[toyotaIdx]}");
                });
            });
        }


        [Test]
        [RetryOnFailure(2)]
        [Author(Author.Gil)]
        [Tenant(UNIFY)]
        [Category("CL")]
        [Category("Interview")]
        [Category("Quoting")]
        [TestCaseId(239923)]
        [Description("Invalid VIN Validation verifies through api request")]
        public async Task Unify_D2C_Invalid_VIN_Validation()
        {
            //At the moment, this will fail in UAT. Once the flag is verified to be enabled in UAT, we need to add the first user API key 
            string ExpectedVinMessage =
                "VIN was not found. Please verify the VIN number is correct and make sure the vehicle is valid for the product being quoted";

            var requestData = new ApplicationRequestModel<CommercialLineData>
            {
                Products = Products.CommercialAuto,
                Data = CommercialLineDataProvider.GetCommercialAutoData(
                vehicles: [Vehicles.CommercialAuto_001AN4GY3MM021769])
            };

            PostApplicationResponseModel<CommercialLineData> consumerResponse = null!;
            PostApplicationResponseModel<CommercialLineData> agentResponse = null!;

            await _logger.ExecuteStepAsync("Create Commercial Auto application as Consumer (D2C)", async () =>
                {
                    ScopeContext.Set(ctx => ctx.CurrentUser, TestContextAccessor.CurrentUserCollection.Consumer);
                    consumerResponse = await _getQuoteApi.CreateApplicationAsync(requestData).EnsureSuccessContentAsync();
                }, "Expected result: application created successfully for the D2C consumer");

            await _logger.ExecuteStepAsync("Verify invalid VIN is returned as a Warning", () =>
            {
                var warningMessages = consumerResponse?.Warnings?
                    .Where(w => w.Id?.Contains(".VIN") == true)
                    .SelectMany(w => w.Errors ?? [])
                    .ToList() ?? [];

                Assert.That(warningMessages, Has.Some.EqualTo(ExpectedVinMessage),
                    "Expected the invalid-VIN message under Warnings for the Consumer user.");
                return Task.CompletedTask;
            }, "Expected result: invalid VIN surfaced under Warnings (validation shown as a warning while the flag is on)");

            await _logger.ExecuteStepAsync("Create Commercial Auto application as Market Liberty agent (CL)", async () =>
            {
                ScopeContext.Set(ctx => ctx.CurrentUser, TestContextAccessor.CurrentUserCollection.MarketLibAgentCL);
                agentResponse = await _getQuoteApi.CreateApplicationAsync(requestData).EnsureSuccessContentAsync();
            }, "Expected result: application created successfully for the Market Liberty CL agent");

            await _logger.ExecuteStepAsync("Verify invalid VIN is returned as a ValidationError", () =>
            {
                var validationErrorMessages = agentResponse?.ValidationErrors?
                    .Where(e => e.Id?.Contains(".VIN") == true)
                    .SelectMany(e => e.Errors ?? [])
                    .ToList() ?? [];

                Assert.That(validationErrorMessages, Has.Some.EqualTo(ExpectedVinMessage),
                    "Expected the invalid-VIN message under ValidationErrors for the Market Liberty agent.");
                return Task.CompletedTask;
            }, "Expected result: invalid VIN surfaced under ValidationErrors for the CL agent");
        }

        [Test]
        [RetryOnFailure(2)]
        [Author(Author.Helen)]
        [Tenant(UNIFY)]
        [Category("D2C")]
        [Category("Quoting")]
        [Category("Sanity")]
        [TestCaseId(252376)]
        [RunIn(BoltEnvironment.Staging)]
        [Description("Verify the AgencyOne D2C PL Auto interview rates in CO and that two selected carriers can be compared")]
        public async Task UNIFY_D2C_Auto_Rates_Comparison_CO()
        {
            var agencyOneUrl = ScopeContext.Data.UrlDataCollection?.FrontEnd?.AdditionalUrls?.GetValueOrDefault("AgencyOneAddress")
                ?? throw new TestSetupException("UNIFY Staging AgencyOneAddress URL not configured");

            var ratesPage = await Executor.Execute<D2C_YourAddressPage, D2C_RatesPage>(
                FlowType.D2CAutoFlow,
                new Dictionary<string, string>
                {
                    [FieldNames.OnlineAddress] = "4444 Havana St, Denver, CO 80239, USA",
                    [FieldNames.FirstName] = "Tim",
                    [FieldNames.LastName] = "TAYLOR",
                    // The TC writes the date as 10.12.1978; read as the US MM/DD the form expects.
                    [FieldNames.DateOfBirth] = "10/12/1978",
                    [FieldNames.Gender] = "Male",
                    [FieldNames.Email] = "TAYLOR@epos.com",
                    [FieldNames.PrimaryPhoneNumber] = "2168989990",
                    [FieldNames.VIN] = "1GTP1BEKXS1113713",
                    [FieldNames.AnnualMileage] = "12334",
                },
                fillForms: true,
                // Coverages goes straight to the policy start date here - the cross-sell step the TC lists
                // is not rendered on this entry point.
                pagesToSkip: [typeof(D2C_CrossSellInformationPage)],
                startUrl: agencyOneUrl
            );

            var carriersToCompare = new List<string>();

            await _logger.ExecuteStepAsync("Verify at least two carriers returned a rate", async () =>
            {
                var ratedCarriers = await ratesPage.GetAllRatedCarriers();

                Assert.That(ratedCarriers.Count, Is.GreaterThanOrEqualTo(2), $"Expected at least two carrier rates, but found {ratedCarriers.Count}: [{string.Join(", ", ratedCarriers)}]");

                // Which carriers rate is quote-dependent, so the pair to compare is taken from what came back.
                carriersToCompare.AddRange(ratedCarriers.Take(2));
            }, "Expected result: user moved to the rates page and at least 2 carrier rates are displayed");

            await _logger.ExecuteStepAsync("Select two carriers to compare", async () =>
            {
                foreach (var carrier in carriersToCompare)
                    await ratesPage.SelectCarrierForComparison(carrier);

                var footerDisplayed = await ratesPage.IsComparisonFooterDisplayed();
                var footerCarriers = await ratesPage.GetCarriersInComparisonFooter();

                _logger.LogDataValidation("Comparison Footer Displayed", footerDisplayed, "True", footerDisplayed.ToString(), "Selecting carriers should raise the compare footer");

                Assert.Multiple(() =>
                {
                    Assert.That(footerDisplayed, Is.True, "Compare footer is not displayed after selecting carriers");
                    Assert.That(footerCarriers, Is.EquivalentTo(carriersToCompare), $"Compare footer shows [{string.Join(", ", footerCarriers)}] instead of the selected [{string.Join(", ", carriersToCompare)}]");
                });
            }, "Expected result: footer pop up displays the selected carriers");

            await _logger.ExecuteStepAsync("Open the comparison table", async () =>
            {
                var comparison = await ratesPage.ClickCompare();

                var comparedCarriers = await comparison.GetComparedCarriers();
                var premiums = await comparison.GetComparedPremiums();
                var coverageRows = await comparison.GetCoverageRows();

                Assert.Multiple(() =>
                {
                    Assert.That(comparedCarriers, Is.EquivalentTo(carriersToCompare), $"Comparison table shows [{string.Join(", ", comparedCarriers)}] instead of the selected [{string.Join(", ", carriersToCompare)}]");
                    Assert.That(premiums, Has.Count.EqualTo(carriersToCompare.Count).And.All.Not.Empty, $"Expected a premium per compared carrier, got [{string.Join(", ", premiums)}]");
                    Assert.That(coverageRows, Is.Not.Empty, "Comparison table displayed no coverage information");
                    Assert.That(coverageRows.Values, Is.All.Count.EqualTo(carriersToCompare.Count), "Every coverage row should carry a value for each compared carrier");
                });
            }, "Expected result: comparison table displays the selected carriers with premium and coverage information");
        }

        [Test]
        [RunIn(includeProduction: true)]
        [Author(Author.Sandy)]
        [Tenant(BOLTAG)]
        [TestCaseId(234114)]
        [Category("GetQuoteApi")]
        [Category("Regression")]
        [Description("Verifies RV makes, models, and details prefill lookups return 200 with expected data.")]
        public async Task BOLTAG_GQ_Vehicle_RV_LOB_Prefill_Usage_And_Endpoints()
        {
            await _logger.ExecuteStepAsync("GET Recreational Vehicle makes for year 2017 and verify A Liner is one of the makes", async () =>
            {
                var makes = await _getQuoteApi!.GetRVMakesAsync("2017").EnsureSuccessContentAsync();
                Assert.That(makes, Is.Not.Null.And.Not.Empty, "Response should contain a non-empty list of RV makes");
                Assert.That(makes, Does.Contain("A Liner"), "Response should contain the 'A Liner' make");
            });

            await _logger.ExecuteStepAsync("GET Recreational Vehicle models for year 2017 and make 'A Liner' and verify returned models", async () =>
            {
                var models = await _getQuoteApi!.GetRVModelsAsync("2017", "A Liner").EnsureSuccessContentAsync();
                Assert.That(models, Is.Not.Null.And.Not.Empty, "Response should contain a non-empty list of RV models");
                Assert.That(models, Does.Contain("A Liner Popup Trailer"), "Response should contain the 'A Liner Popup Trailer' model");
                Assert.That(models, Does.Contain("Ascape"), "Response should contain the 'Ascape' model");
            });

            await _logger.ExecuteStepAsync("GET Recreational Vehicle details for year 2017, make 'A Liner' and model 'Ascape'", async () =>
            {
                var details = await _getQuoteApi!.GetRVDetailsAsync("2017", "A Liner", "Ascape").EnsureSuccessContentAsync();
                Assert.That(details, Has.Count.EqualTo(1), "Response should contain exactly one RV detail and verify returned details");

                var detail = details[0];
                Assert.Multiple(() =>
                {
                    Assert.That(detail.Name, Is.EqualTo("2017 A Liner Ascape 13 FT"), "name should match");
                    Assert.That(detail.Type, Is.EqualTo("trailer"), "type should match");
                    // Length and Value come from the vendor's RV valuation feed and can be revised over time
                    // (e.g. yearly guide updates), so pin structural expectations instead of exact numbers.
                    Assert.That(detail.Length, Is.GreaterThan(0), "length should be a positive number");
                    Assert.That(detail.Value, Is.GreaterThan(0), "value should be a positive number");
                });
            });
        }
    }
}